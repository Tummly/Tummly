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
            var trimmed = ValidateBody(body);
            var author = await RequireUserAsync(authorUserId, cancellationToken);

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

            return ToItemDto(note);
        }

        public async Task<IReadOnlyList<FeedbackInternalNoteItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        )
        {
            return await _context.FeedbackInternalNotes
                .AsNoTracking()
                .Where(n => n.FeedbackId == feedbackId && n.DeletedAt == null)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Take(MaxListLimit)
                .Select(n => new FeedbackInternalNoteItemDto
                {
                    Id = n.Id,
                    Body = n.Body,
                    AuthorDisplayName = n.AuthorDisplayName,
                    CreatedAt = n.CreatedAt,
                    UpdatedAt = n.UpdatedAt,
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<FeedbackInternalNoteActivityFactDto>> ListActivityFactsForFeedbackAsync(
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
                .Select(n => new FeedbackInternalNoteActivityFactDto
                {
                    Id = n.Id,
                    AuthorDisplayName = n.AuthorDisplayName,
                    CreatedAt = n.CreatedAt,
                    DeletedAt = n.DeletedAt,
                    DeletedByDisplayName = n.DeletedByDisplayName,
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<FeedbackInternalNoteItemDto?> UpdateAsync(
            int feedbackId,
            int noteId,
            int editorUserId,
            string body,
            CancellationToken cancellationToken = default
        )
        {
            var trimmed = ValidateBody(body);
            var editor = await RequireUserAsync(editorUserId, cancellationToken);

            var note = await _context.FeedbackInternalNotes
                .FirstOrDefaultAsync(
                    n =>
                        n.Id == noteId
                        && n.FeedbackId == feedbackId
                        && n.DeletedAt == null,
                    cancellationToken
                );

            if (note == null)
            {
                return null;
            }

            note.Body = trimmed;
            note.UpdatedAt = DateTime.UtcNow;
            note.LastEditedByUserId = editorUserId;
            note.LastEditedByDisplayName = editor.FullName;

            await _context.SaveChangesAsync(cancellationToken);

            return ToItemDto(note);
        }

        public async Task<SoftDeleteFeedbackInternalNoteResultDto?> SoftDeleteAsync(
            int feedbackId,
            int noteId,
            int actorUserId,
            CancellationToken cancellationToken = default
        )
        {
            var actor = await RequireUserAsync(actorUserId, cancellationToken);

            var note = await _context.FeedbackInternalNotes
                .FirstOrDefaultAsync(
                    n =>
                        n.Id == noteId
                        && n.FeedbackId == feedbackId
                        && n.DeletedAt == null,
                    cancellationToken
                );

            if (note == null)
            {
                return null;
            }

            var deletedAt = DateTime.UtcNow;
            note.DeletedAt = deletedAt;
            note.DeletedByUserId = actorUserId;
            note.DeletedByDisplayName = actor.FullName;

            await _context.SaveChangesAsync(cancellationToken);

            return new SoftDeleteFeedbackInternalNoteResultDto
            {
                DeletedAt = deletedAt,
                DeletedByDisplayName = actor.FullName,
            };
        }

        private static string ValidateBody(string body)
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

            return trimmed;
        }

        private async Task<User> RequireUserAsync(
            int userId,
            CancellationToken cancellationToken
        )
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            return user;
        }

        private static FeedbackInternalNoteItemDto ToItemDto(FeedbackInternalNote note)
        {
            return new FeedbackInternalNoteItemDto
            {
                Id = note.Id,
                Body = note.Body,
                AuthorDisplayName = note.AuthorDisplayName,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt,
            };
        }
    }
}
