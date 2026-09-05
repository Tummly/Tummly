using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class WeeklyBriefPhase1SectionsTests
    {
        [Fact]
        public void BuildWhatChanged_WithPrior_EmitsSignedPercentRows()
        {
            var current = EmptyMetrics() with
            {
                QrScanEvents = 112,
                FeedbackCount = 54,
                GuestsJoined = 46,
                RedemptionsInWeek = 24,
                UnsubscribesInWeek = 4,
            };
            var prior = EmptyMetrics() with
            {
                QrScanEvents = 100,
                FeedbackCount = 50,
                GuestsJoined = 40,
                RedemptionsInWeek = 25,
                UnsubscribesInWeek = 5,
            };

            var rows = WeeklyBriefPhase1Sections.BuildWhatChanged(current, prior);

            Assert.Equal(5, rows.Count);
            Assert.Equal("QR scans", rows[0].Area);
            Assert.Equal("+12%", rows[0].Change);
            Assert.Equal(
                "More guests are engaging with your QR placements.",
                rows[0].Meaning
            );
            Assert.Equal("Feedback received", rows[1].Area);
            Assert.Equal("+8%", rows[1].Change);
            Assert.Equal("Contactable guests", rows[2].Area);
            Assert.Equal("+15%", rows[2].Change);
            Assert.Equal("Offer redemptions", rows[3].Area);
            Assert.Equal("-4%", rows[3].Change);
            Assert.Equal(
                "Claimed offers may need clearer staff visibility.",
                rows[3].Meaning
            );
            Assert.Equal("Unsubscribes", rows[4].Area);
            Assert.Equal("-20%", rows[4].Change);
            Assert.Equal(
                "Fewer guests opted out of marketing.",
                rows[4].Meaning
            );
        }

        [Fact]
        public void BuildWhatChanged_UnsubscribesWithoutPrior_UsesAbsoluteTotal()
        {
            var current = EmptyMetrics() with { UnsubscribesInWeek = 4 };

            var rows = WeeklyBriefPhase1Sections.BuildWhatChanged(current, prior: null);

            Assert.Single(rows);
            Assert.Equal("Unsubscribes", rows[0].Area);
            Assert.Equal("4 total", rows[0].Change);
            Assert.Equal(
                "Review message frequency and audience relevance.",
                rows[0].Meaning
            );
        }

        [Fact]
        public void BuildWhatChanged_WithoutPrior_UsesAbsoluteTotals()
        {
            var current = EmptyMetrics() with
            {
                QrScanEvents = 12,
                FeedbackCount = 0,
                GuestsJoined = 3,
            };

            var rows = WeeklyBriefPhase1Sections.BuildWhatChanged(current, prior: null);

            Assert.Equal(2, rows.Count);
            Assert.Equal("QR scans", rows[0].Area);
            Assert.Equal("12 total", rows[0].Change);
            Assert.Equal("Contactable guests", rows[1].Area);
            Assert.Equal("3 total", rows[1].Change);
        }

        [Fact]
        public void BuildWhatChanged_AllZero_ReturnsEmpty()
        {
            var rows = WeeklyBriefPhase1Sections.BuildWhatChanged(
                EmptyMetrics(),
                EmptyMetrics()
            );

            Assert.Empty(rows);
        }

        [Fact]
        public void BuildFeedbackSummary_WhenFeedbackPresent_ReturnsFacts()
        {
            var metrics = EmptyMetrics() with
            {
                FeedbackCount = 42,
                PositiveFeedbackCount = 30,
                NeutralFeedbackCount = 8,
                NegativeFeedbackCount = 4,
                NeedsAttentionCount = 6,
                DetectedTagCounts = new Dictionary<string, int>
                {
                    ["packaging"] = 5,
                    ["wait time"] = 4,
                    ["staff"] = 2,
                },
            };

            var summary = WeeklyBriefPhase1Sections.BuildFeedbackSummary(
                metrics,
                periodLabel: "6–12 July"
            );

            Assert.NotNull(summary);
            Assert.Equal(6, summary!.NeedsAttentionCount);
            Assert.Equal(
                "Based on private feedback submitted between 6–12 July.",
                summary.Subtitle
            );
            Assert.Contains("42 private feedback messages", summary.Text);
            Assert.Contains("6 may need follow-up", summary.Text);
            Assert.Contains("packaging", summary.Text);
            Assert.Contains("wait time", summary.Text);
        }

        [Fact]
        public void BuildFeedbackSummary_WhenNoFeedback_ReturnsNull()
        {
            var summary = WeeklyBriefPhase1Sections.BuildFeedbackSummary(
                EmptyMetrics(),
                periodLabel: "6–12 July"
            );

            Assert.Null(summary);
        }

        [Fact]
        public void TryPriorWeekKey_ShiftsWorkspaceWeekBySevenDays()
        {
            Assert.True(
                WeeklyBriefWeekKey.TryPriorWeekKey(
                    "monday:2026-07-06",
                    out var prior
                )
            );
            Assert.Equal("monday:2026-06-29", prior);
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
