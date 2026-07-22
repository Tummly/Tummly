using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class LocationGuestProjectionsTests
    {
        [Fact]
        public void DeriveMarketingStatus_ReturnsNotEligibleWhenOffersOptOut()
        {
            Assert.Equal(
                "Not eligible",
                LocationGuestProjections.DeriveMarketingStatus(
                    offersOptOut: true,
                    email: "guest@example.com",
                    mobile: "07700900123"
                )
            );
        }

        [Fact]
        public void DeriveMarketingStatus_PrefersEmailOverSmsWhenBothPresent()
        {
            Assert.Equal(
                "Eligible — Email",
                LocationGuestProjections.DeriveMarketingStatus(
                    offersOptOut: false,
                    email: "guest@example.com",
                    mobile: "07700900123"
                )
            );
        }

        [Fact]
        public void DeriveMarketingStatus_ReturnsSmsWhenOnlyMobilePresent()
        {
            Assert.Equal(
                "Eligible — SMS",
                LocationGuestProjections.DeriveMarketingStatus(
                    offersOptOut: false,
                    email: null,
                    mobile: "07700900123"
                )
            );
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("   ", "  ")]
        [InlineData("", "")]
        public void DeriveMarketingStatus_ReturnsNotEligibleWithoutReachableContact(
            string? email,
            string? mobile
        )
        {
            Assert.Equal(
                "Not eligible",
                LocationGuestProjections.DeriveMarketingStatus(
                    offersOptOut: false,
                    email,
                    mobile
                )
            );
        }

        [Fact]
        public void BuildFeedbackStats_ReturnsEmptyDefaultsWhenNoFeedback()
        {
            var stats = LocationGuestProjections.BuildFeedbackStats(
                Array.Empty<LocationGuestFeedbackFact>()
            );

            Assert.Equal(0, stats.FeedbackSubmissionCount);
            Assert.Equal("none", stats.LatestFeedbackSentiment);
            Assert.Null(stats.LastInteractionAt);
        }

        [Fact]
        public void BuildFeedbackStats_UsesLatestSucceededSentimentAndNewestCreatedAt()
        {
            var olderSucceeded = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
            var newerPending = new DateTime(2026, 7, 12, 12, 0, 0, DateTimeKind.Utc);

            var stats = LocationGuestProjections.BuildFeedbackStats(
                [
                    new LocationGuestFeedbackFact(
                        olderSucceeded,
                        ClassificationStatus.Succeeded,
                        FeedbackSentiment.Positive
                    ),
                    new LocationGuestFeedbackFact(
                        newerPending,
                        ClassificationStatus.Pending,
                        Sentiment: null
                    ),
                ]
            );

            Assert.Equal(2, stats.FeedbackSubmissionCount);
            Assert.Equal("positive", stats.LatestFeedbackSentiment);
            Assert.Equal(newerPending, stats.LastInteractionAt);
        }

        [Fact]
        public void BuildFeedbackStats_ReturnsNoneSentimentWhenNoSucceededClassification()
        {
            var createdAt = new DateTime(2026, 7, 15, 9, 0, 0, DateTimeKind.Utc);

            var stats = LocationGuestProjections.BuildFeedbackStats(
                [
                    new LocationGuestFeedbackFact(
                        createdAt,
                        ClassificationStatus.Pending,
                        Sentiment: null
                    ),
                ]
            );

            Assert.Equal(1, stats.FeedbackSubmissionCount);
            Assert.Equal("none", stats.LatestFeedbackSentiment);
            Assert.Equal(createdAt, stats.LastInteractionAt);
        }

        [Fact]
        public void BuildContactEligibility_ReturnsEligibleAndNotProvidedRows()
        {
            var rows = LocationGuestProjections.BuildContactEligibility(
                offersOptOut: false,
                email: "guest@example.com",
                mobile: null
            );

            Assert.Equal(2, rows.Count);
            Assert.Equal("email", rows[0].Channel);
            Assert.Equal("eligible", rows[0].Status);
            Assert.Equal("consent_captured", rows[0].DetailKind);
            Assert.Null(rows[0].DetailAt);
            Assert.Equal("sms", rows[1].Channel);
            Assert.Equal("not_provided", rows[1].Status);
            Assert.Null(rows[1].DetailKind);
            Assert.Null(rows[1].DetailAt);
        }

        [Fact]
        public void BuildContactEligibility_MarksPresentChannelsUnsubscribedWhenOptedOut()
        {
            var rows = LocationGuestProjections.BuildContactEligibility(
                offersOptOut: true,
                email: "guest@example.com",
                mobile: "07700900123"
            );

            Assert.All(
                rows,
                row =>
                {
                    Assert.Equal("unsubscribed", row.Status);
                    Assert.Equal("unsubscribed", row.DetailKind);
                    Assert.Null(row.DetailAt);
                }
            );
        }
    }
}
