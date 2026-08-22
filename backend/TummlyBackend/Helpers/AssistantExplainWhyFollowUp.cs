using System.Text.RegularExpressions;
using TummlyBackend.DTOs.Assistant;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Closed explain-why follow-up needles. Stay Retrieve. Run after Help
    /// Centre refuse and before normal classify.
    /// </summary>
    public enum AssistantExplainWhyKind
    {
        None = 0,
        Recommendation = 1,
        Results = 2,
        NeedsAttention = 3,
    }

    public enum AssistantExplainWhyPriorPath
    {
        None = 0,
        NeedsAttention = 1,
        RecommendedNextStep = 2,
        Mix = 3,
        WeeklyBrief = 4,
        ProductExpert = 5,
        CombinedCreate = 6,
        DomainRetrieve = 7,
    }

    public static class AssistantExplainWhyFollowUp
    {
        public const string DataHeading = "## Data";

        public const string InterpretationHeading = "## Interpretation";

        public const string RecommendationHeading = "## Recommendation";

        private static readonly string[] RecommendationNeedles =
        [
            "why are you recommending",
            "why did you recommend",
            "explain this recommendation",
        ];

        private static readonly string[] ResultsNeedles =
        [
            "explain these results",
        ];

        private static readonly string[] NeedsAttentionNeedles =
        [
            "why does this need attention",
            "explain what needs attention",
        ];

        private static readonly (string Needle, string? PresetId)[] PeriodNeedles =
        [
            ("last 7 days", "last7"),
            ("last seven days", "last7"),
            ("last7", "last7"),
            ("last 30 days", "last30"),
            ("last thirty days", "last30"),
            ("last30", "last30"),
            ("this month", "thisMonth"),
            ("thismonth", "thisMonth"),
            ("last week", null),
            ("this week", null),
            ("yesterday", null),
            ("last month", null),
        ];

        public static AssistantExplainWhyKind Detect(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            if (lower.Length == 0)
            {
                return AssistantExplainWhyKind.None;
            }

            if (ContainsAny(lower, NeedsAttentionNeedles))
            {
                return AssistantExplainWhyKind.NeedsAttention;
            }

            if (ContainsAny(lower, RecommendationNeedles))
            {
                return AssistantExplainWhyKind.Recommendation;
            }

            if (ContainsAny(lower, ResultsNeedles))
            {
                return AssistantExplainWhyKind.Results;
            }

            return AssistantExplainWhyKind.None;
        }

        public static bool IsExplainWhyFollowUp(string message)
            => Detect(message) != AssistantExplainWhyKind.None;

        public static bool HasDataLayer(string body)
            => ContainsHeading(body, DataHeading);

        public static bool HasInterpretationLayer(string body)
            => ContainsHeading(body, InterpretationHeading);

        public static bool HasRecommendationLayer(string body)
            => ContainsHeading(body, RecommendationHeading);

        public static AssistantExplainWhyPriorPath InferPriorPath(
            string priorUserMessage,
            string priorAssistantTitle,
            string priorAssistantBody
        )
        {
            var attention = AssistantAttentionAsk.Detect(priorUserMessage);
            if (attention == AssistantAttentionSurface.NeedsAttention)
            {
                return AssistantExplainWhyPriorPath.NeedsAttention;
            }

            if (attention == AssistantAttentionSurface.RecommendedNextStep)
            {
                return AssistantExplainWhyPriorPath.RecommendedNextStep;
            }

            if (attention == AssistantAttentionSurface.Mix)
            {
                return AssistantExplainWhyPriorPath.Mix;
            }

            if (attention == AssistantAttentionSurface.WeeklyBrief)
            {
                return AssistantExplainWhyPriorPath.WeeklyBrief;
            }

            var topics = AssistantProductExpertTopics.Detect(priorUserMessage);
            if (topics.Count > 0 && !AssistantProductExpertTopics.IsMixedRetrieve(priorUserMessage))
            {
                return AssistantExplainWhyPriorPath.ProductExpert;
            }

            if (string.Equals(
                    priorAssistantTitle,
                    AssistantCombinedCreatePersistCopy.SuccessTitle,
                    StringComparison.Ordinal
                )
                || string.Equals(
                    priorAssistantTitle,
                    AssistantCombinedCreatePersistCopy.FailureTitle,
                    StringComparison.Ordinal
                ))
            {
                return AssistantExplainWhyPriorPath.CombinedCreate;
            }

            if (HasDataLayer(priorAssistantBody)
                || AssistantAskIntent.HasExplicitRetrieveAsk(priorUserMessage))
            {
                return AssistantExplainWhyPriorPath.DomainRetrieve;
            }

            return AssistantExplainWhyPriorPath.None;
        }

        public static bool PathUsesRecommendation(AssistantExplainWhyPriorPath path)
            => path is AssistantExplainWhyPriorPath.RecommendedNextStep
                or AssistantExplainWhyPriorPath.Mix;

        public static bool MatchesPrior(
            AssistantExplainWhyKind kind,
            AssistantExplainWhyPriorPath path,
            string priorAssistantBody
        )
        {
            if (path == AssistantExplainWhyPriorPath.None)
            {
                return false;
            }

            return kind switch
            {
                AssistantExplainWhyKind.NeedsAttention =>
                    path == AssistantExplainWhyPriorPath.NeedsAttention,
                AssistantExplainWhyKind.Results =>
                    path == AssistantExplainWhyPriorPath.ProductExpert
                        || HasDataLayer(priorAssistantBody)
                        || path == AssistantExplainWhyPriorPath.DomainRetrieve,
                AssistantExplainWhyKind.Recommendation =>
                    HasRecommendationLayer(priorAssistantBody)
                        || PathUsesRecommendation(path)
                        || path == AssistantExplainWhyPriorPath.NeedsAttention,
                _ => false,
            };
        }

        public static bool NamesNewPeriodOrLocation(
            string message,
            AssistantAnalysisScopeDto currentScope,
            IReadOnlyList<AssistantOwnedLocationRef> ownedLocations
        )
        {
            var lower = message.Trim().ToLowerInvariant();
            if (NamesNewPeriod(lower, currentScope))
            {
                return true;
            }

            return NamesNewOwnedLocation(message, currentScope, ownedLocations);
        }

        public static string? NamedPeriodPreset(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            foreach (var (needle, presetId) in PeriodNeedles)
            {
                if (presetId is not null
                    && lower.Contains(needle, StringComparison.Ordinal))
                {
                    return presetId;
                }
            }

            return null;
        }

        public static AssistantOwnedLocationRef? NamedOtherLocation(
            string message,
            int? savedLocationId,
            IReadOnlyList<AssistantOwnedLocationRef> ownedLocations
        )
        {
            foreach (var location in ownedLocations.OrderByDescending(item => item.Name.Length))
            {
                if (!ContainsName(message, location.Name))
                {
                    continue;
                }

                if (savedLocationId is not int id || location.Id != id)
                {
                    return location;
                }
            }

            return null;
        }

        private static bool NamesNewPeriod(
            string lower,
            AssistantAnalysisScopeDto currentScope
        )
        {
            var currentKind = (currentScope.ReportingPeriod.Kind ?? "preset")
                .Trim()
                .ToLowerInvariant();
            var currentPreset = (currentScope.ReportingPeriod.PresetId ?? "last7")
                .Trim();

            foreach (var (needle, presetId) in PeriodNeedles)
            {
                if (!lower.Contains(needle, StringComparison.Ordinal))
                {
                    continue;
                }

                if (presetId is null || currentKind == "custom")
                {
                    return true;
                }

                if (!string.Equals(presetId, currentPreset, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool NamesNewOwnedLocation(
            string message,
            AssistantAnalysisScopeDto currentScope,
            IReadOnlyList<AssistantOwnedLocationRef> ownedLocations
        )
            => NamedOtherLocation(
                message,
                currentScope.OwnedLocationId,
                ownedLocations
            ) is not null;

        private static bool ContainsHeading(string body, string heading)
            => body.Contains(heading, StringComparison.Ordinal);

        private static bool ContainsAny(string haystack, IReadOnlyList<string> needles)
        {
            foreach (var needle in needles)
            {
                if (haystack.Contains(needle, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsName(string text, string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
            {
                return false;
            }

            return Regex.IsMatch(
                text,
                $@"\b{Regex.Escape(name)}\b",
                RegexOptions.IgnoreCase
            );
        }
    }
}
