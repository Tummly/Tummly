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
        private readonly IGuestReplySmsSendStep _smsSend;
        private readonly TimeProvider _clock;
        private readonly IGuestReplyLock _replyLock;

        public FeedbackGuestResponsesService(
            ApplicationDbContext context,
            IGuestResponseEmailDeliveryWork emailDelivery,
            IGuestReplySmsSendStep smsSend,
            TimeProvider clock,
            IGuestReplyLock replyLock
        )
        {
            _context = context;
            _emailDelivery = emailDelivery;
            _smsSend = smsSend;
            _clock = clock;
            _replyLock = replyLock;
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

            if (
                !await ServiceReplyEligibility.IsAllowedAsync(
                    _context,
                    feedback,
                    channel,
                    cancellationToken
                )
            )
            {
                throw new ArgumentException(
                    ServiceReplyEligibility.NotAllowedMessage
                );
            }

            await _smsSend.EnsureOperatorBillingAllowsSendAsync(
                feedback.RestaurantLocationId,
                cancellationToken
            );

            // Held until the send finishes. Duplicate checks and the save run
            // under it, so two identical replies cannot both pass.
            await using var replyLease = await AcquireReplyLockAsync(
                feedback.Id,
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

        private async Task<IAsyncDisposable> AcquireReplyLockAsync(
            int feedbackId,
            CancellationToken cancellationToken
        )
        {
            var lease = await _replyLock.TryAcquireAsync(
                _context,
                feedbackId,
                cancellationToken
            );

            return lease ?? throw new GuestReplyLockUnavailableException();
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
            await GuestReplyRules.EnsureNotDuplicateAsync(
                _context,
                feedback.Id,
                FeedbackGuestResponseChannel.Email,
                content.Body,
                _clock.GetUtcNow().UtcDateTime,
                cancellationToken
            );

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
            row.DeliveryState = GuestReplyDeliveryState.Pending;

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
            _smsSend.EnsureBillingLive();

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

            await GuestReplyRules.EnsureNotDuplicateAsync(
                _context,
                feedback.Id,
                FeedbackGuestResponseChannel.Sms,
                content.Body,
                _clock.GetUtcNow().UtcDateTime,
                cancellationToken
            );

            var reservation = await _smsSend.ReserveAsync(
                feedback,
                normalizedKey,
                content.Body,
                cancellationToken
            );

            var outcome = await _smsSend.SendAndSettleAsync(
                reservation,
                feedback,
                content.Body,
                cancellationToken
            );

            if (outcome.Kind == GuestReplySmsOutcomeKind.Failed)
            {
                // The provider refused the text. No reply row is saved, so there
                // is no state to move to Failed.
                throw new GuestReplySmsSendFailedException();
            }

            var now = _clock.GetUtcNow().UtcDateTime;
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
                now
            );
            row.EmailDeliveryStatus = GuestResponseEmailDeliveryStatus.NotApplicable;

            if (outcome.Kind == GuestReplySmsOutcomeKind.Unknown)
            {
                // The provider may have sent the text. Record the reply as
                // Unavailable, keep the hold for the sweeper, and block a repeat
                // with the duplicate rule. The save ignores cancellation so the
                // record is kept.
                _smsSend.ApplyUnknownOutcome(row, reservation, now);
                _context.FeedbackGuestResponses.Add(row);
                await _context.SaveChangesAsync(CancellationToken.None);

                throw new GuestReplySmsSendFailedException();
            }

            _smsSend.ApplyAccepted(row, reservation, outcome.Accepted!, now);
            _context.FeedbackGuestResponses.Add(row);
            if (outcome.Settled)
            {
                await _smsSend.CompleteAsync(reservation, row, cancellationToken);
            }
            await _context.SaveChangesAsync(cancellationToken);

            return BuildResult(feedback, row);
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
