using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Helpers
{
    public class WeeklyBriefPhase1MetaTests
    {
        [Theory]
        [InlineData("monday:2026-07-06", "6–12 July")]
        [InlineData("monday:2026-07-28", "28 July – 3 August")]
        [InlineData("2026-W33", "Week 33, 2026")]
        public void FormatPeriod_MatchesWorkspaceAndLegacyForms(
            string weekKey,
            string expected
        )
        {
            Assert.Equal(expected, WeeklyBriefPhase1Meta.FormatPeriod(weekKey));
        }

        [Fact]
        public void ResolveConfidence_High_WhenEnoughDomainsAndActivity()
        {
            var metrics = EmptyMetrics() with
            {
                GuestsJoined = 10,
                QrScanEvents = 8,
                FeedbackCount = 5,
                ClaimsInWeek = 2,
                CampaignsSentInWeek = 1,
            };
            var body = FakeWeeklyBriefProvider.FixtureFor(metrics);

            var (level, copy) = WeeklyBriefPhase1Meta.ResolveConfidence(
                body,
                metrics
            );

            Assert.Equal("high", level);
            Assert.Equal(WeeklyBriefPhase1Meta.ConfidenceHighCopy, copy);
        }

        [Fact]
        public void ResolveConfidence_Medium_WhenTwoDomainsOnly()
        {
            var metrics = EmptyMetrics() with
            {
                GuestsJoined = 4,
                FeedbackCount = 2,
            };
            var body = FakeWeeklyBriefProvider.FixtureFor(metrics);

            var (level, copy) = WeeklyBriefPhase1Meta.ResolveConfidence(
                body,
                metrics
            );

            Assert.Equal("medium", level);
            Assert.Equal(WeeklyBriefPhase1Meta.ConfidenceMediumCopy, copy);
        }

        [Fact]
        public void ResolveConfidence_Low_WhenThinWeek()
        {
            var metrics = EmptyMetrics() with { GuestsJoined = 1 };
            var body = FakeWeeklyBriefProvider.FixtureFor(metrics);

            var (level, copy) = WeeklyBriefPhase1Meta.ResolveConfidence(
                body,
                metrics
            );

            Assert.Equal("low", level);
            Assert.Equal(WeeklyBriefPhase1Meta.ConfidenceLowCopy, copy);
        }

        [Fact]
        public void BuildExecutiveSummary_JoinsHeadlineAndHasDataSections()
        {
            var body = new WeeklyBriefBody(
                Headline: "Steady week.",
                Capture: new WeeklyBriefSection(true, "Scans up.", null),
                Feedback: new WeeklyBriefSection(false, "ignored", null),
                Offers: new WeeklyBriefSection(true, "Claims steady.", null),
                Campaigns: new WeeklyBriefSection(false, "", null),
                WatchNext: []
            );

            Assert.Equal(
                "Steady week. Scans up. Claims steady.",
                WeeklyBriefPhase1Meta.BuildExecutiveSummary(body)
            );
        }

        private static WeeklyBriefMetrics EmptyMetrics()
            => new(
                GuestsJoined: 0,
                QrScanEvents: 0,
                FeedbackCount: 0,
                PositiveFeedbackCount: 0,
                NeutralFeedbackCount: 0,
                NegativeFeedbackCount: 0,
                NeedsAttentionCount: 0,
                DetectedTagCounts: new Dictionary<string, int>(),
                ActiveOffers: 0,
                ClaimsInWeek: 0,
                RedemptionsInWeek: 0,
                CampaignsSentInWeek: 0,
                CampaignRecipientsReached: 0,
                UnsubscribesInWeek: 0
            );
    }
}
