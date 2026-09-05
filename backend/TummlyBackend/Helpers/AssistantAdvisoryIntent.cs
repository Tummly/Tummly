using System.Globalization;
using TummlyBackend.Configurations;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public enum AdvisoryAskSubType
    {
        GeneralHealth = 0,
        Growth = 1,
        Comparison = 2,
        Diagnostic = 3,
        PureProductFaq = 4,
    }

    public enum AdvisoryGapReason
    {
        ScopeUnresolved = 0,
        RangeAmbiguous = 1,
        MetricAmbiguous = 2,
        InsufficientData = 3,
        ModelRequested = 4,
    }

    public sealed record AdvisoryGap(
        AdvisoryGapReason Reason,
        string[] CandidateOptions,
        string? PartialDiagnosisNote,
        string ConversationTurnId
    );

    public abstract record AdvisoryPreCheckOutcome
    {
        public sealed record Clear(
            LocationScope Scope,
            PeriodWindow Current,
            PeriodWindow Comparison,
            RestaurantContextSnapshot Snapshot,
            AdvisoryAskSubType SubType,
            string? ChosenMetricNote
        ) : AdvisoryPreCheckOutcome;

        public sealed record Gap(AdvisoryGap Advisory) : AdvisoryPreCheckOutcome;

        public sealed record PureProduct : AdvisoryPreCheckOutcome;

        public sealed record NoClearDriver(
            string Body,
            RestaurantContextSnapshot Snapshot
        ) : AdvisoryPreCheckOutcome;
    }

    /// <summary>
    /// Advisory-lane ask classify and pre-check table. Creation lane stays
    /// on AssistantTaskClassification.
    /// </summary>
    public static class AssistantAdvisoryIntent
    {
        private sealed record PreCheckInput(
            AdvisoryAskSubType SubType,
            IReadOnlyList<string> OwnedLocationIds,
            string Message,
            RestaurantContextSnapshot Snapshot,
            RestaurantContextSnapshotSettings Settings,
            string ConversationTurnId,
            string? ChosenMetricNote
        );

        private static readonly IReadOnlyDictionary<
            AdvisoryAskSubType,
            Func<PreCheckInput, AdvisoryPreCheckOutcome>
        > PreCheckRules =
            new Dictionary<AdvisoryAskSubType, Func<PreCheckInput, AdvisoryPreCheckOutcome>>
            {
                [AdvisoryAskSubType.PureProductFaq] = _ =>
                    new AdvisoryPreCheckOutcome.PureProduct(),
                [AdvisoryAskSubType.GeneralHealth] = EvaluateGeneralHealth,
                [AdvisoryAskSubType.Growth] = EvaluateGrowth,
                [AdvisoryAskSubType.Comparison] = EvaluateComparison,
                [AdvisoryAskSubType.Diagnostic] = EvaluateDiagnostic,
            };

        public static IReadOnlyCollection<AdvisoryAskSubType> CoveredSubTypes
            => PreCheckRules.Keys.ToArray();

        public static AdvisoryAskSubType ClassifySubType(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            if (lower.Length == 0)
            {
                return AdvisoryAskSubType.GeneralHealth;
            }

            if (AssistantProductExpertTopics.Detect(message).Count > 0
                && !AssistantProductExpertTopics.IsMixedRetrieve(message)
                && !LooksLikeHealth(lower)
                && !LooksLikeGrowth(lower)
                && !LooksLikeDiagnostic(lower)
                && !LooksLikeComparison(lower))
            {
                return AdvisoryAskSubType.PureProductFaq;
            }

            if (LooksLikeDiagnostic(lower))
            {
                return AdvisoryAskSubType.Diagnostic;
            }

            if (LooksLikeComparison(lower))
            {
                return AdvisoryAskSubType.Comparison;
            }

            if (LooksLikeGrowth(lower))
            {
                return AdvisoryAskSubType.Growth;
            }

            if (LooksLikeHealth(lower))
            {
                return AdvisoryAskSubType.GeneralHealth;
            }

            if (AssistantProductExpertTopics.Detect(message).Count > 0
                && !AssistantProductExpertTopics.IsMixedRetrieve(message))
            {
                return AdvisoryAskSubType.PureProductFaq;
            }

            return AdvisoryAskSubType.GeneralHealth;
        }

        public static bool LooksLikeAdvisoryRetrieve(string message)
        {
            if (AssistantTaskClassification.LooksLikeCreateTurn(message)
                || AssistantTaskClassification.LooksLikeCreateCampaignDraft(message)
                || AssistantTaskClassification.LooksLikeCreateCampaignWithOffer(message)
                || AssistantTaskClassification.LooksLikeOfferPath(message)
                || AssistantTaskClassification.LooksLikeRecoveryPath(message)
                || AssistantSendScheduleAsk.LooksLikeSendOrSchedule(message)
                || AssistantSendScheduleAsk.LooksLikeOfferActivate(message))
            {
                return false;
            }

            var task = AssistantTaskClassification.Classify(message);
            if (task != AssistantTask.Retrieve)
            {
                return false;
            }

            var lower = message.Trim().ToLowerInvariant();
            return LooksLikeHealth(lower)
                || LooksLikeGrowth(lower)
                || LooksLikeComparison(lower)
                || LooksLikeDiagnostic(lower);
        }

        public static AdvisoryPreCheckOutcome Evaluate(
            IReadOnlyList<string> ownedLocationIds,
            string message,
            RestaurantContextSnapshot snapshot,
            RestaurantContextSnapshotSettings settings,
            string? conversationTurnId = null,
            string? chosenMetricNote = null
        )
        {
            var subType = ClassifySubType(message);
            var input = new PreCheckInput(
                subType,
                ownedLocationIds,
                message,
                snapshot,
                settings,
                conversationTurnId
                    ?? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                chosenMetricNote
            );
            if (!PreCheckRules.TryGetValue(subType, out var rule))
            {
                return EvaluateGeneralHealth(input);
            }

            return rule(input);
        }

        public static string GapQuestionBody(AdvisoryGap gap)
        {
            var prefix = string.IsNullOrWhiteSpace(gap.PartialDiagnosisNote)
                ? string.Empty
                : gap.PartialDiagnosisNote.Trim() + " ";
            var options = gap.CandidateOptions.Length == 0
                ? string.Empty
                : " " + AssistantCreateLocationGap.Join(gap.CandidateOptions);
            var ask = gap.Reason switch
            {
                AdvisoryGapReason.ScopeUnresolved =>
                    $"Which venue should I use:{options}?",
                AdvisoryGapReason.RangeAmbiguous =>
                    $"Which date range should I use:{options}?",
                AdvisoryGapReason.MetricAmbiguous =>
                    $"Which metric should I focus on:{options}?",
                AdvisoryGapReason.InsufficientData =>
                    "I do not have enough history yet for a reliable trend. "
                    + "Add more guest activity, or ask again after two weeks.",
                AdvisoryGapReason.ModelRequested =>
                    $"I need one choice from you:{options}?",
                _ => $"I need one choice from you:{options}?",
            };
            return prefix + ask;
        }

        private static AdvisoryPreCheckOutcome EvaluateGeneralHealth(PreCheckInput input)
            => ScopeOrHistoryGap(input) ?? ClearOutcome(input);

        private static AdvisoryPreCheckOutcome EvaluateGrowth(PreCheckInput input)
        {
            if (ScopeOrHistoryGap(input) is { } gap)
            {
                return gap;
            }

            if (string.IsNullOrWhiteSpace(input.ChosenMetricNote)
                && HasContradictoryTrends(input.Snapshot))
            {
                return GapOutcome(
                    AdvisoryGapReason.MetricAmbiguous,
                    ["covers", "capture", "sentiment"],
                    input.ConversationTurnId,
                    "Trends move in different directions."
                );
            }

            return ClearOutcome(input);
        }

        private static AdvisoryPreCheckOutcome EvaluateComparison(PreCheckInput input)
        {
            if (input.OwnedLocationIds.Count == 0)
            {
                return GapOutcome(
                    AdvisoryGapReason.ScopeUnresolved,
                    [],
                    input.ConversationTurnId,
                    null
                );
            }

            if (input.Snapshot.Meta.SectionsWithInsufficientData.Length > 0
                && input.Snapshot.Meta.TotalDaysOfHistory
                    < input.Settings.MinDaysForTrendClaim)
            {
                return GapOutcome(
                    AdvisoryGapReason.InsufficientData,
                    ["last 7 days", "last 30 days", "this month"],
                    input.ConversationTurnId,
                    "The comparison window still has thin data."
                );
            }

            return ClearOutcome(input);
        }

        private static AdvisoryPreCheckOutcome EvaluateDiagnostic(PreCheckInput input)
        {
            if (input.OwnedLocationIds.Count == 0)
            {
                return GapOutcome(
                    AdvisoryGapReason.ScopeUnresolved,
                    [],
                    input.ConversationTurnId,
                    null
                );
            }

            // Diagnostic asks almost never hard-gap: thin history and missing
            // drivers are honest answers, not clarifying questions.
            if (input.Snapshot.Meta.TotalDaysOfHistory
                    < input.Settings.MinDaysForTrendClaim
                || !HasDriverFlag(input.Snapshot))
            {
                return new AdvisoryPreCheckOutcome.NoClearDriver(
                    "I can see the recent numbers, but I do not see a clear "
                        + "driver yet. I will not invent a cause.",
                    input.Snapshot
                );
            }

            return ClearOutcome(input);
        }

        /// <summary>
        /// Gap when the live Reason model asks for clarify mid-turn.
        /// </summary>
        public static AdvisoryGap ModelRequestedGap(
            string[] candidateOptions,
            string conversationTurnId,
            string? partialDiagnosisNote = null
        )
            => new(
                AdvisoryGapReason.ModelRequested,
                candidateOptions,
                partialDiagnosisNote,
                conversationTurnId
            );

        private static AdvisoryPreCheckOutcome? ScopeOrHistoryGap(PreCheckInput input)
        {
            if (input.OwnedLocationIds.Count == 0)
            {
                return GapOutcome(
                    AdvisoryGapReason.ScopeUnresolved,
                    [],
                    input.ConversationTurnId,
                    "No Owned location is available."
                );
            }

            if (input.Snapshot.Meta.TotalDaysOfHistory
                < input.Settings.MinDaysForTrendClaim)
            {
                return GapOutcome(
                    AdvisoryGapReason.InsufficientData,
                    [],
                    input.ConversationTurnId,
                    null
                );
            }

            return null;
        }

        private static AdvisoryPreCheckOutcome.Clear ClearOutcome(PreCheckInput input)
            => new(
                input.Snapshot.Scope,
                input.Snapshot.CurrentPeriod,
                input.Snapshot.ComparisonPeriod,
                input.Snapshot,
                input.SubType,
                input.ChosenMetricNote
            );

        private static AdvisoryPreCheckOutcome.Gap GapOutcome(
            AdvisoryGapReason reason,
            string[] options,
            string conversationTurnId,
            string? partialNote
        )
            => new(
                new AdvisoryGap(
                    reason,
                    options,
                    partialNote,
                    conversationTurnId
                )
            );

        private static bool HasContradictoryTrends(RestaurantContextSnapshot snapshot)
        {
            var signs = new List<int>();
            AddSign(signs, snapshot.Account.Covers.PctDelta);
            AddSign(signs, snapshot.Capture.FunnelCompleteCount.PctDelta);
            AddSign(signs, snapshot.Feedback.SentimentScore.PctDelta);
            if (signs.Count < 2)
            {
                return false;
            }

            return signs.Any(sign => sign > 0) && signs.Any(sign => sign < 0);
        }

        private static void AddSign(List<int> signs, decimal? pctDelta)
        {
            if (pctDelta is null || pctDelta == 0m)
            {
                return;
            }

            signs.Add(Math.Sign(pctDelta.Value));
        }

        private static bool HasDriverFlag(RestaurantContextSnapshot snapshot)
            => snapshot.Account.Flags.Any(IsDriver)
                || snapshot.Campaigns.Flags.Any(IsDriver)
                || snapshot.Offers.Flags.Any(IsDriver)
                || snapshot.Feedback.Flags.Any(IsDriver)
                || snapshot.Guests.Flags.Any(IsDriver);

        private static bool IsDriver(Flag flag)
            => flag.Severity is FlagSeverity.Notable or FlagSeverity.Urgent;

        private static bool LooksLikeHealth(string lower)
            => ContainsAny(
                lower,
                "how are we doing",
                "how is business",
                "how's business",
                "health check",
                "business health",
                "overview of",
                "overall performance",
                "how are things"
            );

        private static bool LooksLikeGrowth(string lower)
            => ContainsAny(
                lower,
                "grow",
                "growth",
                "increase covers",
                "increase revenue",
                "improve sales",
                "boost guests",
                "more guests"
            );

        private static bool LooksLikeComparison(string lower)
            => ContainsAny(
                lower,
                "compare",
                " versus ",
                " vs ",
                "this week vs",
                "this month vs",
                "compared to",
                "against last"
            );

        private static bool LooksLikeDiagnostic(string lower)
            => ContainsAny(
                lower,
                "why did",
                "why is",
                "what's wrong",
                "what is wrong",
                "what caused",
                "root cause",
                "diagnose",
                "what happened to"
            );

        private static bool ContainsAny(string lower, params string[] needles)
            => needles.Any(needle => lower.Contains(needle, StringComparison.Ordinal));
    }
}
