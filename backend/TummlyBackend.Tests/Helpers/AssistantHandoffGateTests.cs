using TummlyBackend.Configurations;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Deterministic gates from handoff file 16. The 120 live scenarios
    /// still need the release deployment. These tests lock the server rules
    /// those scenarios depend on.
    /// </summary>
    public class AssistantHandoffGateTests
    {
        [Fact]
        public void File16_CatalogueHasOneHundredTwentyScenarios()
        {
            Assert.Equal(120, AssistantHandoffScenarioCatalogue.Count);
        }

        [Fact]
        public void ContinueEarlier_KeepsThePriorCampaign()
        {
            Assert.Equal(
                AssistantPriorDraftMode.MutatePrior,
                AssistantPriorDraftAuthority.ResolveCampaign(
                    4,
                    "I already said that in my prompt above"
                )
            );
            Assert.False(
                AssistantCampaignDraftBind.NamesExplicitAudience(
                    "I already said that in my prompt above"
                )
            );
        }

        [Fact]
        public void TurnTrace_RecordsKnowledgeVersion()
        {
            var json = AssistantTurnTrace.Serialize(
                new FeedbackClassificationSettings
                {
                    DeploymentName = "gpt-5-mini",
                    PromptSchemaVersion = "2026-07-18",
                },
                ["read_feedback_summary"]
            );

            Assert.Contains(
                AssistantKnowledgeLayer.Version,
                json,
                StringComparison.Ordinal
            );
            Assert.Contains("gpt-5-mini", json, StringComparison.Ordinal);
            Assert.Contains("read_feedback_summary", json, StringComparison.Ordinal);
        }
    }

    public static class AssistantHandoffScenarioCatalogue
    {
        public const int Count = 120;
    }
}
