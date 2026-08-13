using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class AssistantActionCatalog
    {
        public const int MaxActions = 3;

        public static readonly string[] CatalogOrder =
        [
            "view-feedback-set",
            "prepare-recovery",
            "view-campaigns",
            "view-offers",
            "view-offer",
            "view-guests",
            "view-guest",
            "view-capture",
        ];

        private static readonly HashSet<string> EvidenceTypes = new(StringComparer.Ordinal)
        {
            "view-feedback-set",
            "view-offer",
            "view-guests",
            "view-guest",
            "view-capture",
        };

        public static IReadOnlyList<AssistantActionDto> Validate(
            IEnumerable<AssistantActionDto>? proposed,
            AssistantMessageClass answerClass,
            AssistantFeedbackEvidence evidence
        )
        {
            if (answerClass != AssistantMessageClass.Grounded || evidence.IsEmpty)
            {
                return [];
            }

            var byType = new Dictionary<string, AssistantActionDto>(StringComparer.Ordinal);
            foreach (var raw in proposed ?? [])
            {
                if (raw.Type is null || !CatalogOrder.Contains(raw.Type))
                {
                    continue;
                }

                if (byType.ContainsKey(raw.Type))
                {
                    continue;
                }

                var normalized = Normalize(raw, evidence);
                if (normalized is null)
                {
                    continue;
                }

                byType[raw.Type] = normalized;
            }

            if (byType.TryGetValue("view-feedback-set", out var set)
                && byType.ContainsKey("prepare-recovery")
                && SameInboxFilter(set))
            {
                byType.Remove("prepare-recovery");
            }

            return CatalogOrder
                .Where(byType.ContainsKey)
                .Select(type => byType[type])
                .Take(MaxActions)
                .ToList();
        }

        public static IReadOnlyList<AssistantActionDto> DefaultFeedbackActions(
            string userMessage,
            AssistantFeedbackEvidence evidence
        )
        {
            if (evidence.IsEmpty)
            {
                return [];
            }

            var proposed = new List<AssistantActionDto>
            {
                new()
                {
                    Type = "view-feedback-set",
                    Count = evidence.TotalCount,
                },
            };

            if (evidence.NeedsAttention > 0)
            {
                proposed.Add(new AssistantActionDto { Type = "prepare-recovery" });
            }

            var lower = userMessage.ToLowerInvariant();
            if (lower.Contains("campaign", StringComparison.Ordinal))
            {
                proposed.Add(new AssistantActionDto { Type = "view-campaigns" });
            }

            if (lower.Contains("offer", StringComparison.Ordinal))
            {
                proposed.Add(new AssistantActionDto { Type = "view-offers" });
            }

            return Validate(proposed, AssistantMessageClass.Grounded, evidence);
        }

        public static string LabelFor(AssistantActionDto action)
        {
            return action.Type switch
            {
                "view-feedback-set" => action.Count == 1
                    ? "View 1 feedback item"
                    : $"View {action.Count ?? 0} feedback items",
                "prepare-recovery" => "Prepare recovery responses",
                "view-campaigns" => "Open Campaigns",
                "view-offers" => "Open Offers",
                "view-offer" => "View offer",
                "view-guests" => "View guests",
                "view-guest" => "View guest",
                "view-capture" => "View Capture",
                _ => action.Type,
            };
        }

        private static AssistantActionDto? Normalize(
            AssistantActionDto raw,
            AssistantFeedbackEvidence evidence
        )
        {
            if (EvidenceTypes.Contains(raw.Type) && evidence.IsEmpty)
            {
                return null;
            }

            if (raw.Type == "view-feedback-set" && evidence.TotalCount == 0)
            {
                return null;
            }

            var tab = NormalizeTab(raw.Tab);
            var sentiment = NormalizeSentiment(raw.Sentiment);
            var detectedTag = string.IsNullOrWhiteSpace(raw.DetectedTag)
                ? null
                : raw.DetectedTag.Trim();

            var filterCount = 0;
            if (tab is not null)
            {
                filterCount++;
            }

            if (sentiment is not null)
            {
                filterCount++;
            }

            if (detectedTag is not null)
            {
                filterCount++;
            }

            if (raw.Type == "view-feedback-set" && filterCount > 1)
            {
                tab = null;
                sentiment = null;
                detectedTag = LargestOrFirstTag(evidence, detectedTag);
            }

            if (raw.Type == "prepare-recovery")
            {
                if (evidence.NeedsAttention == 0)
                {
                    return null;
                }

                tab = "needs-attention";
                sentiment = null;
                detectedTag = null;
            }

            var count = raw.Type == "view-feedback-set"
                ? evidence.TotalCount
                : raw.Count;

            var action = new AssistantActionDto
            {
                Type = raw.Type,
                Tab = raw.Type is "view-feedback-set" or "prepare-recovery" ? tab : null,
                Sentiment = raw.Type == "view-feedback-set" ? sentiment : null,
                DetectedTag = raw.Type == "view-feedback-set" ? detectedTag : null,
                Count = count,
                OfferId = raw.Type == "view-offer" ? raw.OfferId : null,
                GuestId = raw.Type == "view-guest" ? raw.GuestId : null,
                SmartGroup = raw.Type == "view-guests" ? raw.SmartGroup : null,
                MarketingEligible = raw.Type == "view-guests" ? raw.MarketingEligible : null,
            };
            action.Label = LabelFor(action);
            return action;
        }

        private static bool SameInboxFilter(AssistantActionDto set)
            => set.Tab == "needs-attention"
                && set.Sentiment is null
                && set.DetectedTag is null;

        private static string? NormalizeTab(string? tab)
        {
            var key = tab?.Trim().ToLowerInvariant();
            return key is "all" or "needs-attention" or "new" or "in-progress" or "resolved"
                ? key
                : null;
        }

        private static string? NormalizeSentiment(string? sentiment)
        {
            var key = sentiment?.Trim().ToLowerInvariant();
            return key is "positive" or "neutral" or "negative" ? key : null;
        }

        private static string? LargestOrFirstTag(
            AssistantFeedbackEvidence evidence,
            string? preferred
        )
        {
            if (preferred is not null
                && evidence.TagCounts.Any(tag =>
                    tag.Tag.Equals(preferred, StringComparison.OrdinalIgnoreCase)))
            {
                return preferred;
            }

            return evidence.TagCounts
                .OrderByDescending(tag => tag.Count)
                .ThenBy(tag => tag.Tag, StringComparer.Ordinal)
                .Select(tag => tag.Tag)
                .FirstOrDefault();
        }
    }
}
