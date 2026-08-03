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

        public FeedbackRespondAndRecordService(ApplicationDbContext context)
        {
            _context = context;
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
            var trimmedBody = (body ?? string.Empty).Trim();
            if (trimmedBody.Length == 0)
            {
                throw new ArgumentException("Body is required.");
            }

            if (trimmedBody.Length > FeedbackGuestResponsesService.MaxBodyLength)
            {
                throw new ArgumentException(
                    $"Body must be at most {FeedbackGuestResponsesService.MaxBodyLength} characters."
                );
            }

            string? trimmedSubject = null;
            if (channel == FeedbackGuestResponseChannel.Email)
            {
                trimmedSubject = (subject ?? string.Empty).Trim();
                if (trimmedSubject.Length == 0)
                {
                    throw new ArgumentException(
                        "Subject is required for email."
                    );
                }

                if (trimmedSubject.Length
                    > FeedbackGuestResponsesService.MaxSubjectLength)
                {
                    throw new ArgumentException(
                        $"Subject must be at most {FeedbackGuestResponsesService.MaxSubjectLength} characters."
                    );
                }
            }
            else if (!string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentException(
                    "Subject must be omitted for SMS."
                );
            }

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

            EnsureChannelMatchesContact(feedback, channel);

            var createdAt = DateTime.UtcNow;
            var intent = FeedbackRecoveryIntent.RespondAndRecordInternalAction;
            var maskedDestination = FeedbackGuestResponseMapping.MaskDestination(
                feedback.ContactType,
                feedback.GuestContact
            );

            var guestResponse = new FeedbackGuestResponse
            {
                FeedbackId = feedbackId,
                Channel = channel,
                Intent = intent,
                MaskedDestination = maskedDestination,
                Subject = trimmedSubject,
                Body = trimmedBody,
                Purpose = string.IsNullOrWhiteSpace(purpose)
                    ? null
                    : purpose.Trim(),
                Tone = string.IsNullOrWhiteSpace(tone) ? null : tone.Trim(),
                IncludeNotes = string.IsNullOrWhiteSpace(includeNotes)
                    ? null
                    : includeNotes.Trim(),
                AuthorUserId = authorUserId,
                AuthorDisplayName = author.FullName,
                CreatedAt = createdAt,
            };

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

            // Channel delivery is stubbed for MVP — fact write is the send.

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

        private static void EnsureChannelMatchesContact(
            Feedback feedback,
            FeedbackGuestResponseChannel channel
        )
        {
            var hasContact = !string.IsNullOrWhiteSpace(feedback.GuestContact);
            if (!hasContact || feedback.ContactType == ContactType.Unknown)
            {
                throw new ArgumentException(
                    "No contact method available for this Feedback."
                );
            }

            if (
                channel == FeedbackGuestResponseChannel.Email
                && feedback.ContactType != ContactType.Email
            )
            {
                throw new ArgumentException(
                    "Email channel requires an Email contact."
                );
            }

            if (
                channel == FeedbackGuestResponseChannel.Sms
                && feedback.ContactType != ContactType.Phone
            )
            {
                throw new ArgumentException(
                    "SMS channel requires a Phone contact."
                );
            }
        }
    }
}
