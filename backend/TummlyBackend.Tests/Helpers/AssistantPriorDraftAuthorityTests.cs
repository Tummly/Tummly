using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantPriorDraftAuthorityTests
    {
        [Theory]
        [InlineData("change audience to sms eligible only")]
        [InlineData("Change the campaign audience to email eligible only")]
        [InlineData("sms eligible only")]
        [InlineData("email eligible only")]
        public void ResolveCampaign_PriorId_MutateFamily_IsMutatePrior(string ask)
        {
            Assert.Equal(
                AssistantPriorDraftMode.MutatePrior,
                AssistantPriorDraftAuthority.ResolveCampaign(4, ask)
            );
            Assert.Equal(
                AssistantTask.CreateCampaignDraft,
                AssistantTaskClassification.Classify(ask)
            );
        }

        [Fact]
        public void ResolveCampaign_MutateFamily_NoPrior_IsNoPrior()
        {
            Assert.Equal(
                AssistantPriorDraftMode.NoPrior,
                AssistantPriorDraftAuthority.ResolveCampaign(
                    null,
                    "change audience to sms eligible only"
                )
            );
        }

        [Fact]
        public void ResolveCampaign_ExplicitCreate_IsCreateNew_EvenWithPrior()
        {
            Assert.Equal(
                AssistantPriorDraftMode.CreateNew,
                AssistantPriorDraftAuthority.ResolveCampaign(
                    4,
                    "Create a campaign to thank guests"
                )
            );
        }

        [Fact]
        public void ResolveCampaign_ExplicitAnother_IsCreateNew()
        {
            Assert.Equal(
                AssistantPriorDraftMode.CreateNew,
                AssistantPriorDraftAuthority.ResolveCampaign(
                    4,
                    "create another campaign for SMS"
                )
            );
        }

        [Theory]
        [InlineData("change the offer to 15%")]
        [InlineData("change offer to 85%")]
        [InlineData("update offer validity to 30 days after issue")]
        [InlineData("set the discount to 20%")]
        public void ResolveOffer_PriorId_MutateFamily_IsMutatePrior(string ask)
        {
            Assert.Equal(
                AssistantPriorDraftMode.MutatePrior,
                AssistantPriorDraftAuthority.ResolveOffer(2, ask)
            );
            Assert.Equal(
                AssistantTask.OfferPath,
                AssistantTaskClassification.Classify(ask)
            );
        }

        [Fact]
        public void ResolveOffer_MutateFamily_NoPrior_IsNoPrior()
        {
            Assert.Equal(
                AssistantPriorDraftMode.NoPrior,
                AssistantPriorDraftAuthority.ResolveOffer(null, "change the offer to 15%")
            );
        }

        [Fact]
        public void ResolveOffer_FreshCreate_IsCreateNew()
        {
            Assert.Equal(
                AssistantPriorDraftMode.CreateNew,
                AssistantPriorDraftAuthority.ResolveOffer(
                    2,
                    "Create an offer: 10% off, valid for 14 days"
                )
            );
        }
    }
}
