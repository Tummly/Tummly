using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantNextTryCopyTests
    {
        [Fact]
        public void Sentence_PointsAtChangeScopeAndAMoreSpecificAsk()
        {
            Assert.Contains("Change Scope", AssistantNextTryCopy.Sentence, StringComparison.Ordinal);
            Assert.Contains("Owned location", AssistantNextTryCopy.Sentence, StringComparison.Ordinal);
            Assert.Contains("Reporting period", AssistantNextTryCopy.Sentence, StringComparison.Ordinal);
            Assert.Contains("more specific ask", AssistantNextTryCopy.Sentence, StringComparison.Ordinal);
        }

        [Fact]
        public void EmptyGrounded_NamesScopeAndNextTry()
        {
            var result = AssistantLiveAnswerCopy.EmptyGrounded("Camden", "the last 7 days");

            Assert.Equal("No facts at Camden for the last 7 days", result.Title);
            Assert.Contains("nothing to summarise", result.Body, StringComparison.Ordinal);
            Assert.Contains(AssistantNextTryCopy.Sentence, result.Body, StringComparison.Ordinal);
            Assert.Empty(result.Actions);
        }

        [Fact]
        public void CampaignOfferAndRecoveryFailureBodies_IncludeNextTry()
        {
            Assert.Contains(
                AssistantNextTryCopy.Sentence,
                AssistantCampaignDraftPersistCopy.FailureBody("Campaign create"),
                StringComparison.Ordinal
            );
            Assert.Contains(
                AssistantNextTryCopy.Sentence,
                AssistantOfferPathPersistCopy.FailureBody("Offer create"),
                StringComparison.Ordinal
            );
            Assert.Contains(
                AssistantNextTryCopy.Sentence,
                AssistantRecoveryPersistCopy.FailureBody("copy prepare"),
                StringComparison.Ordinal
            );
            Assert.Contains(
                AssistantNextTryCopy.Sentence,
                AssistantRecoveryPersistCopy.EmptyScopeBody("Camden", "the last 7 days"),
                StringComparison.Ordinal
            );
            Assert.Contains(
                AssistantNextTryCopy.Sentence,
                AssistantRecoveryPersistCopy.NoNegativeBody("Camden", "the last 7 days"),
                StringComparison.Ordinal
            );
            Assert.Contains(
                AssistantNextTryCopy.Sentence,
                AssistantAnalysisScope.FailureBody,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void CombinedCreatePartialFailure_InterpretationNamesCampaignStep_NotFullFailureOpener()
        {
            var body = AssistantCombinedCreatePersistCopy.PartialFailureBody(
                "Campaign create",
                "Camden",
                "Percentage discount",
                "10%",
                "30 days after issue",
                "10% off"
            );

            Assert.Contains(
                "## Interpretation\nCampaign was not saved. The Campaign create step failed.",
                body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "I could not save this Campaign with Offer",
                body,
                StringComparison.Ordinal
            );
            Assert.Contains("## Data", body, StringComparison.Ordinal);
            Assert.Contains("Draft (not Active)", body, StringComparison.Ordinal);
            Assert.Contains("## Recommendation", body, StringComparison.Ordinal);
            Assert.Contains(AssistantNextTryCopy.Sentence, body, StringComparison.Ordinal);
        }
    }
}
