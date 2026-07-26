using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class FeedbackInternalNotesService : IFeedbackInternalNotesService
    {
        public const int MaxListLimit = 100;
        public const int MaxBodyLength = 5000;

        private readonly ApplicationDbContext _context;

        public FeedbackInternalNotesService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FeedbackInternalNoteItemDto?> CreateAsync(
            int feedbackId,
            int authorUserId,
            string body,
            CancellationToken cancellationToken = default
        )
        {
            var trimmed = (body ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                throw new ArgumentException("Note body is required.");
            }

            if (trimmed.Length > MaxBodyLength)
            {
                throw new ArgumentException(
                    $"Note body must be at most {MaxBodyLength} characters."
                );
            }

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

            var authorDisplayName = author.FullName;
            var createdAt = DateTime.UtcNow;
            var note = new FeedbackInternalNote
            {
                FeedbackId = feedbackId,
                Body = trimmed,
                AuthorUserId = authorUserId,
                AuthorDisplayName = authorDisplayName,
                CreatedAt = createdAt,
            };

            _context.FeedbackInternalNotes.Add(note);
            await _context.SaveChangesAsync(cancellationToken);

            return new FeedbackInternalNoteItemDto
            {
                Id = note.Id,
                Body = note.Body,
                AuthorDisplayName = note.AuthorDisplayName,
                CreatedAt = note.CreatedAt,
            };
        }

        public async Task<IReadOnlyList<FeedbackInternalNoteItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        )
        {
            return await _context.FeedbackInternalNotes
                .AsNoTracking()
                .Where(n => n.FeedbackId == feedbackId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Take(MaxListLimit)
                .Select(n => new FeedbackInternalNoteItemDto
                {
                    Id = n.Id,
                    Body = n.Body,
                    AuthorDisplayName = n.AuthorDisplayName,
                    CreatedAt = n.CreatedAt,
                })
                .ToListAsync(cancellationToken);
        }
    }
}
