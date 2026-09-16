using TummlyBackend.Helpers;
using TummlyBackend.Models;
using Xunit;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Golden question-first body tests from tester samples (ticket 02).
    /// </summary>
    public class AssistantLiveAnswerCopyQuestionFirstTests
    {
        [Fact]
        public void GroundedFromEvidence_ActiveCampaignsAsk_OmitsUnrelatedDomains()
        {
            var evidence = new AssistantRetrievedEvidence(
                Feedback: NeutralOnlyFeedback(),
                Offers: AssistantOffersEvidence.Empty,
                Campaigns: OneDraftNoInFlight(),
                Capture: NonEmptyCapture(),
                Home: AssistantHomeKpiEvidence.Empty,
                Guests: AssistantGuestsEvidence.Empty
            );

            var result = AssistantLiveAnswerCopy.GroundedFromEvidence(
                "Are there any active campaigns for this Location?",
                "Camden",
                "today",
                evidence
            );

            Assert.Equal(AssistantMessageClass.Grounded, result.Class);
            Assert.StartsWith(
                "No, there are no active or scheduled Campaigns for Camden.",
                result.Body
            );
            Assert.Contains(
                "You do have 1 Draft Campaign if you want to review it.",
                result.Body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "feedback",
                result.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain(
                "eligible",
                result.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain(
                "QR scans",
                result.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain(
                "Capture",
                result.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.True(result.Actions.Count <= 1);
        }

        [Fact]
        public void GroundedFromEvidence_SingleNeutralFeedback_OmitsZeroBuckets()
        {
            var evidence = AssistantRetrievedEvidence.FromFeedback(
                NeutralOnlyFeedback()
            );

            var result = AssistantLiveAnswerCopy.GroundedFromEvidence(
                "Summarise feedback for me",
                "Camden",
                "the last 7 days",
                evidence
            );

            Assert.Equal(AssistantMessageClass.Grounded, result.Class);
            Assert.Contains(
                "You received 1 piece of Feedback, classified as Neutral.",
                result.Body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "0 positive",
                result.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain(
                "0 negative",
                result.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain(
                "Positive: 0",
                result.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain(
                "Negative: 0",
                result.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain(
                "Succeeded classification",
                result.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.True(result.Actions.Count <= 1);
        }

        [Fact]
        public void GroundedFromEvidence_CaptureQrAsk_IsQrShaped()
        {
            var evidence = new AssistantRetrievedEvidence(
                Feedback: NeutralOnlyFeedback(),
                Offers: AssistantOffersEvidence.Empty,
                Campaigns: AssistantCampaignsEvidence.Empty,
                Capture: NonEmptyCapture(),
                Home: AssistantHomeKpiEvidence.Empty,
                Guests: AssistantGuestsEvidence.Empty
            );

            var result = AssistantLiveAnswerCopy.GroundedFromEvidence(
                "Have we had any QR scans today?",
                "Camden",
                "today",
                evidence
            );

            Assert.Equal(AssistantMessageClass.Grounded, result.Class);
            Assert.Contains(
                "Camden had 3 QR scans over today.",
                result.Body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "feedback submitted",
                result.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain(
                "marketing opt-ins",
                result.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain(
                "Previous window",
                result.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain(
                "feedback item",
                result.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.True(result.Actions.Count <= 1);
        }

        [Fact]
        public void GroundedFromEvidence_OffersRedemptionsAsk_IsOffersPerformanceShaped()
        {
            var evidence = new AssistantRetrievedEvidence(
                Feedback: NeutralOnlyFeedback(),
                Offers: OffersWithRedemptions(),
                Campaigns: AssistantCampaignsEvidence.Empty,
                Capture: NonEmptyCapture(),
                Home: AssistantHomeKpiEvidence.Empty,
                Guests: AssistantGuestsEvidence.Empty
            );

            var result = AssistantLiveAnswerCopy.GroundedFromEvidence(
                "Are there any Offer Redemptions today?",
                "Camden",
                "today",
                evidence
            );

            Assert.Equal(AssistantMessageClass.Grounded, result.Class);
            Assert.Contains(
                "Offers Performance over today: 4 redemptions.",
                result.Body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "feedback",
                result.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain(
                "QR scans",
                result.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain(
                "Capture",
                result.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.True(result.Actions.Count <= 1);
        }

        [Fact]
        public void GroundedFromEvidence_PerformanceAsk_UsesOperatorHomeLanguage()
        {
            var evidence = new AssistantRetrievedEvidence(
                Feedback: AssistantFeedbackEvidence.Empty,
                Offers: AssistantOffersEvidence.Empty,
                Campaigns: AssistantCampaignsEvidence.Empty,
                Capture: AssistantCaptureEvidence.Empty,
                Home: new AssistantHomeKpiEvidence(2, 0, 5, 0, 7, 0),
                Guests: AssistantGuestsEvidence.Empty
            );

            var result = AssistantLiveAnswerCopy.GroundedFromEvidence(
                "What is the Performance overview?",
                "Camden",
                "the last 7 days",
                evidence
            );

            Assert.Equal(AssistantMessageClass.Grounded, result.Class);
            Assert.Contains(
                "2 Feedback submitted",
                result.Body,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "5 Guests joined",
                result.Body,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "7 QR scans",
                result.Body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "feedbackSubmitted",
                result.Body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "guestsJoined",
                result.Body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "qrScans",
                result.Body,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void GroundedFromEvidence_StubCountsAsk_UsesOperatorLanguage()
        {
            var evidence = new AssistantRetrievedEvidence(
                Feedback: NeutralOnlyFeedback(),
                Offers: AssistantOffersEvidence.Empty,
                Campaigns: AssistantCampaignsEvidence.Empty,
                Capture: AssistantCaptureEvidence.Empty,
                Home: AssistantHomeKpiEvidence.Empty,
                Guests: AssistantGuestsEvidence.Empty
            );

            var result = AssistantLiveAnswerCopy.GroundedFromEvidence(
                "How many Home offer redemptions and Feedback submitted?",
                "Camden",
                "today",
                evidence
            );

            Assert.DoesNotContain(
                "stub",
                result.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain(
                "offerClaims",
                result.Body,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Offers Performance",
                result.Body,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void FilterEvidence_CampaignsActive_KeepsCampaignsOnly()
        {
            var evidence = new AssistantRetrievedEvidence(
                Feedback: NeutralOnlyFeedback(),
                Offers: OffersWithRedemptions(),
                Campaigns: OneDraftNoInFlight(),
                Capture: NonEmptyCapture(),
                Home: new AssistantHomeKpiEvidence(1, 0, 2, 0, 3, 0),
                Guests: AssistantGuestsEvidence.Empty
            );

            var filtered = AssistantAskFocus.FilterEvidence(
                AssistantAskFocusKind.CampaignsActive,
                evidence
            );

            Assert.True(filtered.Feedback.IsEmpty);
            Assert.True(filtered.Offers.IsEmpty);
            Assert.True(filtered.Campaigns.HasCampaignFacts);
            Assert.False(filtered.Capture.HasSnapshotFacts);
            Assert.True(filtered.Home.IsEmpty);
        }

        [Fact]
        public void FilterEvidence_OffersRedemptions_DropsFeedback()
        {
            var evidence = new AssistantRetrievedEvidence(
                Feedback: NeutralOnlyFeedback(),
                Offers: OffersWithRedemptions(),
                Campaigns: OneDraftNoInFlight(),
                Capture: NonEmptyCapture(),
                Home: AssistantHomeKpiEvidence.Empty,
                Guests: AssistantGuestsEvidence.Empty
            );

            var filtered = AssistantAskFocus.FilterEvidence(
                AssistantAskFocusKind.OffersRedemptions,
                evidence
            );

            Assert.True(filtered.Feedback.IsEmpty);
            Assert.True(filtered.Offers.HasPerformanceFacts);
            Assert.True(filtered.Campaigns.IsEmpty);
            Assert.False(filtered.Capture.HasSnapshotFacts);
        }

        [Fact]
        public void FilterEvidence_CaptureQr_DropsFeedbackUnlessAlsoAsked()
        {
            var evidence = new AssistantRetrievedEvidence(
                Feedback: NeutralOnlyFeedback(),
                Offers: AssistantOffersEvidence.Empty,
                Campaigns: AssistantCampaignsEvidence.Empty,
                Capture: NonEmptyCapture(),
                Home: AssistantHomeKpiEvidence.Empty,
                Guests: AssistantGuestsEvidence.Empty
            );

            var filtered = AssistantAskFocus.FilterEvidence(
                AssistantAskFocusKind.CaptureQr,
                evidence
            );

            Assert.True(filtered.Feedback.IsEmpty);
            Assert.True(filtered.Capture.HasSnapshotFacts);
            Assert.Equal(0, filtered.Capture.FeedbackSubmitted);
            Assert.Equal(0, filtered.Capture.MarketingOptIns);
            Assert.Equal(0, filtered.Capture.QrScansPrevious);
            Assert.All(
                filtered.Capture.QrRows,
                row =>
                {
                    Assert.Equal(0, row.FeedbackSubmitted);
                    Assert.Equal(0, row.MarketingOptIns);
                }
            );
        }

        private static AssistantFeedbackEvidence NeutralOnlyFeedback()
            => new(
                TotalCount: 1,
                SampleCount: 1,
                SucceededPositive: 0,
                SucceededNeutral: 1,
                SucceededNegative: 0,
                NeedsAttention: 0,
                TagCounts: [],
                Rows: [],
                GuestRows: [],
                Placeholder4GuestRows: [],
                ContactRedactionTokens: []
            );

        private static AssistantCampaignsEvidence OneDraftNoInFlight()
            => new(
                ListTotalCount: 1,
                ListSampleCount: 1,
                InFlightScheduled: 0,
                InFlightSending: 0,
                MessagesSentAccepted: 0,
                Rows:
                [
                    new AssistantCampaignListRow(
                        Id: 10,
                        Name: "Weekend Draft",
                        Status: "draft",
                        CreatedAt: DateTime.UtcNow,
                        UpdatedAt: DateTime.UtcNow,
                        OfferId: null
                    ),
                ],
                Eligibility:
                [
                    new AssistantCampaignEligibilityRow(
                        CampaignId: 10,
                        AudienceKey: "recent-guests",
                        Evaluable: true,
                        Matched: 12,
                        CurrentlyEligible: 8,
                        Excluded: 4
                    ),
                ],
                Details: []
            );

        private static AssistantCaptureEvidence NonEmptyCapture()
            => new(
                QrScans: 3,
                QrScansPrevious: 1,
                FeedbackSubmitted: 2,
                FeedbackSubmittedPrevious: 0,
                MarketingOptIns: 1,
                MarketingOptInsPrevious: 0,
                QrRows:
                [
                    new AssistantCaptureQrRow(
                        QrCodeId: 1,
                        QrType: "Table Tent",
                        Status: "Active",
                        QrScans: 3,
                        FeedbackSubmitted: 2,
                        MarketingOptIns: 1
                    ),
                ]
            );

        private static AssistantOffersEvidence OffersWithRedemptions()
            => new(
                CatalogTotalCount: 0,
                CatalogSampleCount: 0,
                ActiveOffers: 0,
                OffersIssued: 2,
                Claims: 5,
                Redemptions: 4,
                ClaimToRedemptionRate: 0.8,
                Catalog: [],
                PerOfferMetrics: [],
                LinkedCampaigns: [],
                ClaimLogs: [],
                RedemptionLogs: []
            );
    }
}
