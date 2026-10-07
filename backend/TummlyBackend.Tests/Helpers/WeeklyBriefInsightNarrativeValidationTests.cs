using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class WeeklyBriefInsightNarrativeValidationTests
    {
        [Fact]
        public void TryValidate_RejectsEmptyNarrativesWhenBagNonEmpty()
        {
            var bag = SoleControlBag();
            var body = BodyWithWatchNext(["Watch Needs attention volume next week."]);
            var enrichment = new WeeklyBriefEnrichment(
                ExecutiveSummary: "Summary",
                FeedbackSummary: null,
                ActionWording: [],
                InsightNarratives: []
            );

            var ok = WeeklyBriefInsightNarrativeValidation.TryValidate(
                body,
                enrichment,
                bag,
                out var reason
            );

            Assert.False(ok);
            Assert.Equal("insightNarratives required for each candidate", reason);
        }

        [Fact]
        public void TryValidate_RejectsPartialNarrativeCoverage()
        {
            var bag = new WeeklyBriefInsightCandidateBag(
                WeeklyBriefInsightCandidates.ThresholdVersion,
                [
                    SoleControlBag().Candidates[0],
                    new WeeklyBriefInsightCandidate(
                        Id: WeeklyBriefInsightCandidates.TypeDataQualityIssue,
                        Type: WeeklyBriefInsightCandidates.TypeDataQualityIssue,
                        Evidence: new WeeklyBriefInsightEvidence(
                            ["activityScore"],
                            new Dictionary<string, object> { ["activityScore"] = 1 }
                        )
                    ),
                ]
            );
            var body = BodyWithWatchNext(["Watch Needs attention volume next week."]);
            var enrichment = new WeeklyBriefEnrichment(
                ExecutiveSummary: "Summary",
                FeedbackSummary: null,
                ActionWording: [],
                InsightNarratives:
                [
                    new WeeklyBriefInsightNarrative(
                        bag.Candidates[0].Id,
                        "Needs attention count is 4.",
                        "This coincides with unresolved negative feedback.",
                        "Open the follow-up queue."
                    ),
                ]
            );

            var ok = WeeklyBriefInsightNarrativeValidation.TryValidate(
                body,
                enrichment,
                bag,
                out var reason
            );

            Assert.False(ok);
            Assert.Equal("insightNarratives must cover every candidate", reason);
        }

        [Fact]
        public void TryValidate_RejectsEmptyCandidateBag()
        {
            var bag = new WeeklyBriefInsightCandidateBag(
                WeeklyBriefInsightCandidates.ThresholdVersion,
                []
            );
            var body = BodyWithWatchNext([]);
            var enrichment = new WeeklyBriefEnrichment(
                ExecutiveSummary: "Summary",
                FeedbackSummary: null,
                ActionWording: [],
                InsightNarratives: []
            );

            var ok = WeeklyBriefInsightNarrativeValidation.TryValidate(
                body,
                enrichment,
                bag,
                out var reason
            );

            Assert.False(ok);
            Assert.Equal("insight candidate bag must not be empty", reason);
        }

        [Fact]
        public void TryValidate_RejectsUnknownCandidateId()
        {
            var bag = SoleControlBag();
            var body = BodyWithWatchNext(["Watch Needs attention volume next week."]);
            var enrichment = new WeeklyBriefEnrichment(
                ExecutiveSummary: "Summary",
                FeedbackSummary: null,
                ActionWording: [],
                InsightNarratives:
                [
                    new WeeklyBriefInsightNarrative(
                        "missing-id",
                        "Observation text.",
                        "Interpretation text.",
                        "Open the action."
                    ),
                ]
            );

            var ok = WeeklyBriefInsightNarrativeValidation.TryValidate(
                body,
                enrichment,
                bag,
                out var reason
            );

            Assert.False(ok);
            Assert.Equal("unknown candidateId", reason);
        }

        [Fact]
        public void TryValidate_RejectsBannedCausalPhrase()
        {
            var bag = SoleControlBag();
            var body = BodyWithWatchNext(["Watch Needs attention volume next week."]);
            var enrichment = new WeeklyBriefEnrichment(
                ExecutiveSummary: "Summary",
                FeedbackSummary: null,
                ActionWording: [],
                InsightNarratives:
                [
                    new WeeklyBriefInsightNarrative(
                        bag.Candidates[0].Id,
                        "Needs attention count is 4.",
                        "This happened because service slipped.",
                        "Open the action."
                    ),
                ]
            );

            var ok = WeeklyBriefInsightNarrativeValidation.TryValidate(
                body,
                enrichment,
                bag,
                out var reason
            );

            Assert.False(ok);
            Assert.Equal("ungrounded causation", reason);
        }

        [Fact]
        public void TryValidate_RejectsNumberOutsideSnapshot()
        {
            var bag = SoleControlBag();
            var body = BodyWithWatchNext(["Watch Needs attention volume next week."]);
            var enrichment = new WeeklyBriefEnrichment(
                ExecutiveSummary: "Summary",
                FeedbackSummary: null,
                ActionWording: [],
                InsightNarratives:
                [
                    new WeeklyBriefInsightNarrative(
                        bag.Candidates[0].Id,
                        "Needs attention count is 99.",
                        "Review the queue.",
                        "Open the action."
                    ),
                ]
            );

            var ok = WeeklyBriefInsightNarrativeValidation.TryValidate(
                body,
                enrichment,
                bag,
                out var reason
            );

            Assert.False(ok);
            Assert.Equal("number outside candidate snapshot", reason);
        }

        [Fact]
        public void TryValidate_RequiresEmptyWatchNext_ForSoleNoMaterialChange()
        {
            var bag = new WeeklyBriefInsightCandidateBag(
                WeeklyBriefInsightCandidates.ThresholdVersion,
                [
                    new WeeklyBriefInsightCandidate(
                        WeeklyBriefInsightCandidates.TypeNoMaterialChange,
                        WeeklyBriefInsightCandidates.TypeNoMaterialChange,
                        new WeeklyBriefInsightEvidence(
                            ["activityScore"],
                            new Dictionary<string, object> { ["activityScore"] = 0 }
                        )
                    ),
                ]
            );
            var body = BodyWithWatchNext(["Do not invent filler."]);
            var enrichment = new WeeklyBriefEnrichment(
                ExecutiveSummary: "Summary",
                FeedbackSummary: null,
                ActionWording: [],
                InsightNarratives:
                [
                    new WeeklyBriefInsightNarrative(
                        bag.Candidates[0].Id,
                        "No insight candidate cleared.",
                        "Treat the week as stable.",
                        null
                    ),
                ]
            );

            var ok = WeeklyBriefInsightNarrativeValidation.TryValidate(
                body,
                enrichment,
                bag,
                out var reason
            );

            Assert.False(ok);
            Assert.Equal(
                "watchNext must be empty for sole no-material-change",
                reason
            );
        }

        [Fact]
        public void TryValidate_AcceptsGroundedControlNarrative()
        {
            var bag = SoleControlBag();
            var body = BodyWithWatchNext(["Watch Needs attention volume next week."]);
            var enrichment = new WeeklyBriefEnrichment(
                ExecutiveSummary: "Summary",
                FeedbackSummary: null,
                ActionWording: [],
                InsightNarratives:
                [
                    new WeeklyBriefInsightNarrative(
                        bag.Candidates[0].Id,
                        "Needs attention count is 4.",
                        "This coincides with unresolved negative feedback.",
                        "Open the follow-up queue."
                    ),
                ]
            );

            var ok = WeeklyBriefInsightNarrativeValidation.TryValidate(
                body,
                enrichment,
                bag,
                out var reason
            );

            Assert.True(ok);
            Assert.Null(reason);
        }

        private static WeeklyBriefInsightCandidateBag SoleControlBag()
            => new(
                WeeklyBriefInsightCandidates.ThresholdVersion,
                [
                    new WeeklyBriefInsightCandidate(
                        Id: $"{WeeklyBriefInsightCandidates.TypeControlSignal}:{WeeklyBriefInsightCandidates.ActionFeedbackNeedsAttention}",
                        Type: WeeklyBriefInsightCandidates.TypeControlSignal,
                        Evidence: new WeeklyBriefInsightEvidence(
                            ["needsAttentionCount"],
                            new Dictionary<string, object>
                            {
                                ["needsAttentionCount"] = 4,
                            }
                        ),
                        ActionKind: WeeklyBriefInsightCandidates.ActionFeedbackNeedsAttention
                    ),
                ]
            );

        private static WeeklyBriefBody BodyWithWatchNext(IReadOnlyList<string> watchNext)
            => new(
                Headline: "Headline",
                Capture: new WeeklyBriefSection(false, WeeklyBriefStructuredOutput.EmptyCaptureSummary, null),
                Feedback: new WeeklyBriefSection(false, WeeklyBriefStructuredOutput.EmptyFeedbackSummary, null),
                Offers: new WeeklyBriefSection(false, WeeklyBriefStructuredOutput.EmptyOffersSummary, null),
                Campaigns: new WeeklyBriefSection(false, WeeklyBriefStructuredOutput.EmptyCampaignsSummary, null),
                WatchNext: watchNext
            );
    }
}
