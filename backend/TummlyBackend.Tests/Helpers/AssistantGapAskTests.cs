using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantGapAskTests
    {
        [Fact]
        public void OfferTermAsks_DoNotSpeakSchemaNouns()
        {
            var typeOnly = new AssistantOfferPathTermsState();
            var percentNoValue = new AssistantOfferPathTermsState
            {
                OfferType = "percentage_discount",
            };
            var freeNoUsage = new AssistantOfferPathTermsState
            {
                OfferType = "free_item",
                FreeItemText = "coffee",
            };
            var percentNoEnd = new AssistantOfferPathTermsState
            {
                OfferType = "percentage_discount",
                DiscountPercentage = 50m,
            };
            var attachNoPlacement = new AssistantOfferPathTermsState
            {
                OfferType = "percentage_discount",
                DiscountPercentage = 10m,
                Validity = "14_days_after_issue",
                WantsAttach = true,
            };

            var asks = new[]
            {
                AssistantGapAsk.ForOfferTerms(typeOnly),
                AssistantGapAsk.ForOfferTerms(percentNoValue),
                AssistantGapAsk.ForOfferTerms(freeNoUsage),
                AssistantGapAsk.ForOfferTerms(percentNoEnd),
                AssistantGapAsk.ForOfferTerms(attachNoPlacement),
                AssistantGapAsk.ExplainOfferTerms(percentNoEnd),
                AssistantGapAsk.ChannelAsk,
                AssistantGapAsk.ForLocation("Campaign Draft"),
            };

            foreach (var ask in asks)
            {
                Assert.False(
                    AssistantGapAsk.ContainsForbiddenSchemaNoun(ask),
                    ask
                );
            }
        }

        [Fact]
        public void ExplainEndDate_IsNotTheSameAsTheAsk()
        {
            var terms = new AssistantOfferPathTermsState
            {
                OfferType = "percentage_discount",
                DiscountPercentage = 50m,
            };

            var ask = AssistantGapAsk.ForOfferTerms(terms);
            var explain = AssistantGapAsk.ExplainOfferTerms(terms);

            Assert.Equal(AssistantGapAsk.EndDateAsk, ask);
            Assert.NotEqual(ask, explain);
            Assert.Contains("will not pick an end date", explain, StringComparison.Ordinal);
        }

        [Fact]
        public void NewCreateDuringGap_TwoCreateTargets_DropsEvenIfWordingLooksLikeKeep()
        {
            const string send = "create an offer and help me recover";
            Assert.True(AssistantCreateTargets.Detect(send).Count >= 2);
            Assert.True(AssistantGapAsk.LooksLikeNewCreateDuringGap(send));
        }

        [Fact]
        public void NewCreateDuringGap_HelpCentreHowTo_DoesNotDrop()
        {
            Assert.False(
                AssistantGapAsk.LooksLikeNewCreateDuringGap(
                    "How do I create a campaign?"
                )
            );
        }

        [Fact]
        public void NewCreateDuringGap_Retrieve_DoesNotDrop()
        {
            Assert.False(
                AssistantGapAsk.LooksLikeNewCreateDuringGap("Show me recent feedback")
            );
        }

        [Fact]
        public void ResolveNamedChoice_EmailAndAllEligible_AreUniqueNaturalMatches()
        {
            Assert.Equal(
                "Email",
                AssistantCampaignDraftBind.ResolveNamedChoice(["Email", "SMS"], "email")
            );
            Assert.Equal(
                "All eligible guests",
                AssistantCampaignDraftBind.ResolveNamedChoice(
                    ["All eligible guests", "New guests"],
                    "all eligible guests"
                )
            );
        }
    }
}
