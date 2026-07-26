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
            var correction = new FeedbackClassificationCorrection
            {
                FeedbackId = feedbackId,
                FromSentiment = fromSentiment,
                ToSentiment = toSentiment,
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
                AuthorDisplayName = correction.AuthorDisplayName,
                CreatedAt = correction.CreatedAt,
            };
        }
    }
}
