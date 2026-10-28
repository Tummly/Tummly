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
    public class GuestReplySmsSendStep : IGuestReplySmsSendStep
    {
        private static readonly string TrackingKeyAnonymousPrefix = "anon:";

        private readonly ApplicationDbContext _context;
        private readonly IGuestContactSuppressionService _suppressions;
        private readonly IRecoverySmsBillingReserve _smsBilling;
        private readonly IRecoveryGuestSmsDelivery _smsDelivery;
        private readonly TimeProvider _clock;
        private readonly ILogger<GuestReplySmsSendStep> _logger;

        public GuestReplySmsSendStep(
            ApplicationDbContext context,
            IGuestContactSuppressionService suppressions,
            IRecoverySmsBillingReserve smsBilling,
            IRecoveryGuestSmsDelivery smsDelivery,
            TimeProvider clock,
            ILogger<GuestReplySmsSendStep> logger
        )
        {
            _context = context;
            _suppressions = suppressions;
            _smsBilling = smsBilling;
            _smsDelivery = smsDelivery;
            _clock = clock;
            _logger = logger;
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

        /// <summary>
        /// Every guest-reply SMS path calls this before any credit reserve or row save.
        /// A guest who sent STOP to the restaurant is refused (R4.5). The service
        /// reply gate is rechecked here at send time, including suppression.
        /// </summary>
        public async Task<GuestReplySmsReservation> ReserveAsync(
            Feedback feedback,
            string idempotencyKey,
            string body,
            CancellationToken cancellationToken
        )
        {
            await EnsureGuestNotSmsStoppedAsync(feedback, cancellationToken);
            await EnsureServiceReplyAllowedAsync(feedback, cancellationToken);

            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == feedback.RestaurantLocationId)
                .Select(row => new
                {
                    row.RestaurantId,
                    RestaurantName = row.Restaurant!.Name,
                })
                .FirstOrDefaultAsync(cancellationToken);
            var restaurantId = location?.RestaurantId ?? 0;

            // The reserve, the provider call and the reply's segment count all use this text.
            var smsText = GuestSmsText.ForReply(
                location?.RestaurantName ?? string.Empty,
                body
            );
            var estimate = CampaignSmsSegmentCalculator.CountSegments(smsText);
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
                reservationRef,
                smsText
            );
        }

        private async Task EnsureServiceReplyAllowedAsync(
            Feedback feedback,
            CancellationToken cancellationToken
        )
        {
            var decision = await ServiceReplyEligibility.EvaluateAsync(
                _context,
                _suppressions,
                feedback,
                FeedbackGuestResponseChannel.Sms,
                cancellationToken
            );
            if (decision != ServiceReplyDecision.Allowed)
            {
                throw new ServiceReplyRefusedException(decision);
            }
        }

        private async Task EnsureGuestNotSmsStoppedAsync(
            Feedback feedback,
            CancellationToken cancellationToken
        )
        {
            var stopped = await _context.LocationGuests
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.Id == feedback.LocationGuestId
                        && row.MasterGuest != null
                        && row.MasterGuest.SmsStoppedAtUtc != null,
                    cancellationToken
                );

            if (stopped)
            {
                throw new GuestReplySmsStoppedException();
            }
        }

        public async Task SavePendingReplyAsync(
            GuestReplySmsReservation reservation,
            FeedbackGuestResponse row,
            CancellationToken cancellationToken
        )
        {
            try
            {
                await SavePendingChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Recovery SMS pending reply could not be saved before the send. ExceptionType {ExceptionType} FeedbackId {FeedbackId} RestaurantId {RestaurantId} ReservationRef {ReservationRef}",
                    ex.GetType().Name,
                    reservation.FeedbackId,
                    reservation.RestaurantId,
                    reservation.ReservationRef
                );

                // Nothing was sent yet. Drop the unsaved row and give the credits back.
                var entry = _context.Entry(row);
                if (entry.State != EntityState.Detached)
                {
                    entry.State = EntityState.Detached;
                }

                try
                {
                    await ReleaseFailedAsync(reservation, CancellationToken.None);
                }
                catch (Exception releaseEx)
                {
                    _logger.LogError(
                        "Recovery SMS hold release threw {ExceptionType} after the pending reply save failed. Hold left for the sweeper. ReservationRef {ReservationRef}",
                        releaseEx.GetType().Name,
                        reservation.ReservationRef
                    );
                }

                if (ex is OperationCanceledException)
                {
                    throw;
                }

                // Not an InvalidOperationException, so the controller does not return 401.
                throw new GuestReplySmsSendFailedException();
            }
        }

        public async Task SaveFailedReplyAsync(
            FeedbackGuestResponse row,
            DateTime atUtc
        )
        {
            GuestReplyDeliveryStateMachine.TryAdvance(
                row,
                GuestReplyDeliveryState.Failed,
                atUtc
            );

            try
            {
                // No request token: a client disconnect must not leave the row Pending.
                await SaveFailedChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                // The row stays Pending and blocks a retry for 10 minutes. The caller
                // still returns the failed result (502).
                _logger.LogError(
                    "Recovery SMS reply could not be moved to Failed. ExceptionType {ExceptionType} FeedbackId {FeedbackId} GuestResponseId {GuestResponseId}",
                    ex.GetType().Name,
                    row.FeedbackId,
                    row.Id
                );
            }
        }

        /// <summary>The Pending reply save. A seam for tests that inject a save failure.</summary>
        protected virtual Task SavePendingChangesAsync(CancellationToken cancellationToken)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>The Failed reply save. A seam for tests that inject a save failure.</summary>
        protected virtual Task SaveFailedChangesAsync(CancellationToken cancellationToken)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<GuestReplySmsOutcome> SendAndSettleAsync(
            GuestReplySmsReservation reservation,
            Feedback feedback,
            CancellationToken cancellationToken
        )
        {
            RecoveryGuestSmsDeliveryResult delivery;
            try
            {
                delivery = await _smsDelivery.SendAsync(
                    feedback.GuestContact,
                    reservation.SmsText,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                // Timeout or network failure: the provider may have accepted the
                // text. Log the type only (no phone, no message text).
                _logger.LogError(
                    "Recovery SMS provider call threw {ExceptionType}. Outcome unknown. ReservationRef {ReservationRef}",
                    ex.GetType().Name,
                    reservation.ReservationRef
                );
                return new GuestReplySmsOutcome(
                    GuestReplySmsOutcomeKind.Unknown,
                    null,
                    false
                );
            }

            if (delivery is RecoveryGuestSmsDeliveryResult.Failed)
            {
                // The provider has answered. Later writes ignore the request token.
                await ReleaseFailedAsync(reservation, CancellationToken.None);

                return new GuestReplySmsOutcome(
                    GuestReplySmsOutcomeKind.Failed,
                    null,
                    false
                );
            }

            var accepted = (RecoveryGuestSmsDeliveryResult.Accepted)delivery;
            // The provider has answered. Settle and every later write ignore the
            // request token, so a client disconnect cannot leave an accepted send
            // unsettled or the reply without its SID.
            var settled = await TrySettleAcceptedAsync(reservation, accepted);

            return new GuestReplySmsOutcome(
                GuestReplySmsOutcomeKind.Accepted,
                accepted,
                settled
            );
        }

        /// <summary>
        /// Settles the accepted units, then releases the unused hold. A failure is
        /// logged and returns false. The hold stays open, so the sweeper releases it.
        /// </summary>
        private async Task<bool> TrySettleAcceptedAsync(
            GuestReplySmsReservation reservation,
            RecoveryGuestSmsDeliveryResult.Accepted accepted
        )
        {
            var cancellationToken = CancellationToken.None;
            try
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
                    _logger.LogError(
                        "Recovery SMS settle refused after provider accept. Hold left for the sweeper. ReservationRef {ReservationRef}",
                        reservation.ReservationRef
                    );
                    return false;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    "Recovery SMS settle threw {ExceptionType} after provider accept. Hold left for the sweeper. ReservationRef {ReservationRef}",
                    ex.GetType().Name,
                    reservation.ReservationRef
                );
                return false;
            }

            // Lock 05 / ticket 22: settle accepted units, then release unused hold.
            // If the release throws, the hold stays open for the sweeper.
            try
            {
                await _smsBilling.ReleaseAsync(
                    new RecoverySmsBillingReleaseRequest
                    {
                        FeedbackId = reservation.FeedbackId,
                        ReservationRef = reservation.ReservationRef,
                    },
                    cancellationToken
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    "Recovery SMS hold release threw {ExceptionType} after settle. Hold left for the sweeper. ReservationRef {ReservationRef}",
                    ex.GetType().Name,
                    reservation.ReservationRef
                );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Saves the reply after the provider accepted it. A settled reply completes its
        /// idempotency record first. A completion failure is logged and the reply is still
        /// saved. A save failure throws <see cref="GuestReplySmsSendFailedException"/>, never
        /// an InvalidOperationException, so the controller does not return 401.
        /// </summary>
        public async Task SaveAcceptedReplyAsync(
            GuestReplySmsReservation reservation,
            bool settled,
            FeedbackGuestResponse row
        )
        {
            // The provider has answered: these writes ignore the request token.
            var cancellationToken = CancellationToken.None;
            if (settled)
            {
                try
                {
                    await CompleteAsync(reservation, row, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(
                        "Recovery SMS idempotency completion threw {ExceptionType} after provider accept. The reply is saved without completion. FeedbackId {FeedbackId} RestaurantId {RestaurantId} ReservationRef {ReservationRef}",
                        ex.GetType().Name,
                        reservation.FeedbackId,
                        reservation.RestaurantId,
                        reservation.ReservationRef
                    );
                }
            }

            await SaveReplyOrFailAsync(reservation);
        }

        public Task SaveRecordedUnknownReplyAsync(
            GuestReplySmsReservation reservation
        )
        {
            return SaveReplyOrFailAsync(reservation);
        }

        private async Task SaveReplyOrFailAsync(
            GuestReplySmsReservation reservation
        )
        {
            try
            {
                await SaveReplyChangesAsync(CancellationToken.None);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    "Recovery SMS reply could not be saved after provider accept. ExceptionType {ExceptionType} FeedbackId {FeedbackId} RestaurantId {RestaurantId} ReservationRef {ReservationRef}",
                    ex.GetType().Name,
                    reservation.FeedbackId,
                    reservation.RestaurantId,
                    reservation.ReservationRef
                );
                throw new GuestReplySmsSendFailedException();
            }
        }

        /// <summary>The reply save. A seam for tests that inject a save failure.</summary>
        protected virtual Task SaveReplyChangesAsync(CancellationToken cancellationToken)
        {
            return _context.SaveChangesAsync(cancellationToken);
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

        public void ApplyUnknownOutcome(
            FeedbackGuestResponse row,
            GuestReplySmsReservation reservation,
            DateTime atUtc
        )
        {
            row.BillingReservationRef = reservation.ReservationRef;
            GuestReplyDeliveryStateMachine.TryAdvance(
                row,
                GuestReplyDeliveryState.Unavailable,
                atUtc
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

        public virtual async Task CompleteAsync(
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
                            reservation.SmsText
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
