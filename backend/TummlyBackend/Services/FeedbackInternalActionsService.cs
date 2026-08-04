using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class FeedbackInternalActionsService : IFeedbackInternalActionsService
    {
        public const int MaxNoteLength = 2000;
        public const int MaxListLimit = 100;

        private readonly ApplicationDbContext _context;

        public FeedbackInternalActionsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RecordFeedbackInternalActionResultDto?> RecordAsync(
            int feedbackId,
            int authorUserId,
            FeedbackInternalActionCategory category,
            string note,
            FeedbackRecoveryIntent intent,
            CancellationToken cancellationToken = default
        )
        {
            var trimmedNote = (note ?? string.Empty).Trim();
            if (trimmedNote.Length == 0)
            {
                throw new ArgumentException("Note is required.");
            }

            if (trimmedNote.Length > MaxNoteLength)
            {
                throw new ArgumentException(
                    $"Note must be at most {MaxNoteLength} characters."
                );
            }

            if (intent != FeedbackRecoveryIntent.RecordInternalActionOnly)
            {
                throw new ArgumentException(
                    "Intent must be record_internal_action_only."
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

            var row = new FeedbackInternalAction
            {
                FeedbackId = feedbackId,
                Category = category,
                CategoryLabel =
                    FeedbackInternalActionMapping.ToCategoryLabel(category),
                Note = trimmedNote,
                Intent = intent,
                AuthorUserId = authorUserId,
                AuthorDisplayName = author.FullName,
                CreatedAt = DateTime.UtcNow,
            };

            _context.FeedbackInternalActions.Add(row);
            await _context.SaveChangesAsync(cancellationToken);

            return new RecordFeedbackInternalActionResultDto
            {
                WorkflowStatus =
                    FeedbackWorkflowStatusMapping.ToWire(feedback.WorkflowStatus),
                NeedsAttention =
                    FeedbackWorkflowStatusMapping.NeedsAttention(feedback),
                InternalAction = ToItemDto(row),
            };
        }

        public async Task<IReadOnlyList<FeedbackInternalActionItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        )
        {
            var rows = await _context.FeedbackInternalActions
                .AsNoTracking()
                .Where(r => r.FeedbackId == feedbackId)
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id)
                .Take(MaxListLimit)
                .ToListAsync(cancellationToken);

            return rows.Select(ToItemDto).ToList();
        }

        private static FeedbackInternalActionItemDto ToItemDto(
            FeedbackInternalAction row
        )
        {
            return new FeedbackInternalActionItemDto
            {
                Id = row.Id,
                Category = FeedbackInternalActionMapping.ToWireCategory(row.Category),
                CategoryLabel = row.CategoryLabel,
                Note = row.Note,
                Intent = FeedbackInternalActionMapping.ToWireIntent(row.Intent),
                AuthorDisplayName = row.AuthorDisplayName,
                CreatedAt = row.CreatedAt,
            };
        }
    }
}
