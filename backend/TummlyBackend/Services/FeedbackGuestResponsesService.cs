using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class FeedbackGuestResponsesService : IFeedbackGuestResponsesService
    {
        public const int MaxListLimit = 100;

        private static readonly TimeSpan CompletedIdempotencyWindow = TimeSpan.FromHours(24);

        private readonly ApplicationDbContext _context;
        private readonly IGuestResponseEmailDeliveryWork _emailDelivery;
        private readonly IRecoverySmsBillingReserve _smsBilling;
        private readonly IRecoveryGuestSmsDelivery _smsDelivery;
        private readonly TimeProvider _clock;

        public FeedbackGuestResponsesService(
            ApplicationDbContext context,
            IGuestResponseEmailDeliveryWork emailDelivery,
            IRecoverySmsBillingReserve smsBilling,
            IRecoveryGuestSmsDelivery smsDelivery,
            TimeProvider clock
        )
        {
            _context = context;
            _emailDelivery = emailDelivery;
            _smsBilling = smsBilling;
            _smsDelivery = smsDelivery;
            _clock = clock;
        }

        public async Task<SendFeedbackGuestResponseResultDto?> SendAsync(
            int feedbackId,
            int authorUserId,
            FeedbackGuestResponseChannel channel,
            FeedbackRecoveryIntent intent,
            string? subject,
            string body,
            string? purpose,
            string? tone,
            string? includeNotes,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default
        )
        {
            var content = FeedbackGuestResponseComposer.ValidateContent(
                channel,
                subject,
                body
            );

            if (intent != FeedbackRecoveryIntent.RespondToGuest)
            {
                throw new ArgumentException(
                    "Intent must be respond_to_guest."
                );
            }

            var author = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == authorUserId, cancellationToken);

            if (author == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            var feedback = await _context.Feedbacks
                .FirstOrDefaultAsync(f => f.Id == feedbackId, cancellationToken);

            if (feedback == null)
            {
                return null;
            }

            if (feedback.WorkflowStatus == FeedbackWorkflowStatus.Resolved)
            {
                throw new FeedbackAlreadyResolvedException();
            }

            FeedbackGuestResponseComposer.EnsureChannelMatchesContact(
                feedback,
                channel
            );

            await EnsureOperatorBillingAllowsSendAsync(
                feedback.RestaurantLocationId,
                cancellationToken
            );

            if (channel == FeedbackGuestResponseChannel.Email)
            {
                return await SendEmailAsync(
                    feedback,
                    authorUserId,
                    author.FullName,
                    content,
                    purpose,
                    tone,
                    includeNotes,
                    cancellationToken
                );
            }

            return await SendSmsAsync(
                feedback,
                authorUserId,
                author.FullName,
                content,
                purpose,
                tone,
                includeNotes,
                idempotencyKey,
                cancellationToken
            );
        }

        public async Task<IReadOnlyList<FeedbackGuestResponseItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        )
        {
            var rows = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .Where(r => r.FeedbackId == feedbackId)
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id)
                .Take(MaxListLimit)
                .ToListAsync(cancellationToken);

            return rows.Select(ToItemDto).ToList();
        }

        private async Task<SendFeedbackGuestResponseResultDto> SendEmailAsync(
            Feedback feedback,
            int authorUserId,
            string authorDisplayName,
            FeedbackGuestResponseComposer.ValidatedContent content,
            string? purpose,
            string? tone,
            string? includeNotes,
            CancellationToken cancellationToken
        )
        {
            var row = FeedbackGuestResponseComposer.Build(
                feedback,
                FeedbackGuestResponseChannel.Email,
                FeedbackRecoveryIntent.RespondToGuest,
                content,
                purpose,
                tone,
                includeNotes,
                authorUserId,
                authorDisplayName,
                DateTime.UtcNow
            );

            row.EmailDeliveryStatus = GuestResponseEmailDeliveryStatus.Pending;

            _context.FeedbackGuestResponses.Add(row);
            await _context.SaveChangesAsync(cancellationToken);

            await _emailDelivery.NotifyAsync(row.Id, cancellationToken);

            return BuildResult(feedback, row);
        }

        private async Task<SendFeedbackGuestResponseResultDto> SendSmsAsync(
            Feedback feedback,
            int authorUserId,
            string authorDisplayName,
            FeedbackGuestResponseComposer.ValidatedContent content,
            string? purpose,
            string? tone,
            string? includeNotes,
            string? idempotencyKey,
            CancellationToken cancellationToken
        )
        {
            if (!_smsBilling.IsLive)
            {
                throw new RecoverySmsBillingUnavailableException();
            }

            var restaurantId = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == feedback.RestaurantLocationId)
                .Select(row => row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);

            var normalizedKey = (idempotencyKey ?? string.Empty).Trim();
            if (normalizedKey.Length > 0)
            {
                var replay = await TryReplayCompletedAsync(
                    restaurantId,
                    normalizedKey,
                    feedback,
                    cancellationToken
                );
                if (replay != null)
                {
                    return replay;
                }
            }

            var estimate = CampaignSmsSegmentCalculator.CountSegments(content.Body);
            await ReleaseExpiredOpenHoldForKeyAsync(
                restaurantId,
                feedback.Id,
                normalizedKey,
                cancellationToken
            );

            var reservationRef = await ResolveOpenReservationRefAsync(
                restaurantId,
                feedback.Id,
                normalizedKey,
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
                        IdempotencyKey = TrackingKey(normalizedKey, reservationRef),
                        ReservationRef = reservationRef,
                        ReservedUnits = estimate,
                        ReservedAtUtc = now,
                        HoldExpiresAtUtc = now
                            + CreditReservationSweeperBackgroundService.HoldTtl,
                    }
                );
                await _context.SaveChangesAsync(cancellationToken);
            }

            var delivery = await _smsDelivery.SendAsync(
                feedback.GuestContact,
                content.Body,
                cancellationToken
            );

            if (delivery is RecoveryGuestSmsDeliveryResult.Failed)
            {
                await _smsBilling.ReleaseAsync(
                    new RecoverySmsBillingReleaseRequest
                    {
                        FeedbackId = feedback.Id,
                        ReservationRef = reservationRef,
                    },
                    cancellationToken
                );
                await ClearOpenHoldAsync(
                    restaurantId,
                    normalizedKey,
                    reservationRef,
                    cancellationToken
                );

                throw new InvalidOperationException(
                    "Unable to send Recovery SMS."
                );
            }

            var accepted =
                ((RecoveryGuestSmsDeliveryResult.Accepted)delivery).AcceptedSegments;
            var settle = await _smsBilling.SettleAsync(
                new RecoverySmsBillingSettleRequest
                {
                    FeedbackId = feedback.Id,
                    ReservationRef = reservationRef,
                    AcceptedUnits = accepted,
                },
                cancellationToken
            );

            if (settle is RecoverySmsBillingSettleResult.Failed)
            {
                await _smsBilling.ReleaseAsync(
                    new RecoverySmsBillingReleaseRequest
                    {
                        FeedbackId = feedback.Id,
                        ReservationRef = reservationRef,
                    },
                    cancellationToken
                );
                await ClearOpenHoldAsync(
                    restaurantId,
                    normalizedKey,
                    reservationRef,
                    cancellationToken
                );

                throw new InvalidOperationException(
                    "Unable to settle Recovery SMS credits."
                );
            }

            // Lock 05 / ticket 22: settle accepted units, then release unused hold
            // (same close order as CampaignBillingClose).
            var release = await _smsBilling.ReleaseAsync(
                new RecoverySmsBillingReleaseRequest
                {
                    FeedbackId = feedback.Id,
                    ReservationRef = reservationRef,
                },
                cancellationToken
            );
            if (release is RecoverySmsBillingReleaseResult.Failed)
            {
                throw new InvalidOperationException(
                    "Unable to release Recovery SMS credit hold."
                );
            }

            var row = FeedbackGuestResponseComposer.Build(
                feedback,
                FeedbackGuestResponseChannel.Sms,
                FeedbackRecoveryIntent.RespondToGuest,
                content,
                purpose,
                tone,
                includeNotes,
                authorUserId,
                authorDisplayName,
                _clock.GetUtcNow().UtcDateTime
            );
            row.EmailDeliveryStatus = GuestResponseEmailDeliveryStatus.NotApplicable;
            row.BillingReservationRef = reservationRef;

            _context.FeedbackGuestResponses.Add(row);
            await MarkIdempotencyCompletedAsync(
                restaurantId,
                normalizedKey,
                reservationRef,
                row,
                cancellationToken
            );
            await _context.SaveChangesAsync(cancellationToken);

            return BuildResult(feedback, row);
        }

        private static string TrackingKey(string clientKey, string reservationRef)
            => clientKey.Length > 0 ? clientKey : $"anon:{reservationRef}";

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

        private async Task<SendFeedbackGuestResponseResultDto?> TryReplayCompletedAsync(
            int restaurantId,
            string idempotencyKey,
            Feedback feedback,
            CancellationToken cancellationToken
        )
        {
            var cutoff = _clock.GetUtcNow().UtcDateTime - CompletedIdempotencyWindow;
            var completedGuestResponseId = await _context.RecoverySmsSendIdempotencies
                .AsNoTracking()
                .Where(item =>
                    item.RestaurantId == restaurantId
                    && item.IdempotencyKey == idempotencyKey
                    && item.CompletedGuestResponseId != null
                    && item.CompletedAtUtc >= cutoff
                )
                .Select(item => item.CompletedGuestResponseId)
                .FirstOrDefaultAsync(cancellationToken);

            if (completedGuestResponseId == null)
            {
                return null;
            }

            var row = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    guestResponse => guestResponse.Id == completedGuestResponseId,
                    cancellationToken
                );

            if (row == null || row.FeedbackId != feedback.Id)
            {
                return null;
            }

            return BuildResult(feedback, row);
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

        private async Task MarkIdempotencyCompletedAsync(
            int restaurantId,
            string idempotencyKey,
            string reservationRef,
            FeedbackGuestResponse row,
            CancellationToken cancellationToken
        )
        {
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

        private async Task EnsureOperatorBillingAllowsSendAsync(
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

        private static SendFeedbackGuestResponseResultDto BuildResult(
            Feedback feedback,
            FeedbackGuestResponse row
        )
        {
            return new SendFeedbackGuestResponseResultDto
            {
                WorkflowStatus =
                    FeedbackWorkflowStatusMapping.ToWire(feedback.WorkflowStatus),
                NeedsAttention =
                    FeedbackWorkflowStatusMapping.NeedsAttention(feedback),
                GuestResponse = ToItemDto(row),
            };
        }

        private static FeedbackGuestResponseItemDto ToItemDto(
            FeedbackGuestResponse row
        )
        {
            return new FeedbackGuestResponseItemDto
            {
                Id = row.Id,
                Channel = FeedbackGuestResponseMapping.ToWireChannel(row.Channel),
                Intent = FeedbackGuestResponseMapping.ToWireIntent(row.Intent),
                MaskedDestination = row.MaskedDestination,
                Subject = row.Subject,
                Body = row.Body,
                AuthorDisplayName = row.AuthorDisplayName,
                CreatedAt = row.CreatedAt,
            };
        }
    }
}
