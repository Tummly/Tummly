using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class FeedbackDetectedTagsService : IFeedbackDetectedTagsService
    {
        public const int MaxListLimit = 100;

        private readonly ApplicationDbContext _context;
        private readonly IGuestTaggingService _guestTagging;

        public FeedbackDetectedTagsService(
            ApplicationDbContext context,
            IGuestTaggingService guestTagging
        )
        {
            _context = context;
            _guestTagging = guestTagging;
        }

        public async Task<UpdateFeedbackDetectedTagsResultDto?> UpdateAsync(
            int feedbackId,
            int authorUserId,
            IReadOnlyList<DetectedTag> detectedTags,
            FeedbackSentiment? sentiment,
            bool sentimentProvided,
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

            var feedback = await _context.Feedbacks
                .FirstOrDefaultAsync(f => f.Id == feedbackId, cancellationToken);

            if (feedback == null)
            {
                return null;
            }

            var status = feedback.ClassificationStatus;
            if (
                status != ClassificationStatus.Succeeded
                && status != ClassificationStatus.Failed
            )
            {
                throw new FeedbackDetectedTagsConflictException();
            }

            if (status == ClassificationStatus.Succeeded && sentimentProvided)
            {
                throw new ArgumentException(
                    "Sentiment cannot be changed via detected tags on Succeeded Feedback. Use classification correction."
                );
            }

            if (status == ClassificationStatus.Failed)
            {
                if (!sentimentProvided || sentiment is null)
                {
                    throw new ArgumentException(
                        "Sentiment is required when classification has failed."
                    );
                }
            }

            var fromTags = ParseCurrentTags(feedback);
            var toTags = detectedTags.ToList();
            toTags.Sort();

            var isIdempotentSucceeded =
                status == ClassificationStatus.Succeeded
                && DetectedTagSet.SetsEqual(fromTags, toTags);

            if (isIdempotentSucceeded)
            {
                var unchanged =
                    FeedbackClassificationMapping.ToApiFields(feedback);
                return new UpdateFeedbackDetectedTagsResultDto
                {
                    ClassificationStatus = unchanged.ClassificationStatus,
                    Sentiment = unchanged.Sentiment,
                    DetectedTags = unchanged.DetectedTags,
                    NeedsAttention =
                        FeedbackWorkflowStatusMapping.NeedsAttention(feedback),
                    ClassifiedAt = feedback.ClassifiedAt,
                    ActivityEvent = null,
                };
            }

            var now = DateTime.UtcNow;
            var fromSentiment =
                status == ClassificationStatus.Failed
                    ? null
                    : feedback.Sentiment;
            FeedbackSentiment? toSentiment = null;

            if (status == ClassificationStatus.Failed)
            {
                feedback.ClassificationStatus = ClassificationStatus.Succeeded;
                feedback.Sentiment = sentiment;
                feedback.ClassifiedAt = now;
                feedback.ClassificationClaimedAt = null;
                feedback.ClassificationRetryable = false;
                feedback.ClassificationRetryAfter = null;
                toSentiment = sentiment;
            }

            feedback.DetectedTagsJson =
                FeedbackClassificationMapping.SerializeDetectedTags(toTags);

            var change = new FeedbackDetectedTagsChange
            {
                FeedbackId = feedbackId,
                FromTagsJson =
                    FeedbackClassificationMapping.SerializeDetectedTags(fromTags),
                ToTagsJson =
                    FeedbackClassificationMapping.SerializeDetectedTags(toTags),
                FromSentiment = fromSentiment,
                ToSentiment = toSentiment,
                AuthorUserId = authorUserId,
                AuthorDisplayName = author.FullName,
                CreatedAt = now,
            };

            _context.FeedbackDetectedTagsChanges.Add(change);
            await _context.SaveChangesAsync(cancellationToken);

            await _guestTagging.UnionDetectedTagsFromFeedbackAsync(
                feedback,
                cancellationToken
            );

            var item = ToItemDto(change);
            var classification =
                FeedbackClassificationMapping.ToApiFields(feedback);

            return new UpdateFeedbackDetectedTagsResultDto
            {
                ClassificationStatus = classification.ClassificationStatus,
                Sentiment = classification.Sentiment,
                DetectedTags = classification.DetectedTags,
                NeedsAttention =
                    FeedbackWorkflowStatusMapping.NeedsAttention(feedback),
                ClassifiedAt = feedback.ClassifiedAt,
                ActivityEvent = FeedbackActivityHistory.ToActivityEvent(item),
            };
        }

        public async Task<IReadOnlyList<FeedbackDetectedTagsChangeItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        )
        {
            var rows = await _context.FeedbackDetectedTagsChanges
                .AsNoTracking()
                .Where(c => c.FeedbackId == feedbackId)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .Take(MaxListLimit)
                .ToListAsync(cancellationToken);

            return rows.Select(ToItemDto).ToList();
        }

        private static IReadOnlyList<DetectedTag> ParseCurrentTags(Feedback feedback)
        {
            var keys = FeedbackClassificationMapping.DeserializeDetectedTagKeys(
                feedback.DetectedTagsJson
            );
            if (keys is null || keys.Count == 0)
            {
                return Array.Empty<DetectedTag>();
            }

            if (!DetectedTagSet.TryNormalize(keys, out var tags, out _))
            {
                // Persist may hold legacy noise — treat unparseable as empty
                // for from-set audit rather than blocking the operator edit.
                return Array.Empty<DetectedTag>();
            }

            return tags;
        }

        private static FeedbackDetectedTagsChangeItemDto ToItemDto(
            FeedbackDetectedTagsChange change
        )
        {
            var fromKeys =
                FeedbackClassificationMapping.DeserializeDetectedTagKeys(
                    change.FromTagsJson
                ) ?? Array.Empty<string>();
            var toKeys =
                FeedbackClassificationMapping.DeserializeDetectedTagKeys(
                    change.ToTagsJson
                ) ?? Array.Empty<string>();

            return new FeedbackDetectedTagsChangeItemDto
            {
                Id = change.Id,
                FromDetectedTags = fromKeys,
                ToDetectedTags = toKeys,
                FromSentiment =
                    FeedbackClassificationMapping.ToWireSentiment(
                        change.FromSentiment
                    ),
                ToSentiment =
                    FeedbackClassificationMapping.ToWireSentiment(
                        change.ToSentiment
                    ),
                AuthorDisplayName = change.AuthorDisplayName,
                CreatedAt = change.CreatedAt,
            };
        }
    }
}
