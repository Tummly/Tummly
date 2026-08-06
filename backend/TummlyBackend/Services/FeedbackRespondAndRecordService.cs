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

        public FeedbackRespondAndRecordService(
            ApplicationDbContext context,
            IGuestResponseEmailDeliveryWork emailDelivery
        )
        {
            _context = context;
            _emailDelivery = emailDelivery;
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

            guestResponse.EmailDeliveryStatus =
                channel == FeedbackGuestResponseChannel.Email
                    ? GuestResponseEmailDeliveryStatus.Pending
                    : GuestResponseEmailDeliveryStatus.NotApplicable;

            var internalAction = new FeedbackInternalAction
            {
                FeedbackId = feedbackId,
                Category = category,
                CategoryLabel =
                    FeedbackInternalActionMapping.ToCategoryLabel(category),
                Note = trimmedNote,
                Intent = intent,
                AuthorUserId = authorUserId,
                AuthorDisplayName = author.FullName,
                CreatedAt = createdAt,
            };

            _context.FeedbackGuestResponses.Add(guestResponse);
            _context.FeedbackInternalActions.Add(internalAction);
            await _context.SaveChangesAsync(cancellationToken);

            if (
                guestResponse.EmailDeliveryStatus
                    == GuestResponseEmailDeliveryStatus.Pending
            )
            {
                await _emailDelivery.NotifyAsync(
                    guestResponse.Id,
                    cancellationToken
                );
            }

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
