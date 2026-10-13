using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Validates advisory Reason model output against the fed snapshot.
    /// Drops bad recommendations; malformed clarify falls back.
    /// </summary>
    public static class AssistantAdvisoryReasonValidate
    {
        public const string NoClearDriverBody =
            "I can see the recent numbers, but I do not see a clear "
            + "driver yet. I will not invent a cause.";

        public static AdvisoryReasonValidateResult Validate(
            AssistantAdvisoryReasonOutput output,
            RestaurantContextSnapshot snapshot,
            ILogger logger,
            string? conversationTurnId = null
        )
        {
            var answerType = output.AnswerType.Trim();
            var allowedPaths = BuildAllowedEvidencePaths(snapshot);

            if (string.Equals(answerType, "clarify", StringComparison.Ordinal))
            {
                if (output.Recommendations.Count > 0
                    || string.IsNullOrWhiteSpace(output.ClarifyingQuestion))
                {
                    logger.LogError(
                        "Advisory Reason clarify after Clear is malformed "
                        + "(recommendations={RecommendationCount}, "
                        + "clarifying_question_empty={QuestionEmpty}). "
                        + "Falling back to NoClearDriver.",
                        output.Recommendations.Count,
                        string.IsNullOrWhiteSpace(output.ClarifyingQuestion)
                    );
                    return new AdvisoryReasonValidateResult.FallbackNoClearDriver();
                }

                var turnId = string.IsNullOrWhiteSpace(conversationTurnId)
                    ? Guid.NewGuid().ToString("N")
                    : conversationTurnId;
                return new AdvisoryReasonValidateResult.Clarify(
                    AssistantAdvisoryIntent.ModelRequestedGap(
                        [],
                        turnId,
                        output.ClarifyingQuestion
                    )
                );
            }

            IReadOnlyList<AssistantAdvisoryReasonRecommendation> recommendations;
            if (string.Equals(answerType, "direct", StringComparison.Ordinal)
                || string.Equals(answerType, "product_expert", StringComparison.Ordinal))
            {
                // Warn on the model payload before evidence filtering so a turn
                // that only had bad refs still logs the answer_type violation.
                if (output.Recommendations.Count > 0)
                {
                    logger.LogWarning(
                        "Advisory Reason answer_type={AnswerType} returned "
                        + "{Count} recommendations; dropping them.",
                        answerType,
                        output.Recommendations.Count
                    );
                }

                recommendations = [];
            }
            else
            {
                recommendations = FilterRecommendations(
                    output,
                    allowedPaths,
                    logger
                );
            }

            var evidenceUsed = output.EvidenceUsed
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            return new AdvisoryReasonValidateResult.Valid(
                output with
                {
                    Recommendations = recommendations,
                    EvidenceUsed = evidenceUsed,
                }
            );
        }

        public static bool EvidenceRefResolves(
            string path,
            IReadOnlySet<string> allowedPaths
        )
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var trimmed = path.Trim();
            if (allowedPaths.Contains(trimmed))
            {
                return true;
            }

            foreach (var allowed in allowedPaths)
            {
                if (allowed.StartsWith(trimmed + ".", StringComparison.Ordinal)
                    || allowed.StartsWith(trimmed + "[", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static HashSet<string> BuildAllowedEvidencePaths(
            RestaurantContextSnapshot snapshot
        )
        {
            var paths = new HashSet<string>(StringComparer.Ordinal)
            {
                "Account",
                "Account.Covers",
                "Account.Revenue",
                "Account.AvgTicket",
                "Account.RepeatVisitRate",
                "Account.Flags",
                "Account.RepeatVisitRateByLocation",
                "Campaigns",
                "Campaigns.Active",
                "Campaigns.RecentlyEnded",
                "Campaigns.Flags",
                "Offers",
                "Offers.Active",
                "Offers.ExpiringUnused",
                "Offers.Flags",
                "Feedback",
                "Feedback.SentimentScore",
                "Feedback.Flagged",
                "Feedback.RecurringThemes",
                "Feedback.UnresolvedRecoveryCount",
                "Feedback.Flags",
                "Guests",
                "Guests.NewGuestCount",
                "Guests.LapsedGuestCount",
                "Guests.VipAtRiskCount",
                "Guests.Flags",
                "Capture",
                "Capture.FunnelStartCount",
                "Capture.FunnelCompleteCount",
                "Capture.DropOffRate",
                "Capture.DropOffStageFlag",
                "Capture.Flags",
                "RecentActions",
                "RecentActions.Last30Days",
                "Meta",
                "Meta.IsNewAccount",
                "Meta.TotalDaysOfHistory",
                "Meta.SectionsWithInsufficientData",
            };

            AddFlagPaths(paths, "Account.Flags", snapshot.Account.Flags);
            AddFlagPaths(paths, "Campaigns.Flags", snapshot.Campaigns.Flags);
            AddFlagPaths(paths, "Offers.Flags", snapshot.Offers.Flags);
            AddFlagPaths(paths, "Feedback.Flags", snapshot.Feedback.Flags);
            AddFlagPaths(paths, "Guests.Flags", snapshot.Guests.Flags);
            AddFlagPaths(paths, "Capture.Flags", snapshot.Capture.Flags);

            return paths;
        }

        public static string RenderBody(AssistantAdvisoryReasonOutput output)
        {
            var summary = FormatSummaryForOperator(output.Summary);
            if (output.Recommendations.Count == 0)
            {
                return $"## Summary\n\n{summary}";
            }

            var lines = output.Recommendations
                .Select(RenderRecommendationLine)
                .Take(3);
            return "## Summary\n\n"
                + summary
                + "\n\n## Recommended next steps\n\n"
                + string.Join("\n", lines);
        }

        /// <summary>
        /// Operator-facing summary: scrub technical refs, prefer short bullets.
        /// </summary>
        public static string FormatSummaryForOperator(string summary)
        {
            var scrubbed = ScrubTechnicalProse(summary);
            scrubbed = AssistantOperatorProse.Scrub(scrubbed);
            if (string.IsNullOrWhiteSpace(scrubbed))
            {
                return scrubbed;
            }

            if (scrubbed.Contains("\n- ", StringComparison.Ordinal)
                || scrubbed.TrimStart().StartsWith("- ", StringComparison.Ordinal))
            {
                return scrubbed;
            }

            var sentences = SplitSentences(scrubbed);
            if (sentences.Count <= 1)
            {
                return scrubbed;
            }

            return string.Join(
                "\n",
                sentences.Take(6).Select(sentence => $"- {sentence}")
            );
        }

        public static string ScrubTechnicalProse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text?.Trim() ?? string.Empty;
            }

            var cleaned = text;
            // Drop parenthetical tech dumps: (IsNewAccount: true), (Account.Revenue, …)
            cleaned = Regex.Replace(
                cleaned,
                @"\s*\((?:[^)]*[A-Z][A-Za-z0-9_.]*(?:[:.=,]|true|false)[^)]*)\)",
                string.Empty,
                RegexOptions.CultureInvariant
            );
            // Drop dotted snapshot paths: Account.Revenue, Feedback.Flags
            cleaned = Regex.Replace(
                cleaned,
                @"\b[A-Z][A-Za-z]+(?:\.[A-Z][A-Za-z0-9_]+)+\b(?:\s*=\s*[^,;.]+)?",
                string.Empty,
                RegexOptions.CultureInvariant
            );
            // Drop SCREAMING_SNAKE flag codes
            cleaned = Regex.Replace(
                cleaned,
                @"\b[A-Z]{2,}(?:_[A-Z0-9]+)+\b",
                string.Empty,
                RegexOptions.CultureInvariant
            );
            // Drop leftover "includes ," / empty equals fragments
            cleaned = Regex.Replace(
                cleaned,
                @"\b(?:includes|equals)\s*[,;.]",
                ".",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            cleaned = Regex.Replace(cleaned, @"\s{2,}", " ", RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\s+([,;.])", "$1", RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"([.!?]){2,}", "$1", RegexOptions.CultureInvariant);
            return cleaned.Trim();
        }

        private static List<string> SplitSentences(string text)
        {
            var parts = Regex.Split(text.Trim(), @"(?<=[.!?])\s+")
                .Select(part => part.Trim())
                .Where(part => part.Length > 0)
                .ToList();
            return parts;
        }

        public static string AnswerTitle(AdvisoryAskSubType subType)
            => subType switch
            {
                AdvisoryAskSubType.Growth => "Growth outlook",
                AdvisoryAskSubType.Comparison => "Period comparison",
                AdvisoryAskSubType.Diagnostic => "What stands out",
                AdvisoryAskSubType.PureProductFaq => "Product guidance",
                _ => "Performance overview",
            };

        /// <summary>
        /// Compact bar chart of key Advisory snapshot metrics for the UI.
        /// </summary>
        public static AssistantAnswerChart? BuildOverviewChart(
            RestaurantContextSnapshot snapshot
        )
        {
            var series = new List<AssistantAnswerChartPoint>();
            AddMetricBar(series, "Covers", snapshot.Account.Covers.Current);
            AddMetricBar(series, "New guests", snapshot.Guests.NewGuestCount.Current);
            AddMetricBar(
                series,
                "Flagged feedback",
                snapshot.Feedback.Flagged.Count
            );
            AddMetricBar(
                series,
                "Funnel completes",
                snapshot.Capture.FunnelCompleteCount.Current
            );
            AddMetricBar(
                series,
                "QR / funnel starts",
                snapshot.Capture.FunnelStartCount.Current
            );

            if (!AssistantAnswerChartGrounding.HasChartableSeries(series))
            {
                return null;
            }

            return new AssistantAnswerChart(
                "Key metrics this period",
                AssistantAnswerChartGrounding.BarKind,
                "Metric",
                series
            );
        }

        private static void AddMetricBar(
            List<AssistantAnswerChartPoint> series,
            string label,
            decimal value
        )
        {
            if (value <= 0m)
            {
                return;
            }

            var rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            if (rounded <= 0)
            {
                return;
            }

            series.Add(new AssistantAnswerChartPoint(label, rounded));
        }

        private static string RenderRecommendationLine(
            AssistantAdvisoryReasonRecommendation recommendation
        )
        {
            var headline = ScrubTechnicalProse(recommendation.Headline.Trim());
            var reason = ScrubTechnicalProse(recommendation.Reason.Trim());
            var line = $"- **{headline}** — {reason}";
            if (IsRouterAction(recommendation.Action))
            {
                line += $" ({recommendation.Action})";
            }

            return line;
        }

        private static bool IsRouterAction(string action)
            => action is AssistantTask.CreateCampaignWithOffer
                or AssistantTask.CreateCampaignDraft
                or AssistantTask.OfferPath
                or AssistantTask.RecoveryPath;

        private static IReadOnlyList<AssistantAdvisoryReasonRecommendation>
            FilterRecommendations(
                AssistantAdvisoryReasonOutput output,
                IReadOnlySet<string> allowedPaths,
                ILogger logger
            )
        {
            var kept = new List<AssistantAdvisoryReasonRecommendation>();
            foreach (var recommendation in output.Recommendations)
            {
                // Empty evidence_ref has nothing that fails to resolve — keep it.
                var badRefs = recommendation.EvidenceRef
                    .Where(path => !EvidenceRefResolves(path, allowedPaths))
                    .ToArray();
                if (badRefs.Length > 0)
                {
                    logger.LogWarning(
                        "Dropping advisory recommendation for bad evidence_ref "
                        + "{BadRefs} (headline={Headline}).",
                        string.Join(", ", badRefs),
                        recommendation.Headline
                    );
                    continue;
                }

                kept.Add(recommendation);
            }

            return kept;
        }

        private static void AddFlagPaths(
            HashSet<string> paths,
            string flagsRoot,
            IReadOnlyList<Flag> flags
        )
        {
            foreach (var flag in flags)
            {
                if (string.IsNullOrWhiteSpace(flag.Code))
                {
                    continue;
                }

                paths.Add($"{flagsRoot}.{flag.Code.Trim()}");
            }
        }
    }
}
