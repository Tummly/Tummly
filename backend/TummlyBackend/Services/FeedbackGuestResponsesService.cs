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

            var row = FeedbackGuestResponseComposer.Build(
                feedback,
                channel,
                intent,
                content,
                purpose,
                tone,
                includeNotes,
                authorUserId,
                author.FullName,
                DateTime.UtcNow
            );

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
