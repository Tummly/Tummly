using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantLiveAnswerResolveTests
    {
        [Fact]
        public void Resolve_ProviderFailed_CreateAsk_ReturnsCreateCampaignDraftTask()
        {
            var resolved = AssistantLiveAnswerResolve.Resolve(
                new AssistantLiveAnswerResult.Failed(Retryable: true),
                "Help me create a Campaign",
                "Flat Iron Soho",
                "the last 7 days",
                AssistantRetrievedEvidence.Empty,
                allowLocalRetrieveFallback: true
            );

            var succeeded = Assert.IsType<AssistantLiveAnswerResult.Succeeded>(resolved);
            Assert.Equal(AssistantTask.CreateCampaignDraft, succeeded.AssistantTask);
            Assert.Equal(AssistantMessageClass.Grounded, succeeded.Class);
        }

        [Fact]
        public void Resolve_ProviderRetrieve_CreateAsk_OverridesToCreateCampaignDraft()
        {
            var resolved = AssistantLiveAnswerResolve.Resolve(
                new AssistantLiveAnswerResult.Succeeded(
                    AssistantMessageClass.Grounded,
                    "No facts",
                    "There is nothing to summarise.",
                    [],
                    AssistantTask.Retrieve
                ),
                "Create a Campaign for recent guests.",
                "Flat Iron Soho",
                "the last 7 days",
                AssistantRetrievedEvidence.Empty,
                allowLocalRetrieveFallback: true
            );

            var succeeded = Assert.IsType<AssistantLiveAnswerResult.Succeeded>(resolved);
            Assert.Equal(AssistantTask.CreateCampaignDraft, succeeded.AssistantTask);
        }

        [Fact]
        public void Resolve_ProviderFailed_FeedbackAsk_UsesLocalGrounded()
        {
            var evidence = new AssistantRetrievedEvidence(
                new AssistantFeedbackEvidence(
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
                ),
                AssistantOffersEvidence.Empty,
                AssistantCampaignsEvidence.Empty,
                AssistantCaptureEvidence.Empty,
                AssistantHomeKpiEvidence.Empty,
                AssistantGuestsEvidence.Empty
            );

            var resolved = AssistantLiveAnswerResolve.Resolve(
                new AssistantLiveAnswerResult.Failed(Retryable: true),
                "How is Feedback looking for this Location?",
                "Flat Iron Soho",
                "the last 7 days",
                evidence,
                allowLocalRetrieveFallback: true
            );

            var succeeded = Assert.IsType<AssistantLiveAnswerResult.Succeeded>(resolved);
            Assert.Equal(AssistantTask.Retrieve, succeeded.AssistantTask);
            Assert.Equal(AssistantMessageClass.Grounded, succeeded.Class);
            Assert.Contains("Neutral", succeeded.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Positive: 0", succeeded.Body, StringComparison.Ordinal);
        }

        [Fact]
        public void Resolve_ProviderRetrieve_CreateWithOfferAsk_KeepsRetrieve()
        {
            var resolved = AssistantLiveAnswerResolve.Resolve(
                new AssistantLiveAnswerResult.Succeeded(
                    AssistantMessageClass.Grounded,
                    "Feedback",
                    "Retrieved facts.",
                    [],
                    AssistantTask.Retrieve
                ),
                "Draft an Email Campaign with a 10% off Offer for eligible guests at Camden",
                "Camden",
                "the last 7 days",
                AssistantRetrievedEvidence.Empty,
                allowLocalRetrieveFallback: true
            );

            var succeeded = Assert.IsType<AssistantLiveAnswerResult.Succeeded>(resolved);
            Assert.Equal(AssistantTask.Retrieve, succeeded.AssistantTask);
        }

        [Fact]
        public void Resolve_ProviderFailed_CompareAll_KeepsFailure()
        {
            var resolved = AssistantLiveAnswerResolve.Resolve(
                new AssistantLiveAnswerResult.Failed(Retryable: true),
                "Summarise recent feedback",
                "All Locations",
                "the last 7 days",
                AssistantRetrievedEvidence.Empty,
                allowLocalRetrieveFallback: false
            );

            Assert.IsType<AssistantLiveAnswerResult.Failed>(resolved);
        }
    }
}
