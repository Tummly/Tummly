using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class FeedbackClassificationCorrectionsService
        : IFeedbackClassificationCorrectionsService
    {
        public const int MaxListLimit = 100;

        private readonly ApplicationDbContext _context;

        public FeedbackClassificationCorrectionsService(
            ApplicationDbContext context
        )
        {
            _context = context;
        }

        public async Task<FeedbackClassificationCorrectionItemDto?> RecordAsync(
            int feedbackId,
            int authorUserId,
            FeedbackSentiment fromSentiment,
            FeedbackSentiment toSentiment,
            FeedbackClassificationCorrectionReason reason,
            string? noteBody,
            CancellationToken cancellationToken = default
        )
        {
            ValidateNoteBody(reason, noteBody);

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
            var note = NormalizeNoteBody(noteBody);
            var correction = new FeedbackClassificationCorrection
            {
                FeedbackId = feedbackId,
                FromSentiment = fromSentiment,
                ToSentiment = toSentiment,
                Reason = reason,
                Note = note,
                AuthorUserId = authorUserId,
                AuthorDisplayName = author.FullName,
                CreatedAt = createdAt,
            };

            _context.FeedbackClassificationCorrections.Add(correction);
            await _context.SaveChangesAsync(cancellationToken);

            return ToItemDto(correction);
        }

        public async Task<IReadOnlyList<FeedbackClassificationCorrectionItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        )
        {
            var rows = await _context.FeedbackClassificationCorrections
                .AsNoTracking()
                .Where(c => c.FeedbackId == feedbackId)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .Take(MaxListLimit)
                .ToListAsync(cancellationToken);

            return rows.Select(ToItemDto).ToList();
        }

        private static void ValidateNoteBody(
            FeedbackClassificationCorrectionReason reason,
            string? noteBody
        )
        {
            var hasBody = !string.IsNullOrWhiteSpace(noteBody);

            if (reason == FeedbackClassificationCorrectionReason.Other)
            {
                if (!hasBody)
                {
                    throw new ArgumentException(
                        "Note body is required when reason is other."
                    );
                }

                var trimmed = noteBody!.Trim();
                if (trimmed.Length > FeedbackInternalNotesService.MaxBodyLength)
                {
                    throw new ArgumentException(
                        $"Note body must be at most {FeedbackInternalNotesService.MaxBodyLength} characters."
                    );
                }

                return;
            }

            if (!hasBody)
            {
                return;
            }

            var optionalTrimmed = noteBody!.Trim();
            if (optionalTrimmed.Length > FeedbackInternalNotesService.MaxBodyLength)
            {
                throw new ArgumentException(
                    $"Note body must be at most {FeedbackInternalNotesService.MaxBodyLength} characters."
                );
            }
        }

        private static string? NormalizeNoteBody(string? noteBody)
        {
            if (string.IsNullOrWhiteSpace(noteBody))
            {
                return null;
            }

            return noteBody.Trim();
        }

        private static FeedbackClassificationCorrectionItemDto ToItemDto(
            FeedbackClassificationCorrection correction
        )
        {
            return new FeedbackClassificationCorrectionItemDto
            {
                Id = correction.Id,
                FromSentiment =
                    FeedbackClassificationMapping.ToWireSentiment(
                        correction.FromSentiment
                    ) ?? string.Empty,
                ToSentiment =
                    FeedbackClassificationMapping.ToWireSentiment(
                        correction.ToSentiment
                    ) ?? string.Empty,
                Reason =
                    FeedbackClassificationCorrectionMapping.ToWireReason(
                        correction.Reason
                    ),
                Note = correction.Note,
                AuthorDisplayName = correction.AuthorDisplayName,
                CreatedAt = correction.CreatedAt,
            };
        }
    }
}
