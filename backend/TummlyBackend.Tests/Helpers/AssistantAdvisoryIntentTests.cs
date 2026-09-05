using TummlyBackend.Configurations;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantAdvisoryIntentTests
    {
        private static readonly RestaurantContextSnapshotSettings Settings = new()
        {
            MinDaysForTrendClaim = 14,
            NewAccountHistoryDays = 30,
        };

        [Theory]
        [InlineData("How are we doing this month?", AdvisoryAskSubType.GeneralHealth)]
        [InlineData("How can we grow covers?", AdvisoryAskSubType.Growth)]
        [InlineData("Compare this month vs last month", AdvisoryAskSubType.Comparison)]
        [InlineData("Why did covers drop?", AdvisoryAskSubType.Diagnostic)]
        [InlineData("What can you do?", AdvisoryAskSubType.PureProductFaq)]
        public void ClassifySubType_MatchesKeywords(
            string message,
            AdvisoryAskSubType expected
        )
        {
            Assert.Equal(expected, AssistantAdvisoryIntent.ClassifySubType(message));
        }

        [Fact]
        public void PreCheckTable_CoversAllSubTypes()
        {
            var expected = Enum.GetValues<AdvisoryAskSubType>();
            Assert.Equal(
                expected.OrderBy(value => value),
                AssistantAdvisoryIntent.CoveredSubTypes.OrderBy(value => value)
            );
        }

        [Fact]
        public void Evaluate_GeneralHealth_ZeroLocations_ScopeUnresolved()
        {
            var snapshot = Snapshot(historyDays: 60);

            var outcome = AssistantAdvisoryIntent.Evaluate(
                ownedLocationIds: [],
                message: "How are we doing?",
                snapshot,
                Settings
            );

            var gap = Assert.IsType<AdvisoryPreCheckOutcome.Gap>(outcome);
            Assert.Equal(AdvisoryGapReason.ScopeUnresolved, gap.Advisory.Reason);
        }

        [Fact]
        public void Evaluate_GeneralHealth_ShortHistory_InsufficientData()
        {
            var snapshot = Snapshot(historyDays: 10);

            var outcome = AssistantAdvisoryIntent.Evaluate(
                ownedLocationIds: ["10"],
                message: "How are we doing?",
                snapshot,
                Settings
            );

            var gap = Assert.IsType<AdvisoryPreCheckOutcome.Gap>(outcome);
            Assert.Equal(AdvisoryGapReason.InsufficientData, gap.Advisory.Reason);
        }

        [Fact]
        public void Evaluate_Growth_ContradictoryTrends_MetricAmbiguous()
        {
            var snapshot = Snapshot(
                historyDays: 60,
                coversPct: 20m,
                capturePct: -15m,
                sentimentPct: 5m
            );

            var outcome = AssistantAdvisoryIntent.Evaluate(
                ownedLocationIds: ["10"],
                message: "How can we grow?",
                snapshot,
                Settings
            );

            var gap = Assert.IsType<AdvisoryPreCheckOutcome.Gap>(outcome);
            Assert.Equal(AdvisoryGapReason.MetricAmbiguous, gap.Advisory.Reason);
            Assert.Contains("covers", gap.Advisory.CandidateOptions);
            Assert.StartsWith(
                "Trends move in different directions.",
                AssistantAdvisoryIntent.GapQuestionBody(gap.Advisory),
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void Evaluate_PureProduct_NeverGaps()
        {
            var snapshot = Snapshot(historyDays: 0);

            var outcome = AssistantAdvisoryIntent.Evaluate(
                ownedLocationIds: [],
                message: "What can you do?",
                snapshot,
                Settings
            );

            Assert.IsType<AdvisoryPreCheckOutcome.PureProduct>(outcome);
        }

        [Fact]
        public void Evaluate_Diagnostic_NoDriver_IsNoClearDriverNotGap()
        {
            var snapshot = Snapshot(historyDays: 60);

            var outcome = AssistantAdvisoryIntent.Evaluate(
                ownedLocationIds: ["10"],
                message: "Why did covers drop?",
                snapshot,
                Settings
            );

            var noDriver = Assert.IsType<AdvisoryPreCheckOutcome.NoClearDriver>(outcome);
            Assert.Contains("will not invent a cause", noDriver.Body, StringComparison.Ordinal);
        }

        [Fact]
        public void Evaluate_Diagnostic_ShortHistory_IsNoClearDriverNotGap()
        {
            var snapshot = Snapshot(historyDays: 5);

            var outcome = AssistantAdvisoryIntent.Evaluate(
                ownedLocationIds: ["10"],
                message: "Why did covers drop?",
                snapshot,
                Settings
            );

            Assert.IsType<AdvisoryPreCheckOutcome.NoClearDriver>(outcome);
        }

        [Fact]
        public void Evaluate_Comparison_ThinHistory_InsufficientData()
        {
            var snapshot = Snapshot(historyDays: 5) with
            {
                Meta = new SnapshotMeta(
                    IsNewAccount: true,
                    TotalDaysOfHistory: 5,
                    SectionsWithInsufficientData: ["Offers"]
                ),
            };

            var outcome = AssistantAdvisoryIntent.Evaluate(
                ownedLocationIds: ["10"],
                message: "Compare this month vs last month",
                snapshot,
                Settings
            );

            var gap = Assert.IsType<AdvisoryPreCheckOutcome.Gap>(outcome);
            Assert.Equal(AdvisoryGapReason.InsufficientData, gap.Advisory.Reason);
        }

        [Fact]
        public void ModelRequestedGap_UsesModelRequestedReason()
        {
            var gap = AssistantAdvisoryIntent.ModelRequestedGap(
                ["covers", "sentiment"],
                "turn-1",
                "Partial note"
            );
            Assert.Equal(AdvisoryGapReason.ModelRequested, gap.Reason);
            Assert.Equal("Partial note", gap.PartialDiagnosisNote);
        }

        private static RestaurantContextSnapshot Snapshot(
            int historyDays,
            decimal? coversPct = null,
            decimal? capturePct = null,
            decimal? sentimentPct = null
        )
        {
            var current = new PeriodWindow(new DateOnly(2026, 8, 7), new DateOnly(2026, 9, 5));
            var comparison = new PeriodWindow(new DateOnly(2026, 7, 8), new DateOnly(2026, 8, 6));
            return new RestaurantContextSnapshot(
                "2026-09-05",
                new SingleLocation("10"),
                current,
                comparison,
                new AccountSection(
                    new MetricPoint(10m, 8m, coversPct),
                    new MetricPoint(0m, null, null),
                    new MetricPoint(0m, null, null),
                    new MetricPoint(10m, 8m, coversPct),
                    []
                ),
                new CampaignsSection([], [], []),
                new OffersSection([], [], []),
                new FeedbackSection(
                    new MetricPoint(70m, 65m, sentimentPct),
                    [],
                    [],
                    0,
                    []
                ),
                new GuestsSection(
                    new MetricPoint(10m, 8m, coversPct),
                    new MetricPoint(0m, null, null),
                    new MetricPoint(0m, null, null),
                    []
                ),
                new CaptureSection(
                    new MetricPoint(20m, 18m, capturePct),
                    new MetricPoint(10m, 12m, capturePct),
                    new MetricPoint(50m, 40m, null),
                    null,
                    []
                ),
                new RecentActionsSection([]),
                new SnapshotMeta(
                    historyDays < 30,
                    historyDays,
                    []
                )
            );
        }
    }
}
