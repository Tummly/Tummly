using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantOfferReplaceConfirmGapTests
    {
        [Fact]
        public void CreateOfferReplaceConfirm_StoresPendingIdsAndYesNoOptions()
        {
            var state = AssistantGapTurn.CreateOfferReplaceConfirm(
                campaignId: 12,
                campaignName: "Summer win-back",
                offerId: 34,
                newOfferTitle: "Happy Hour",
                previousOfferTitle: "10% Off Lunch",
                sourceUserMessage: "Attach Happy Hour to Summer win-back campaign",
                assistantTask: AssistantTask.CreateCampaignWithOffer
            );

            Assert.Equal(AssistantGapTurn.KindOfferReplaceConfirm, state.Kind);
            Assert.NotNull(state.OfferReplaceConfirm);
            Assert.Equal(12, state.OfferReplaceConfirm!.CampaignId);
            Assert.Equal(34, state.OfferReplaceConfirm.OfferId);
            Assert.Equal("Summer win-back", state.OfferReplaceConfirm.CampaignName);
            Assert.Equal(
                [AssistantGapAsk.OfferReplaceConfirmYes, AssistantGapAsk.OfferReplaceConfirmNo],
                state.Options
            );

            var roundTrip = AssistantGapTurn.Parse(AssistantGapTurn.Serialize(state));
            Assert.NotNull(roundTrip);
            Assert.Equal(AssistantGapTurn.KindOfferReplaceConfirm, roundTrip!.Kind);
            Assert.Equal(12, roundTrip.OfferReplaceConfirm!.CampaignId);
            Assert.Equal(34, roundTrip.OfferReplaceConfirm.OfferId);
        }

        [Fact]
        public void ReplaceConfirmAccept_KeepsReplaceLocal_AndContinueDoesNot()
        {
            Assert.True(AssistantGapTurn.LooksLikeContinueAnswer("Yes"));
            Assert.False(AssistantGapTurn.LooksLikeContinueAnswer("replace it"));
            Assert.True(AssistantGapTurn.LooksLikeReplaceConfirmAccept("replace it"));
            Assert.True(AssistantGapTurn.LooksLikeReplaceConfirmAccept("Yes"));
            Assert.True(AssistantGapTurn.LooksLikeDeclineAnswer("No"));
            Assert.True(AssistantGapTurn.LooksLikeDeclineAnswer("keep it"));
            Assert.False(AssistantGapTurn.LooksLikeDeclineAnswer("Yes"));
        }

        [Fact]
        public void ForOfferReplaceConfirm_NamesBothOffers()
        {
            var body = AssistantGapAsk.ForOfferReplaceConfirm(
                "Summer win-back",
                "10% Off Lunch",
                "Happy Hour"
            );

            Assert.Contains("Summer win-back", body, StringComparison.Ordinal);
            Assert.Contains("10% Off Lunch", body, StringComparison.Ordinal);
            Assert.Contains("Happy Hour", body, StringComparison.Ordinal);
            Assert.Contains("Yes", body, StringComparison.Ordinal);
            Assert.Contains("No", body, StringComparison.Ordinal);
        }

        [Fact]
        public void CreateOfferRemoveConfirm_StoresPendingAndYesNoOptions()
        {
            var state = AssistantGapTurn.CreateOfferRemoveConfirm(
                campaignId: 12,
                campaignName: "Summer win-back",
                offerTitle: "Happy Hour",
                sourceUserMessage: "Remove the offer from Summer win-back campaign",
                assistantTask: AssistantTask.CreateCampaignWithOffer
            );

            Assert.Equal(AssistantGapTurn.KindOfferRemoveConfirm, state.Kind);
            Assert.NotNull(state.OfferRemoveConfirm);
            Assert.Equal(12, state.OfferRemoveConfirm!.CampaignId);
            Assert.Equal("Happy Hour", state.OfferRemoveConfirm.OfferTitle);
            Assert.Equal(
                [AssistantGapAsk.OfferReplaceConfirmYes, AssistantGapAsk.OfferReplaceConfirmNo],
                state.Options
            );

            var roundTrip = AssistantGapTurn.Parse(AssistantGapTurn.Serialize(state));
            Assert.NotNull(roundTrip);
            Assert.Equal(AssistantGapTurn.KindOfferRemoveConfirm, roundTrip!.Kind);
            Assert.Equal(12, roundTrip.OfferRemoveConfirm!.CampaignId);
        }
    }
}
