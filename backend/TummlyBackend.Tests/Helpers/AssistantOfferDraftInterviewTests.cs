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
            Assert.Contains("### Purchase requirement catalogue", item.Body);
            Assert.Contains("- No purchase required", item.Body);
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
            Assert.Contains("7 days after issue", completed.Body);
            Assert.DoesNotContain("7_days_after_issue", completed.Body);
            var payload = AssistantOfferDraftInterview.ToPayload(completed.State, 42);
            Assert.Equal(42, payload.LocationId);
            Assert.Equal("replacement_item", payload.OfferType);
            Assert.Equal("main course", payload.ReplacementItemText);
            Assert.Equal("Replacement meal", payload.Title);
            Assert.Equal("7_days_after_issue", payload.Validity);
            Assert.Null(payload.AdditionalExclusions);
        }

        [Fact]
        public void BareAskBackReplies_FillTypeClusterAndStaffCopy()
        {
            var typed = AssistantOfferDraftInterview.Apply(null, "Percentage discount");
            var valued = AssistantOfferDraftInterview.Apply(typed.State, "20");
            Assert.False(valued.IsReady);
            Assert.Equal(20m, valued.State.DiscountPercentage);
            Assert.Contains("staff instructions", valued.Body);

            var staffed = AssistantOfferDraftInterview.Apply(
                valued.State,
                "Ask the guest for their code"
            );
            Assert.True(staffed.IsReady);
            Assert.Equal("Ask the guest for their code", staffed.State.StaffInstructions);
            Assert.Contains("Percentage discount", staffed.Body);
        }

        [Fact]
        public void BareFreeItemText_FillsWhenTypeIsLocked()
        {
            var typed = AssistantOfferDraftInterview.Apply(null, "Free item");
            var item = AssistantOfferDraftInterview.Apply(typed.State, "coffee");
            Assert.Equal("coffee", item.State.FreeItemText);
            Assert.Contains("purchase requirement", item.Body);
        }

        [Fact]
        public void NaturalReplies_FillOfferTypeValueAndPurchaseRequirement()
        {
            var item = AssistantOfferDraftInterview.Apply(
                null,
                "Give them a free coffee"
            );
            Assert.Equal("free_item", item.State.OfferType);
            Assert.Equal("coffee", item.State.FreeItemText);

            var requirement = AssistantOfferDraftInterview.Apply(
                item.State,
                "They can buy anything"
            );
            Assert.Equal("with_any_purchase", requirement.State.PurchaseRequirement);

            var completed = AssistantOfferDraftInterview.Apply(
                requirement.State,
                "Draft it now"
            );
            Assert.True(completed.IsReady);
        }

        [Fact]
        public void NaturalExpiryDate_IsNormalised()
        {
            var valued = AssistantOfferDraftInterview.Apply(
                null,
                "Give them £5 off"
            );
            var dated = AssistantOfferDraftInterview.Apply(
                valued.State,
                "It expires on 30 September 2026"
            );

            Assert.Equal("choose_expiry_date", dated.State.Validity);
            Assert.Equal("2026-09-30", dated.State.ExpiryDate);
        }

        [Theory]
        [InlineData("replacement item offer and attach it", null)]
        [InlineData("replacement item: burger", "burger")]
        [InlineData("replace the burger and attach it", "burger")]
        [InlineData("swap the burger and attach it", "burger")]
        [InlineData("replace and attach it", null)]
        public void ReplacementItem_LockstepWithOfferPath_CleansOrOmitsItem(
            string message,
            string? expectedItem
        )
        {
            var turn = AssistantOfferDraftInterview.Apply(null, message);

            Assert.Equal("replacement_item", turn.State.OfferType);
            Assert.Equal(expectedItem, turn.State.ReplacementItemText);
        }

        [Fact]
        public void TypeAlreadyLocked_ReplacementFill_UsesSharedHelper()
        {
            var typed = AssistantOfferDraftInterview.Apply(null, "Replacement item");
            Assert.Equal("replacement_item", typed.State.OfferType);
            Assert.True(string.IsNullOrWhiteSpace(typed.State.ReplacementItemText));

            var filled = AssistantOfferDraftInterview.Apply(
                CloneDraft(typed.State),
                "burger and attach it"
            );
            Assert.Equal("burger", filled.State.ReplacementItemText);

            var ignored = AssistantOfferDraftInterview.Apply(
                CloneDraft(typed.State),
                "attach it to thank you"
            );
            Assert.True(string.IsNullOrWhiteSpace(ignored.State.ReplacementItemText));
        }

        private static AssistantOfferDraftState CloneDraft(AssistantOfferDraftState source)
            => new()
            {
                Target = source.Target,
                OfferType = source.OfferType,
                OfferTypeLabel = source.OfferTypeLabel,
                DiscountPercentage = source.DiscountPercentage,
                DiscountAmount = source.DiscountAmount,
                FreeItemText = source.FreeItemText,
                PurchaseRequirement = source.PurchaseRequirement,
                MinimumSpend = source.MinimumSpend,
                ReplacementItemText = source.ReplacementItemText,
                Title = source.Title,
                Description = source.Description,
                Validity = source.Validity,
                ExpiryDate = source.ExpiryDate,
                StaffInstructions = source.StaffInstructions,
                UsefulOptionalsSkipped = source.UsefulOptionalsSkipped,
            };

    }
}