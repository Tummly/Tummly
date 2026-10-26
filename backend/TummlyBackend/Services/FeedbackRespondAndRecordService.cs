using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class FeedbackRespondAndRecordService
        : IFeedbackRespondAndRecordService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGuestResponseEmailDeliveryWork _emailDelivery;
        private readonly IGuestReplyLock _replyLock;
        private readonly IGuestReplySmsSendStep _smsSend;

        public FeedbackRespondAndRecordService(
            ApplicationDbContext context,
            IGuestResponseEmailDeliveryWork emailDelivery,
            IGuestReplyLock replyLock,
            IGuestReplySmsSendStep smsSend
        )
        {
            _context = context;
            _emailDelivery = emailDelivery;
            _replyLock = replyLock;
            _smsSend = smsSend;
        }

        public async Task<RespondAndRecordInternalActionResultDto?> SendAndRecordAsync(
            int feedbackId,
            int authorUserId,
            FeedbackGuestResponseChannel channel,
            FeedbackInternalActionCategory category,
            string note,
            string? subject,
            string body,
            string? purpose,
            string? tone,
            string? includeNotes,
            CancellationToken cancellationToken = default
        )
        {
            var content = FeedbackGuestResponseComposer.ValidateContent(
                channel,
                subject,
                body
            );

            var trimmedNote = (note ?? string.Empty).Trim();
            if (trimmedNote.Length == 0)
            {
                throw new ArgumentException("Note is required.");
            }

            if (trimmedNote.Length > FeedbackInternalActionsService.MaxNoteLength)
            {
                throw new ArgumentException(
                    $"Note must be at most {FeedbackInternalActionsService.MaxNoteLength} characters."
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

            if (channel == FeedbackGuestResponseChannel.Sms)
            {
                return await SendSmsAndRecordAsync(
                    feedback,
                    authorUserId,
                    author.FullName,
                    category,
                    trimmedNote,
                    content,
                    purpose,
                    tone,
                    includeNotes,
                    cancellationToken
                );
            }

            var createdAt = DateTime.UtcNow;
            var intent = FeedbackRecoveryIntent.RespondAndRecordInternalAction;

            var guestResponse = FeedbackGuestResponseComposer.Build(
                feedback,
                channel,
                intent,
                content,
                purpose,
                tone,
                includeNotes,
                authorUserId,
                author.FullName,
                createdAt
            );

            guestResponse.EmailDeliveryStatus = GuestResponseEmailDeliveryStatus.Pending;
            guestResponse.DeliveryState = GuestReplyDeliveryState.Pending;

            var internalAction = BuildInternalAction(
                feedbackId,
                category,
                trimmedNote,
                authorUserId,
                author.FullName,
                createdAt
            );

            _context.FeedbackGuestResponses.Add(guestResponse);
            _context.FeedbackInternalActions.Add(internalAction);
            await _context.SaveChangesAsync(cancellationToken);

            await _emailDelivery.NotifyAsync(
                guestResponse.Id,
                cancellationToken
            );

            return BuildResult(feedback, guestResponse, internalAction);
        }

        /// <summary>
        /// Reserves, sends and settles like a plain reply SMS. The reply and the
        /// internal action are saved only after the provider accepts the text, so a
        /// refused or failed send leaves neither behind.
        /// </summary>
        private async Task<RespondAndRecordInternalActionResultDto> SendSmsAndRecordAsync(
            Feedback feedback,
            int authorUserId,
            string authorDisplayName,
            FeedbackInternalActionCategory category,
            string trimmedNote,
            FeedbackGuestResponseComposer.ValidatedContent content,
            string? purpose,
            string? tone,
            string? includeNotes,
            CancellationToken cancellationToken
        )
        {
            const FeedbackGuestResponseChannel channel = FeedbackGuestResponseChannel.Sms;
            var intent = FeedbackRecoveryIntent.RespondAndRecordInternalAction;

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

            _smsSend.EnsureBillingLive();
            await _smsSend.EnsureOperatorBillingAllowsSendAsync(
                feedback.RestaurantLocationId,
                cancellationToken
            );

            // Held until the send finishes. Duplicate check and save run under it.
            await using var replyLease = await _replyLock.TryAcquireAsync(
                _context,
                feedback.Id,
                cancellationToken
            ) ?? throw new GuestReplyLockUnavailableException();

            var now = DateTime.UtcNow;
            await GuestReplyRules.EnsureNotDuplicateAsync(
                _context,
                feedback.Id,
                channel,
                content.Body,
                now,
                cancellationToken
            );

            var reservation = await _smsSend.ReserveAsync(
                feedback,
                string.Empty,
                content.Body,
                cancellationToken
            );

            // Save the reply (Pending, no SID) before the provider call, still under
            // the reply lock. If a later save fails after the provider accepted the
            // text, this row stays and the duplicate rule blocks an identical retry.
            var guestResponse = FeedbackGuestResponseComposer.Build(
                feedback,
                channel,
                intent,
                content,
                purpose,
                tone,
                includeNotes,
                authorUserId,
                authorDisplayName,
                now
            );
            guestResponse.EmailDeliveryStatus = GuestResponseEmailDeliveryStatus.NotApplicable;
            guestResponse.DeliveryState = GuestReplyDeliveryState.Pending;
            _context.FeedbackGuestResponses.Add(guestResponse);
            await _smsSend.SavePendingReplyAsync(
                reservation,
                guestResponse,
                cancellationToken
            );

            // The internal action is saved together with the final reply state
            // (Accepted or Unavailable), never before the provider answers. A failed
            // send therefore leaves a Failed reply and no internal action.
            var internalAction = BuildInternalAction(
                feedback.Id,
                category,
                trimmedNote,
                authorUserId,
                authorDisplayName,
                now
            );

            var outcome = await _smsSend.SendAndSettleAsync(
                reservation,
                feedback,
                content.Body,
                cancellationToken
            );

            if (outcome.Kind == GuestReplySmsOutcomeKind.Failed)
            {
                await _smsSend.SaveFailedReplyAsync(guestResponse, now, cancellationToken);
                throw new GuestReplySmsSendFailedException();
            }

            if (outcome.Kind == GuestReplySmsOutcomeKind.Unknown)
            {
                // The provider may have sent the text. Move the saved reply to
                // Unavailable and save the internal action with it. The duplicate
                // rule then blocks a repeat. The save ignores cancellation.
                _smsSend.ApplyUnknownOutcome(guestResponse, reservation, now);
                _context.FeedbackInternalActions.Add(internalAction);
                await _smsSend.SaveRecordedUnknownReplyAsync(reservation, CancellationToken.None);

                throw new GuestReplySmsSendFailedException();
            }

            _smsSend.ApplyAccepted(guestResponse, reservation, outcome.Accepted!, now);
            _context.FeedbackInternalActions.Add(internalAction);
            await _smsSend.SaveAcceptedReplyAsync(
                reservation,
                outcome.Settled,
                guestResponse,
                cancellationToken
            );

            return BuildResult(feedback, guestResponse, internalAction);
        }

        private static FeedbackInternalAction BuildInternalAction(
            int feedbackId,
            FeedbackInternalActionCategory category,
            string trimmedNote,
            int authorUserId,
            string authorDisplayName,
            DateTime createdAt
        )
        {
            return new FeedbackInternalAction
            {
                FeedbackId = feedbackId,
                Category = category,
                CategoryLabel =
                    FeedbackInternalActionMapping.ToCategoryLabel(category),
                Note = trimmedNote,
                Intent = FeedbackRecoveryIntent.RespondAndRecordInternalAction,
                AuthorUserId = authorUserId,
                AuthorDisplayName = authorDisplayName,
                CreatedAt = createdAt,
            };
        }

        private static RespondAndRecordInternalActionResultDto BuildResult(
            Feedback feedback,
            FeedbackGuestResponse guestResponse,
            FeedbackInternalAction internalAction
        )
        {
            return new RespondAndRecordInternalActionResultDto
            {
                WorkflowStatus =
                    FeedbackWorkflowStatusMapping.ToWire(feedback.WorkflowStatus),
                NeedsAttention =
                    FeedbackWorkflowStatusMapping.NeedsAttention(feedback),
                GuestResponse = new FeedbackGuestResponseItemDto
                {
                    Id = guestResponse.Id,
                    Channel = FeedbackGuestResponseMapping.ToWireChannel(
                        guestResponse.Channel
                    ),
                    Intent = FeedbackGuestResponseMapping.ToWireIntent(
                        guestResponse.Intent
                    ),
                    MaskedDestination = guestResponse.MaskedDestination,
                    Subject = guestResponse.Subject,
                    Body = guestResponse.Body,
                    AuthorDisplayName = guestResponse.AuthorDisplayName,
                    CreatedAt = guestResponse.CreatedAt,
                },
                InternalAction = new FeedbackInternalActionItemDto
                {
                    Id = internalAction.Id,
                    Category = FeedbackInternalActionMapping.ToWireCategory(
                        internalAction.Category
                    ),
                    CategoryLabel = internalAction.CategoryLabel,
                    Note = internalAction.Note,
                    Intent = FeedbackInternalActionMapping.ToWireIntent(
                        internalAction.Intent
                    ),
                    AuthorDisplayName = internalAction.AuthorDisplayName,
                    CreatedAt = internalAction.CreatedAt,
                },
            };
        }
    }
}
