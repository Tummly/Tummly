using TummlyBackend.Helpers;
using TummlyBackend.Models;
using Xunit;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Golden question-first body tests from tester samples.
    /// Expected to stay red until ticket 02 (filter + BodyFromEvidence rewrite).
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
    }
}
