using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class FeedbackWorkflowStatusChangesService
        : IFeedbackWorkflowStatusChangesService
    {
        public const int MaxListLimit = 100;

        private readonly ApplicationDbContext _context;

        public FeedbackWorkflowStatusChangesService(
            ApplicationDbContext context
        )
        {
            _context = context;
        }

        public async Task<FeedbackWorkflowStatusChangeItemDto?> RecordAsync(
            int feedbackId,
            int authorUserId,
            FeedbackWorkflowStatus fromStatus,
            FeedbackWorkflowStatus toStatus,
            CancellationToken cancellationToken = default
        )
        {
            var author = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == authorUserId, cancellationToken);

            if (author == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            var exists = await _context.Feedbacks
                .AsNoTracking()
                .AnyAsync(f => f.Id == feedbackId, cancellationToken);

            if (!exists)
            {
                return null;
            }

            var createdAt = DateTime.UtcNow;
            var change = new FeedbackWorkflowStatusChange
            {
                FeedbackId = feedbackId,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                AuthorUserId = authorUserId,
                AuthorDisplayName = author.FullName,
                CreatedAt = createdAt,
            };

            _context.FeedbackWorkflowStatusChanges.Add(change);
            await _context.SaveChangesAsync(cancellationToken);

            return ToItemDto(change);
        }

        public async Task<IReadOnlyList<FeedbackWorkflowStatusChangeItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        )
        {
            var rows = await _context.FeedbackWorkflowStatusChanges
                .AsNoTracking()
                .Where(c => c.FeedbackId == feedbackId)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .Take(MaxListLimit)
                .ToListAsync(cancellationToken);

            return rows.Select(ToItemDto).ToList();
        }

        private static FeedbackWorkflowStatusChangeItemDto ToItemDto(
            FeedbackWorkflowStatusChange change
        )
        {
            return new FeedbackWorkflowStatusChangeItemDto
            {
                Id = change.Id,
                FromWorkflowStatus =
                    FeedbackWorkflowStatusMapping.ToWire(change.FromStatus),
                ToWorkflowStatus =
                    FeedbackWorkflowStatusMapping.ToWire(change.ToStatus),
                AuthorDisplayName = change.AuthorDisplayName,
                CreatedAt = change.CreatedAt,
            };
        }
    }
}
