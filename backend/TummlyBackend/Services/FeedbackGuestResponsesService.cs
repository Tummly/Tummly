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
        public const int MaxBodyLength = 5000;
        public const int MaxSubjectLength = 300;
        public const int MaxListLimit = 100;

        private readonly ApplicationDbContext _context;

        public FeedbackGuestResponsesService(ApplicationDbContext context)
        {
            _context = context;
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
            CancellationToken cancellationToken = default
        )
        {
            var trimmedBody = (body ?? string.Empty).Trim();
            if (trimmedBody.Length == 0)
            {
                throw new ArgumentException("Body is required.");
            }

            if (trimmedBody.Length > MaxBodyLength)
            {
                throw new ArgumentException(
                    $"Body must be at most {MaxBodyLength} characters."
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

                if (trimmedSubject.Length > MaxSubjectLength)
                {
                    throw new ArgumentException(
                        $"Subject must be at most {MaxSubjectLength} characters."
                    );
                }
            }
            else if (!string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentException(
                    "Subject must be omitted for SMS."
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

            var maskedDestination = FeedbackGuestResponseMapping.MaskDestination(
                feedback.ContactType,
                feedback.GuestContact
            );

            var row = new FeedbackGuestResponse
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
                CreatedAt = DateTime.UtcNow,
            };

            _context.FeedbackGuestResponses.Add(row);
            await _context.SaveChangesAsync(cancellationToken);

            // Channel delivery is stubbed for MVP — fact write is the send.

            return new SendFeedbackGuestResponseResultDto
            {
                WorkflowStatus =
                    FeedbackWorkflowStatusMapping.ToWire(feedback.WorkflowStatus),
                NeedsAttention =
                    FeedbackWorkflowStatusMapping.NeedsAttention(feedback),
                GuestResponse = ToItemDto(row),
            };
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
