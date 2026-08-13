using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class AssistantFeedbackRetrieve : IAssistantFeedbackRetrieve
    {
        public const int MaxSampleRows = 100;

        private readonly ApplicationDbContext _context;

        public AssistantFeedbackRetrieve(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AssistantFeedbackRetrieveResult> RetrieveAsync(
            int ownedLocationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var window = _context.Feedbacks
                    .AsNoTracking()
                    .Where(feedback =>
                        feedback.RestaurantLocationId == ownedLocationId
                        && feedback.CreatedAt >= fromUtc
                        && feedback.CreatedAt < toUtc
                    );

                var snapshot = await window
                    .Select(feedback => new
                    {
                        feedback.ClassificationStatus,
                        feedback.Sentiment,
                        feedback.WorkflowStatus,
                        feedback.DetectedTagsJson,
                    })
                    .ToListAsync(cancellationToken);

                var rows = await window
                    .OrderByDescending(feedback => feedback.CreatedAt)
                    .ThenByDescending(feedback => feedback.Id)
                    .Take(MaxSampleRows)
                    .Select(feedback => new
                    {
                        feedback.Id,
                        feedback.CreatedAt,
                        feedback.GuestName,
                        feedback.Sentiment,
                        feedback.ClassificationStatus,
                        feedback.DetectedTagsJson,
                        feedback.WorkflowStatus,
                        feedback.Comment,
                        feedback.ContactType,
                    })
                    .ToListAsync(cancellationToken);

                var tagCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                var succeededPositive = 0;
                var succeededNeutral = 0;
                var succeededNegative = 0;
                var needsAttention = 0;

                foreach (var row in snapshot)
                {
                    if (row.ClassificationStatus == ClassificationStatus.Succeeded
                        && row.Sentiment == FeedbackSentiment.Negative
                        && row.WorkflowStatus != FeedbackWorkflowStatus.Resolved)
                    {
                        needsAttention++;
                    }

                    if (row.ClassificationStatus == ClassificationStatus.Succeeded)
                    {
                        switch (row.Sentiment)
                        {
                            case FeedbackSentiment.Positive:
                                succeededPositive++;
                                break;
                            case FeedbackSentiment.Neutral:
                                succeededNeutral++;
                                break;
                            case FeedbackSentiment.Negative:
                                succeededNegative++;
                                break;
                        }

                        foreach (var tag in ParseTags(row.DetectedTagsJson))
                        {
                            tagCounts[tag] = tagCounts.GetValueOrDefault(tag) + 1;
                        }
                    }
                }

                var evidenceRows = rows
                    .Select(row =>
                    {
                        var succeeded =
                            row.ClassificationStatus == ClassificationStatus.Succeeded;
                        var tags = succeeded ? ParseTags(row.DetectedTagsJson) : [];
                        var needs =
                            succeeded
                            && row.Sentiment == FeedbackSentiment.Negative
                            && row.WorkflowStatus != FeedbackWorkflowStatus.Resolved;

                        return new AssistantFeedbackEvidenceRow(
                            row.Id,
                            row.CreatedAt,
                            row.GuestName,
                            succeeded ? row.Sentiment?.ToString().ToLowerInvariant() : null,
                            row.ClassificationStatus.ToString(),
                            tags,
                            row.WorkflowStatus.ToString(),
                            needs,
                            null,
                            row.ContactType.ToString(),
                            Excerpt(row.Comment),
                            FeedbackReference(row.Id)
                        );
                    })
                    .ToList();

                return new AssistantFeedbackRetrieveResult.Ok(
                    new AssistantFeedbackEvidence(
                        snapshot.Count,
                        evidenceRows.Count,
                        succeededPositive,
                        succeededNeutral,
                        succeededNegative,
                        needsAttention,
                        tagCounts
                            .OrderByDescending(pair => pair.Value)
                            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                            .Select(pair => new AssistantFeedbackTagCount(pair.Key, pair.Value))
                            .ToList(),
                        evidenceRows
                    )
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return new AssistantFeedbackRetrieveResult.Failed();
            }
        }

        private static IReadOnlyList<string> ParseTags(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static string Excerpt(string comment)
        {
            var trimmed = comment.Trim();
            if (trimmed.Length <= 80)
            {
                return trimmed;
            }

            return trimmed[..80] + "…";
        }

        private static string FeedbackReference(int id)
            => $"FDB-{id.ToString().PadLeft(6, '0')}";
    }
}
