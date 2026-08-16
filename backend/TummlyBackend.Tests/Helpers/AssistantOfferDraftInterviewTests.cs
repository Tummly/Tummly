using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantOfferDraftInterviewTests
    {
        [Fact]
        public void DraftItNow_DoesNotSkipFreeItemPersistMinimum()
        {
            var typed = AssistantOfferDraftInterview.Apply(null, "Free item");
            var item = AssistantOfferDraftInterview.Apply(
                typed.State,
                "Free item: coffee; Draft it now"
            );

            Assert.False(item.IsReady);
            Assert.Contains("purchase requirement", item.Body);
        }

        [Fact]
        public void ChooseExpiryDate_RemainsMustAskUntilDateIsNamed()
        {
            var typed = AssistantOfferDraftInterview.Apply(null, "Fixed discount");
            var valued = AssistantOfferDraftInterview.Apply(
                typed.State,
                "£5, choose expiry date, Draft it now"
            );

            Assert.False(valued.IsReady);
            Assert.Contains("expiry date", valued.Body);

            var dated = AssistantOfferDraftInterview.Apply(
                valued.State,
                "2026-09-30"
            );
            Assert.True(dated.IsReady);
            Assert.Equal("2026-09-30", dated.State.ExpiryDate);
        }

        [Fact]
        public void ReadyReplacementOffer_MapsCatalogPayload()
        {
            var typed = AssistantOfferDraftInterview.Apply(null, "Replacement item");
            var completed = AssistantOfferDraftInterview.Apply(
                typed.State,
                "Replacement item: main course; title: Replacement meal; description: We will replace your meal; staff instructions: Check the code"
            );

            Assert.True(completed.IsReady);
            var payload = AssistantOfferDraftInterview.ToPayload(completed.State, 42);
            Assert.Equal(42, payload.LocationId);
            Assert.Equal("replacement_item", payload.OfferType);
            Assert.Equal("main course", payload.ReplacementItemText);
            Assert.Equal("Replacement meal", payload.Title);
            Assert.Equal("7_days_after_issue", payload.Validity);
            Assert.Null(payload.AdditionalExclusions);
        }
    }
}
