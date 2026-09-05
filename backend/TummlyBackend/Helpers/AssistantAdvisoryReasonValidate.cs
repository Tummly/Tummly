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

            var recommendations = FilterRecommendations(
                output,
                allowedPaths,
                logger
            );

            if (string.Equals(answerType, "direct", StringComparison.Ordinal)
                || string.Equals(answerType, "product_expert", StringComparison.Ordinal))
            {
                if (recommendations.Count > 0)
                {
                    logger.LogWarning(
                        "Advisory Reason answer_type={AnswerType} returned "
                        + "{Count} recommendations; dropping them.",
                        answerType,
                        recommendations.Count
                    );
                    recommendations = [];
                }
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
            if (output.Recommendations.Count == 0)
            {
                return output.Summary;
            }

            var lines = output.Recommendations.Select(RenderRecommendationLine);
            return output.Summary + "\n\n" + string.Join("\n", lines);
        }

        private static string RenderRecommendationLine(
            AssistantAdvisoryReasonRecommendation recommendation
        )
        {
            var line = $"{recommendation.Headline} — {recommendation.Reason}";
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
                if (recommendation.EvidenceRef.Count == 0)
                {
                    logger.LogWarning(
                        "Dropping advisory recommendation with empty evidence_ref "
                        + "(headline={Headline}).",
                        recommendation.Headline
                    );
                    continue;
                }

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
