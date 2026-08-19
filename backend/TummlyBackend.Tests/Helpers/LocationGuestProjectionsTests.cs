using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class LocationGuestProjectionsTests
    {
        [Fact]
        public void FromFeedbackOffersOptOut_MapsTrueToOptedOut()
        {
            Assert.Equal(
                LocationGuestMarketingPreference.OptedOut,
                LocationGuestMarketingPreferenceExtensions.FromFeedbackOffersOptOut(
                    true
                )
            );
        }

        [Fact]
        public void FromFeedbackOffersOptOut_MapsFalseToAllowed()
        {
            Assert.Equal(
                LocationGuestMarketingPreference.Allowed,
                LocationGuestMarketingPreferenceExtensions.FromFeedbackOffersOptOut(
                    false
                )
            );
        }

        [Fact]
        public void DeriveMarketingStatus_ReturnsNotEligibleWhenOptedOut()
        {
            Assert.Equal(
                "Not eligible",
                LocationGuestProjections.DeriveMarketingStatus(
                    LocationGuestMarketingPreference.OptedOut,
                    email: "guest@example.com",
                    mobile: "07700900123"
                )
            );
        }

        [Fact]
        public void DeriveMarketingStatus_ReturnsNotEligibleWhenNotRecorded()
        {
            Assert.Equal(
                "Not eligible",
                LocationGuestProjections.DeriveMarketingStatus(
                    LocationGuestMarketingPreference.NotRecorded,
                    email: "guest@example.com",
                    mobile: "07700900123"
                )
            );
        }

        [Fact]
        public void DeriveMarketingStatus_PrefersEmailOverSmsWhenAllowed()
        {
            Assert.Equal(
                "Eligible — Email",
                LocationGuestProjections.DeriveMarketingStatus(
                    LocationGuestMarketingPreference.Allowed,
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
                    LocationGuestMarketingPreference.Allowed,
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
                    LocationGuestMarketingPreference.Allowed,
                    email,
                    mobile
                )
            );
        }

        [Fact]
        public void IsMarketingEligible_IsFalseWhenNotRecordedEvenWithContact()
        {
            Assert.False(
                LocationGuestProjections.IsMarketingEligible(
                    LocationGuestMarketingPreference.NotRecorded,
                    email: "guest@example.com",
                    mobile: "07700900123"
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
            var consentAt = new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc);
            var rows = LocationGuestProjections.BuildContactEligibility(
                LocationGuestMarketingPreference.Allowed,
                email: "guest@example.com",
                mobile: null,
                consentCapturedAt: consentAt
            );

            Assert.Equal(2, rows.Count);
            Assert.Equal("email", rows[0].Channel);
            Assert.Equal("eligible", rows[0].Status);
            Assert.Equal("consent_captured", rows[0].DetailKind);
            Assert.Equal(consentAt, rows[0].DetailAt);
            Assert.Equal("sms", rows[1].Channel);
            Assert.Equal("not_provided", rows[1].Status);
            Assert.Null(rows[1].DetailKind);
            Assert.Null(rows[1].DetailAt);
        }

        [Fact]
        public void BuildContactEligibility_MarksPresentChannelsUnsubscribedWhenOptedOut()
        {
            var unsubscribedAt = new DateTime(2026, 6, 1, 9, 30, 0, DateTimeKind.Utc);
            var rows = LocationGuestProjections.BuildContactEligibility(
                LocationGuestMarketingPreference.OptedOut,
                email: "guest@example.com",
                mobile: "07700900123",
                consentCapturedAt: unsubscribedAt
            );

            Assert.All(
                rows,
                row =>
                {
                    Assert.Equal("unsubscribed", row.Status);
                    Assert.Equal("unsubscribed", row.DetailKind);
                    Assert.Equal(unsubscribedAt, row.DetailAt);
                }
            );
        }

        [Fact]
        public void BuildContactEligibility_MarksPresentChannelsNotRecordedWhenPreferenceIsNotRecorded()
        {
            var capturedAt = new DateTime(2026, 6, 1, 9, 30, 0, DateTimeKind.Utc);
            var rows = LocationGuestProjections.BuildContactEligibility(
                LocationGuestMarketingPreference.NotRecorded,
                email: "guest@example.com",
                mobile: "07700900123",
                consentCapturedAt: capturedAt
            );

            Assert.All(
                rows,
                row =>
                {
                    Assert.Equal("not_recorded", row.Status);
                    Assert.Equal("not_recorded", row.DetailKind);
                    Assert.Equal(capturedAt, row.DetailAt);
                }
            );
        }

        [Fact]
        public void BuildContactEligibility_KeepsMissingContactAsNotProvidedWhenNotRecorded()
        {
            var rows = LocationGuestProjections.BuildContactEligibility(
                LocationGuestMarketingPreference.NotRecorded,
                email: "guest@example.com",
                mobile: null
            );

            Assert.Equal("not_recorded", rows[0].Status);
            Assert.Equal("not_provided", rows[1].Status);
            Assert.Null(rows[1].DetailKind);
        }

        [Fact]
        public void ResolveOffersConsentDetailAt_ReturnsNullWhenNoFeedback()
        {
            Assert.Null(
                LocationGuestProjections.ResolveOffersConsentDetailAt(
                    LocationGuestMarketingPreference.Allowed,
                    feedbacks: Array.Empty<LocationGuestOffersOptOutFact>()
                )
            );
        }

        [Fact]
        public void ResolveOffersConsentDetailAt_KeepsLatestFeedbackStreakWhenNotRecorded()
        {
            var firstConsent = new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc);
            var laterAffirmation = new DateTime(2026, 7, 20, 14, 22, 0, DateTimeKind.Utc);

            var detailAt = LocationGuestProjections.ResolveOffersConsentDetailAt(
                LocationGuestMarketingPreference.NotRecorded,
                feedbacks:
                [
                    new LocationGuestOffersOptOutFact(laterAffirmation, OffersOptOut: false),
                    new LocationGuestOffersOptOutFact(firstConsent, OffersOptOut: false),
                ]
            );

            Assert.Equal(firstConsent, detailAt);
        }

        [Fact]
        public void ResolveOffersConsentDetailAt_KeepsAllowedStreakWhenOperatorOptedOut()
        {
            var firstConsent = new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc);
            var laterAffirmation = new DateTime(2026, 7, 20, 14, 22, 0, DateTimeKind.Utc);

            var detailAt = LocationGuestProjections.ResolveOffersConsentDetailAt(
                LocationGuestMarketingPreference.OptedOut,
                feedbacks:
                [
                    new LocationGuestOffersOptOutFact(laterAffirmation, OffersOptOut: false),
                    new LocationGuestOffersOptOutFact(firstConsent, OffersOptOut: false),
                ]
            );

            Assert.Equal(firstConsent, detailAt);
        }

        [Fact]
        public void ResolveOffersConsentDetailAt_UsesOldestInCurrentPermissionStreak()
        {
            // Always opted in across submissions → first consent time, not latest.
            var firstConsent = new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc);
            var laterAffirmation = new DateTime(2026, 7, 20, 14, 22, 0, DateTimeKind.Utc);

            var detailAt = LocationGuestProjections.ResolveOffersConsentDetailAt(
                LocationGuestMarketingPreference.Allowed,
                feedbacks:
                [
                    new LocationGuestOffersOptOutFact(laterAffirmation, OffersOptOut: false),
                    new LocationGuestOffersOptOutFact(firstConsent, OffersOptOut: false),
                ]
            );

            Assert.Equal(firstConsent, detailAt);
        }

        [Fact]
        public void ResolveOffersConsentDetailAt_UsesReOptInTimeAfterPriorOptOut()
        {
            // Morgan: first submission opts out, later submission opts in.
            var optedOutAt = new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc);
            var reOptedInAt = new DateTime(2026, 7, 20, 14, 22, 0, DateTimeKind.Utc);

            var detailAt = LocationGuestProjections.ResolveOffersConsentDetailAt(
                LocationGuestMarketingPreference.Allowed,
                feedbacks:
                [
                    new LocationGuestOffersOptOutFact(reOptedInAt, OffersOptOut: false),
                    new LocationGuestOffersOptOutFact(optedOutAt, OffersOptOut: true),
                ]
            );

            Assert.Equal(reOptedInAt, detailAt);
        }

        [Fact]
        public void ResolveOffersConsentDetailAt_UsesOptOutTimeWhenCurrentlyUnsubscribed()
        {
            var consentedAt = new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc);
            var optedOutAt = new DateTime(2026, 6, 1, 9, 30, 0, DateTimeKind.Utc);

            var detailAt = LocationGuestProjections.ResolveOffersConsentDetailAt(
                LocationGuestMarketingPreference.OptedOut,
                feedbacks:
                [
                    new LocationGuestOffersOptOutFact(optedOutAt, OffersOptOut: true),
                    new LocationGuestOffersOptOutFact(consentedAt, OffersOptOut: false),
                ]
            );

            Assert.Equal(optedOutAt, detailAt);
        }
    }
}
