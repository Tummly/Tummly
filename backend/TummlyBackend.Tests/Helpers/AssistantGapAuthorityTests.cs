using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantGapAuthorityTests
    {
        private static AssistantGapState OfferTermsGap()
            => AssistantGapTurn.CreateCombinedOfferTerms(
                "Create a 25% off lunch offer",
                new AssistantOfferPathTermsState
                {
                    OfferType = "percentage_discount",
                    DiscountPercentage = 25m,
                },
                AssistantTask.OfferPath
            );

        private static AssistantGapState CreateTargetGap()
            => AssistantGapTurn.CreateTarget(
                [AssistantCreateTargets.Campaign, AssistantCreateTargets.Recovery],
                "help me draft something",
                AssistantTask.CreateCampaignDraft
            );

        [Theory]
        [InlineData("Make the offer valid to 10 days")]
        [InlineData("Make the offer valid for 10 days")]
        [InlineData("make it valid for 10 days")]
        [InlineData("14 days after issue")]
        [InlineData("valid till 7th oct. 2027")]
        public void Decide_OfferTerms_ValidityFill_Continues(string send)
        {
            Assert.Equal(
                AssistantGapAuthorityDecision.ContinueGap,
                AssistantGapAuthority.Decide(OfferTermsGap(), send)
            );
        }

        [Fact]
        public void Decide_OfferTerms_DraftARecovery_Drops()
        {
            Assert.Equal(
                AssistantGapAuthorityDecision.DropForNewCreate,
                AssistantGapAuthority.Decide(
                    OfferTermsGap(),
                    "Draft a recovery for recent feedback"
                )
            );
        }

        [Fact]
        public void Decide_OfferTerms_Cancel_IsCancel()
        {
            Assert.Equal(
                AssistantGapAuthorityDecision.Cancel,
                AssistantGapAuthority.Decide(OfferTermsGap(), "never mind")
            );
        }

        [Fact]
        public void Decide_OfferTerms_ShowMeFeedback_Keeps()
        {
            Assert.Equal(
                AssistantGapAuthorityDecision.KeepGapAnswerRetrieveOrRefuse,
                AssistantGapAuthority.Decide(
                    OfferTermsGap(),
                    "Show me recent feedback"
                )
            );
        }

        [Fact]
        public void Decide_CreateTarget_CampaignToRecover_Continues()
        {
            Assert.Equal(
                AssistantGapAuthorityDecision.ContinueGap,
                AssistantGapAuthority.Decide(
                    CreateTargetGap(),
                    "Campaign to recover 10 eligible guests."
                )
            );
        }

        [Fact]
        public void Decide_OfferTerms_TwoCreateTargets_Drops()
        {
            Assert.Equal(
                AssistantGapAuthorityDecision.DropForNewCreate,
                AssistantGapAuthority.Decide(
                    OfferTermsGap(),
                    "create an offer and help me recover"
                )
            );
        }

        [Fact]
        public void Decide_ChannelBind_Email_Continues()
        {
            var gap = new AssistantGapState
            {
                Kind = AssistantGapTurn.KindChannel,
                AssistantTask = AssistantTask.CreateCampaignDraft,
                Options = ["Email", "SMS"],
                SourceUserMessage = "Draft a campaign",
            };

            Assert.Equal(
                AssistantGapAuthorityDecision.ContinueGap,
                AssistantGapAuthority.Decide(gap, "email")
            );
        }
    }
}
