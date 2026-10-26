using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Reserve, send and settle for guest-reply SMS. The same steps run for a
    /// plain reply, a Recovery Offer and a Respond-and-Record send.
    /// </summary>
    public sealed class GuestReplySmsSendStep : IGuestReplySmsSendStep
    {
        private static readonly string TrackingKeyAnonymousPrefix = "anon:";

        private readonly ApplicationDbContext _context;
        private readonly IRecoverySmsBillingReserve _smsBilling;
        private readonly IRecoveryGuestSmsDelivery _smsDelivery;
        private readonly TimeProvider _clock;

        public GuestReplySmsSendStep(
            ApplicationDbContext context,
            IRecoverySmsBillingReserve smsBilling,
            IRecoveryGuestSmsDelivery smsDelivery,
            TimeProvider clock
        )
        {
            _context = context;
            _smsBilling = smsBilling;
            _smsDelivery = smsDelivery;
            _clock = clock;
        }

        public void EnsureBillingLive()
        {
            if (!_smsBilling.IsLive)
            {
                throw new RecoverySmsBillingUnavailableException();
            }
        }

        public async Task EnsureOperatorBillingAllowsSendAsync(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            var restaurantId = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == locationId)
                .Select(row => row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (restaurantId == 0)
            {
                return;
            }

            var account = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (account == null)
            {
                return;
            }

            var deny = OperatorBillingLockEvaluator.EvaluateSendOrReserveDeny(
                OperatorBillingLockEvaluator.FromBillingAccount(account),
                _clock.GetUtcNow().UtcDateTime
            );
            if (deny != null)
            {
                throw new OperatorBillingLockedException(deny);
            }
        }

        public async Task<GuestReplySmsReservation> ReserveAsync(
            Feedback feedback,
            string idempotencyKey,
            string body,
            CancellationToken cancellationToken
        )
        {
            var restaurantId = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == feedback.RestaurantLocationId)
                .Select(row => row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);

            var estimate = CampaignSmsSegmentCalculator.CountSegments(body);
            await ReleaseExpiredOpenHoldForKeyAsync(
                restaurantId,
                feedback.Id,
                idempotencyKey,
                cancellationToken
            );

            var reservationRef = await ResolveOpenReservationRefAsync(
                restaurantId,
                feedback.Id,
                idempotencyKey,
                estimate,
                cancellationToken
            );

            if (reservationRef == null)
            {
                var reserve = await _smsBilling.ReserveAsync(
                    new RecoverySmsBillingReserveRequest
                    {
                        FeedbackId = feedback.Id,
                        LocationId = feedback.RestaurantLocationId,
                        Units = estimate,
                    },
                    cancellationToken
                );

                if (reserve is RecoverySmsBillingReserveResult.Failed failed)
                {
                    throw new RecoverySmsCreditRefusedException(
                        failed.Code,
                        failed.Remaining,
                        failed.Requested
                    );
                }

                reservationRef =
                    ((RecoverySmsBillingReserveResult.Ok)reserve).ReservationRef;

                var now = _clock.GetUtcNow().UtcDateTime;
                _context.RecoverySmsSendIdempotencies.Add(
                    new RecoverySmsSendIdempotency
                    {
                        RestaurantId = restaurantId,
                        FeedbackId = feedback.Id,
                        IdempotencyKey = TrackingKey(idempotencyKey, reservationRef),
                        ReservationRef = reservationRef,
                        ReservedUnits = estimate,
                        ReservedAtUtc = now,
                        HoldExpiresAtUtc = now
                            + CreditReservationSweeperBackgroundService.HoldTtl,
                    }
                );
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new GuestReplySmsReservation(
                feedback.Id,
                restaurantId,
                idempotencyKey,
                reservationRef
            );
        }

        public Task<RecoveryGuestSmsDeliveryResult> SendAsync(
            Feedback feedback,
            string body,
            CancellationToken cancellationToken
        )
        {
            return _smsDelivery.SendAsync(feedback.GuestContact, body, cancellationToken);
        }

        public async Task<bool> SettleAcceptedAsync(
            GuestReplySmsReservation reservation,
            RecoveryGuestSmsDeliveryResult.Accepted accepted,
            CancellationToken cancellationToken
        )
        {
            var settle = await _smsBilling.SettleAsync(
                new RecoverySmsBillingSettleRequest
                {
                    FeedbackId = reservation.FeedbackId,
                    ReservationRef = reservation.ReservationRef,
                    AcceptedUnits = accepted.AcceptedSegments,
                },
                cancellationToken
            );

            if (settle is RecoverySmsBillingSettleResult.Failed)
            {
                return false;
            }

            // Lock 05 / ticket 22: settle accepted units, then release unused hold
            // (same close order as CampaignBillingClose). Provider already accepted
            // and settle already committed — Release fail must not withhold the
            // guest-response fact; leftover hold stays for the TTL sweeper.
            await _smsBilling.ReleaseAsync(
                new RecoverySmsBillingReleaseRequest
                {
                    FeedbackId = reservation.FeedbackId,
                    ReservationRef = reservation.ReservationRef,
                },
                cancellationToken
            );

            return true;
        }

        public async Task ReleaseFailedAsync(
            GuestReplySmsReservation reservation,
            CancellationToken cancellationToken
        )
        {
            await _smsBilling.ReleaseAsync(
                new RecoverySmsBillingReleaseRequest
                {
                    FeedbackId = reservation.FeedbackId,
                    ReservationRef = reservation.ReservationRef,
                },
                cancellationToken
            );
            await ClearOpenHoldAsync(
                reservation.RestaurantId,
                reservation.IdempotencyKey,
                reservation.ReservationRef,
                cancellationToken
            );
        }

        public void ApplyAccepted(
            FeedbackGuestResponse row,
            GuestReplySmsReservation reservation,
            RecoveryGuestSmsDeliveryResult.Accepted accepted,
            DateTime atUtc
        )
        {
            row.BillingReservationRef = reservation.ReservationRef;
            row.ProviderMessageId = accepted.ProviderMessageId;

            // Accepted only when Twilio will call back with a status for this SID.
            // Without a callback or a SID, the send cannot be confirmed: Unavailable.
            var callbackCanConfirm =
                accepted.StatusCallbackSet
                && !string.IsNullOrWhiteSpace(accepted.ProviderMessageId);
            GuestReplyDeliveryStateMachine.TryAdvance(
                row,
                callbackCanConfirm
                    ? GuestReplyDeliveryState.Accepted
                    : GuestReplyDeliveryState.Unavailable,
                atUtc
            );
        }

        public async Task CompleteAsync(
            GuestReplySmsReservation reservation,
            FeedbackGuestResponse row,
            CancellationToken cancellationToken
        )
        {
            var restaurantId = reservation.RestaurantId;
            var idempotencyKey = reservation.IdempotencyKey;
            var reservationRef = reservation.ReservationRef;

            var trackingKey = TrackingKey(idempotencyKey, reservationRef);
            var open = await _context.RecoverySmsSendIdempotencies
                .FirstOrDefaultAsync(
                    item =>
                        item.RestaurantId == restaurantId
                        && item.IdempotencyKey == trackingKey
                        && item.CompletedGuestResponseId == null,
                    cancellationToken
                );

            if (open == null)
            {
                open = await _context.RecoverySmsSendIdempotencies
                    .FirstOrDefaultAsync(
                        item =>
                            item.RestaurantId == restaurantId
                            && item.ReservationRef == reservationRef
                            && item.CompletedGuestResponseId == null,
                        cancellationToken
                    );
            }

            if (open == null)
            {
                if (idempotencyKey.Length == 0)
                {
                    return;
                }

                _context.RecoverySmsSendIdempotencies.Add(
                    new RecoverySmsSendIdempotency
                    {
                        RestaurantId = restaurantId,
                        FeedbackId = row.FeedbackId,
                        IdempotencyKey = idempotencyKey,
                        ReservationRef = reservationRef,
                        ReservedUnits = CampaignSmsSegmentCalculator.CountSegments(
                            row.Body
                        ),
                        ReservedAtUtc = row.CreatedAt,
                        HoldExpiresAtUtc = row.CreatedAt,
                        CompletedGuestResponse = row,
                        CompletedAtUtc = _clock.GetUtcNow().UtcDateTime,
                    }
                );
                return;
            }

            if (idempotencyKey.Length == 0)
            {
                _context.RecoverySmsSendIdempotencies.Remove(open);
                return;
            }

            open.CompletedGuestResponse = row;
            open.CompletedAtUtc = _clock.GetUtcNow().UtcDateTime;
        }

        private static string TrackingKey(string clientKey, string reservationRef)
            => clientKey.Length > 0
                ? clientKey
                : TrackingKeyAnonymousPrefix + reservationRef;

        private async Task ReleaseExpiredOpenHoldForKeyAsync(
            int restaurantId,
            int feedbackId,
            string idempotencyKey,
            CancellationToken cancellationToken
        )
        {
            if (idempotencyKey.Length == 0)
            {
                return;
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var expired = await _context.RecoverySmsSendIdempotencies
                .FirstOrDefaultAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && row.IdempotencyKey == idempotencyKey
                        && row.CompletedGuestResponseId == null
                        && row.HoldExpiresAtUtc <= now,
                    cancellationToken
                );

            if (expired == null)
            {
                return;
            }

            await _smsBilling.ReleaseAsync(
                new RecoverySmsBillingReleaseRequest
                {
                    FeedbackId = feedbackId,
                    ReservationRef = expired.ReservationRef,
                },
                cancellationToken
            );
            _context.RecoverySmsSendIdempotencies.Remove(expired);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<string?> ResolveOpenReservationRefAsync(
            int restaurantId,
            int feedbackId,
            string idempotencyKey,
            int estimate,
            CancellationToken cancellationToken
        )
        {
            if (idempotencyKey.Length == 0)
            {
                return null;
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var open = await _context.RecoverySmsSendIdempotencies
                .FirstOrDefaultAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && row.IdempotencyKey == idempotencyKey
                        && row.CompletedGuestResponseId == null
                        && row.HoldExpiresAtUtc > now,
                    cancellationToken
                );

            if (open == null)
            {
                return null;
            }

            if (open.FeedbackId != feedbackId || open.ReservedUnits != estimate)
            {
                return null;
            }

            return open.ReservationRef;
        }

        private async Task ClearOpenHoldAsync(
            int restaurantId,
            string idempotencyKey,
            string reservationRef,
            CancellationToken cancellationToken
        )
        {
            var trackingKey = TrackingKey(idempotencyKey, reservationRef);
            var open = await _context.RecoverySmsSendIdempotencies
                .FirstOrDefaultAsync(
                    item =>
                        item.RestaurantId == restaurantId
                        && (
                            item.IdempotencyKey == trackingKey
                            || item.ReservationRef == reservationRef
                        )
                        && item.CompletedGuestResponseId == null,
                    cancellationToken
                );

            if (open != null)
            {
                _context.RecoverySmsSendIdempotencies.Remove(open);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
