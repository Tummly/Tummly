using TummlyBackend.DTOs.Assistant;
using TummlyBackend.DTOs.OperatorHome;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Server-owned Attention Retrieve copy. Reuses Home honesty strings.
    /// Does not rewrite Home Azure recommendation or Weekly brief copy.
    /// </summary>
    public static class AssistantAttentionCopy
    {
        public const string NeedsAttentionEmpty =
            "Nothing needs attention right now.";

        public const string RecommendationNone =
            "A recommended action will appear once there is enough guest activity.";

        public const string WeeklyBriefEmptyTitle =
            "Your first weekly brief will be ready on Monday";

        public const string WeeklyBriefEmptyHelper =
            "It will summarise guest activity, feedback themes, offers and campaigns.";

        public const string NeedsAttentionLoadError =
            "Could not load Needs attention. Please try again.";

        public const string RecommendationLoadError =
            "Could not load a recommendation. Please try again.";

        public const string WeeklyBriefLoadError =
            "Could not load your weekly brief. Please try again.";

        public const string RetryThisSend = "Retry this send.";

        public static string NeedsAttentionClock(string locationName)
            => $"Needs attention is the now-queue at {locationName}. It is not the Reporting period.";

        public static string RecommendedNextStepClock(
            string locationName,
            string periodPhrase
        )
            => $"Recommended next step uses the saved Reporting period ({periodPhrase}) at {locationName}.";

        public static string WeeklyBriefClock(string locationName, string weekKey)
            => $"Weekly brief covers the closed prior week ({weekKey}) at {locationName}.";

        public static string MixClock(
            string locationName,
            string periodPhrase
        )
            => $"{NeedsAttentionClock(locationName)} {RecommendedNextStepClock(locationName, periodPhrase)}";

        public static string NeedsAttentionTitle(int itemCount, string locationName)
        {
            if (itemCount <= 0)
            {
                return $"Nothing needs attention at {locationName}";
            }

            return itemCount == 1
                ? $"1 item needs attention at {locationName}"
                : $"{itemCount} items need attention at {locationName}";
        }

        public static string NeedsAttentionBody(
            string locationName,
            IReadOnlyList<AssistantHomeNeedsAttentionItem> items
        )
        {
            var data = items.Count == 0
                ? NeedsAttentionEmpty
                : string.Join("\n", items.Select(FormatItem));
            return $"{NeedsAttentionClock(locationName)}\n\n## Data\n{data}";
        }

        public static string NeedsAttentionErrorTitle(string locationName)
            => $"Needs attention at {locationName}";

        public static string NeedsAttentionErrorBody(string locationName)
            => $"{NeedsAttentionClock(locationName)}\n\n## Data\n"
                + $"{NeedsAttentionLoadError} {RetryThisSend}";

        public static string RecommendedNextStepBody(
            string locationName,
            string periodPhrase,
            HomeRecommendationDto recommendation
        )
        {
            var clock = RecommendedNextStepClock(locationName, periodPhrase);
            if (IsNone(recommendation))
            {
                return $"{clock}\n\n## Data\n- **Type:** none"
                    + $"\n\n## Recommendation\n{RecommendationNone}";
            }

            return $"{clock}\n\n## Data\n{FormatRecommendationData(recommendation)}"
                + $"\n\n## Recommendation\n{FormatRecommendationCard(recommendation)}";
        }

        public static string RecommendedNextStepErrorBody(
            string locationName,
            string periodPhrase
        )
            => $"{RecommendedNextStepClock(locationName, periodPhrase)}\n\n"
                + $"## Data\n{RecommendationLoadError}\n\n"
                + $"## Recommendation\n{RetryThisSend}";

        public static string WeeklyBriefBodyText(
            string locationName,
            string weekKey,
            WeeklyBriefBody body
        )
        {
            var data = FormatWeeklyBriefData(body);
            return $"{WeeklyBriefClock(locationName, weekKey)}\n\n## Data\n{data}";
        }

        public static string WeeklyBriefEmptyBody(string locationName, string weekKey)
            => $"{WeeklyBriefClock(locationName, weekKey)}\n\n## Data\n"
                + $"{WeeklyBriefEmptyTitle}\n{WeeklyBriefEmptyHelper}";

        public static string WeeklyBriefErrorBody(string locationName, string weekKey)
            => $"{WeeklyBriefClock(locationName, weekKey)}\n\n## Data\n"
                + $"{WeeklyBriefLoadError} {RetryThisSend}";

        public static string MixBody(
            string locationName,
            string periodPhrase,
            IReadOnlyList<AssistantHomeNeedsAttentionItem>? queueItems,
            bool queueError,
            HomeRecommendationDto? recommendation,
            bool recommendationError
        )
        {
            var clock = MixClock(locationName, periodPhrase);
            var queueReady = !queueError && queueItems is { Count: > 0 };
            var recReady = !recommendationError
                && recommendation is not null
                && !IsNone(recommendation);

            if (queueError && recommendationError)
            {
                return $"{clock}\n\n## Data\n{NeedsAttentionLoadError}\n\n"
                    + $"## Recommendation\n{RecommendationLoadError} {RetryThisSend}";
            }

            if (queueReady && recReady)
            {
                return $"{clock}\n\n## Data\n"
                    + string.Join("\n", queueItems!.Select(FormatItem))
                    + $"\n\n## Recommendation\n{FormatRecommendationCard(recommendation!)}";
            }

            if (queueReady)
            {
                var emptyName = recommendationError
                    ? RecommendationLoadError
                    : RecommendationNone;
                return $"{clock}\n\n## Data\n"
                    + string.Join("\n", queueItems!.Select(FormatItem))
                    + $"\n\n## Recommendation\n{emptyName}";
            }

            if (recReady)
            {
                return $"{clock}\n\n## Data\n{NeedsAttentionEmpty}\n\n"
                    + $"## Recommendation\n{FormatRecommendationCard(recommendation!)}";
            }

            if (queueError)
            {
                return $"{clock}\n\n## Data\n{NeedsAttentionLoadError}\n\n"
                    + $"## Recommendation\n{RecommendationNone}";
            }

            if (recommendationError)
            {
                return $"{clock}\n\n## Data\n{NeedsAttentionEmpty}\n\n"
                    + $"## Recommendation\n{RecommendationLoadError} {RetryThisSend}";
            }

            return $"{clock}\n\n## Data\n{NeedsAttentionEmpty}\n\n"
                + $"## Recommendation\n{RecommendationNone}";
        }

        public static string MixTitle(
            string locationName,
            IReadOnlyList<AssistantHomeNeedsAttentionItem>? queueItems,
            HomeRecommendationDto? recommendation
        )
        {
            if (queueItems is { Count: > 0 })
            {
                return NeedsAttentionTitle(queueItems.Count, locationName);
            }

            if (recommendation is not null && !IsNone(recommendation)
                && !string.IsNullOrWhiteSpace(recommendation.Title))
            {
                return recommendation.Title!;
            }

            return $"Attention at {locationName}";
        }

        public static bool IsNone(HomeRecommendationDto recommendation)
            => string.Equals(recommendation.Type, "none", StringComparison.Ordinal);

        private static string FormatItem(AssistantHomeNeedsAttentionItem item)
            => $"- **{item.Title}** — {item.Body} ({item.MetaLine})";

        private static string FormatRecommendationData(HomeRecommendationDto recommendation)
        {
            var lines = new List<string>
            {
                $"- **Type:** {recommendation.Type}",
            };
            if (!string.IsNullOrWhiteSpace(recommendation.Title))
            {
                lines.Add($"- **Title:** {recommendation.Title}");
            }

            if (!string.IsNullOrWhiteSpace(recommendation.Opportunity))
            {
                lines.Add($"- **Opportunity:** {recommendation.Opportunity}");
            }

            if (!string.IsNullOrWhiteSpace(recommendation.EligibleAudience))
            {
                lines.Add($"- **Eligible audience:** {recommendation.EligibleAudience}");
            }

            if (!string.IsNullOrWhiteSpace(recommendation.SuggestedChannel))
            {
                lines.Add($"- **Suggested channel:** {recommendation.SuggestedChannel}");
            }

            if (!string.IsNullOrWhiteSpace(recommendation.EstimatedUsage))
            {
                lines.Add($"- **Estimated usage:** {recommendation.EstimatedUsage}");
            }

            foreach (var bullet in recommendation.WhyBullets ?? [])
            {
                if (!string.IsNullOrWhiteSpace(bullet))
                {
                    lines.Add($"- **Why:** {bullet}");
                }
            }

            return string.Join("\n", lines);
        }

        private static string FormatRecommendationCard(HomeRecommendationDto recommendation)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(recommendation.Title))
            {
                parts.Add(recommendation.Title!);
            }

            if (!string.IsNullOrWhiteSpace(recommendation.Opportunity))
            {
                parts.Add(recommendation.Opportunity!);
            }

            foreach (var bullet in recommendation.WhyBullets ?? [])
            {
                if (!string.IsNullOrWhiteSpace(bullet))
                {
                    parts.Add(bullet);
                }
            }

            return parts.Count == 0 ? recommendation.Type : string.Join("\n", parts);
        }

        private static string FormatWeeklyBriefData(WeeklyBriefBody body)
        {
            var lines = new List<string>
            {
                FormatSection("Capture", body.Capture),
                FormatSection("Feedback", body.Feedback),
                FormatSection("Offers", body.Offers),
                FormatSection("Campaigns", body.Campaigns),
            };
            if (body.WatchNext.Count > 0)
            {
                lines.Add("**Watch next:** " + string.Join("; ", body.WatchNext));
            }

            return string.Join("\n", lines);
        }

        private static string FormatSection(string label, WeeklyBriefSection section)
            => $"- **{label}:** {section.Summary}";
    }
}
