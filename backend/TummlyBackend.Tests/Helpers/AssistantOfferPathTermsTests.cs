using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantOfferPathTermsTests
    {
        private static readonly DateTime Utc2026 = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Parse_CanonicalTwentyFivePercent_IsComplete()
        {
            var state = AssistantOfferPathTerms.Parse(
                "Create a 25% Offer valid 30 days after issue",
                Utc2026
            );

            Assert.True(AssistantOfferPathTerms.IsComplete(state));
            Assert.Equal("percentage_discount", state.OfferType);
            Assert.Equal(25m, state.DiscountPercentage);
            Assert.Equal("30_days_after_issue", state.Validity);
            AssistantOfferPathTerms.ProposeCopy(state);
            Assert.Equal("25% off", state.Title);
            Assert.Equal("Save 25%.", state.Description);
            Assert.DoesNotContain("visit", state.Title, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("order", state.Description, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(
                OfferIssueSources.GuestFormThankYou,
                AssistantOfferPathTermsState.PlacementGuestFormThankYou
            );
        }

        [Fact]
        public void Parse_YearEnd_Uses31DecemberOfUtcYear()
        {
            var state = AssistantOfferPathTerms.Parse(
                "Create a 25% Offer valid until the end of the year",
                Utc2026
            );

            Assert.Equal("choose_expiry_date", state.Validity);
            Assert.Equal("2026-12-31", state.ExpiryDate);
        }

        [Fact]
        public void Parse_OmitsValidity_DoesNotDefault()
        {
            var state = AssistantOfferPathTerms.Parse("Create a 25% Offer", Utc2026);

            Assert.Null(state.Validity);
            Assert.Contains("validity", AssistantOfferPathTerms.MissingFields(state));
        }

        [Fact]
        public void Parse_ConflictingBenefits_DoesNotPickOne()
        {
            var state = AssistantOfferPathTerms.Parse(
                "Create a 25% Offer and a free dessert",
                Utc2026
            );

            Assert.False(AssistantOfferPathTerms.IsComplete(state));
            Assert.Null(state.OfferType);
            Assert.True(state.ConflictingBenefits.Count >= 2);
            Assert.Contains(
                "one authorised benefit",
                AssistantOfferPathTerms.OpenRuleNames(state),
                StringComparer.OrdinalIgnoreCase
            );
        }

        [Fact]
        public void Merge_ConflictingThenNamesOneBenefit_IsComplete()
        {
            var prior = AssistantOfferPathTerms.Parse(
                "Create a 25% Offer and a free dessert valid 30 days after issue",
                Utc2026
            );

            var merged = AssistantOfferPathTerms.Merge(prior, "25%", Utc2026);

            Assert.True(AssistantOfferPathTerms.IsComplete(merged));
            Assert.Equal("percentage_discount", merged.OfferType);
            Assert.Equal(25m, merged.DiscountPercentage);
            Assert.Null(merged.FreeItemText);
            Assert.Equal("30_days_after_issue", merged.Validity);
            Assert.Empty(merged.ConflictingBenefits);
        }

        [Theory]
        [InlineData("replacement item offer and attach it", null)]
        [InlineData("create a replacement item", null)]
        [InlineData("replacement item: burger", "burger")]
        [InlineData("replacement item is burger", "burger")]
        [InlineData("replace the burger", "burger")]
        [InlineData("replace the burger and attach it", "burger")]
        [InlineData(
            "replace the burger then attach it to the thank-you page",
            "burger"
        )]
        [InlineData("replace fish and chips", "fish and chips")]
        [InlineData("replace the fish and chips and attach it", "fish and chips")]
        [InlineData("swap the burger and attach it", "burger")]
        [InlineData("replace with burger", "burger")]
        [InlineData("replace and attach it", null)]
        [InlineData("replace an item", null)]
        public void Parse_ReplacementItemTable_SetsTypeAndCleansItem(
            string message,
            string? expectedItem
        )
        {
            var state = AssistantOfferPathTerms.Parse(message, Utc2026);

            Assert.Equal("replacement_item", state.OfferType);
            Assert.Equal(expectedItem, state.ReplacementItemText);
            var missing = AssistantOfferPathTerms.MissingFields(state);
            if (expectedItem is null)
            {
                Assert.Contains("value", missing);
            }
            else
            {
                Assert.DoesNotContain("value", missing);
            }
            Assert.Contains("validity", missing);
            Assert.False(AssistantOfferPathTerms.IsComplete(state));
            AssistantOfferPathTerms.ProposeCopy(state);
            Assert.Null(state.Title);
            Assert.Null(state.Description);
        }

        [Fact]
        public void Parse_ReplacementDoesNotMatchInsideReplacementWord()
        {
            var state = AssistantOfferPathTerms.Parse(
                "create a replacement item offer and attach it",
                Utc2026
            );

            Assert.Equal("replacement_item", state.OfferType);
            Assert.Null(state.ReplacementItemText);
        }

        [Theory]
        [InlineData("Caesar salad", "Caesar salad", null, false)]
        [InlineData("30 days", null, "30_days_after_issue", false)]
        [InlineData("14 days", null, "14_days_after_issue", false)]
        [InlineData("activate it", null, null, true)]
        [InlineData("attach it to thank you", null, null, false)]
        [InlineData("then attach it", null, null, false)]
        public void Merge_ReplacementItemFill_RespectsExclusions(
            string followUp,
            string? expectedItem,
            string? expectedValidity,
            bool expectsActivate
        )
        {
            var prior = AssistantOfferPathTerms.Parse("create a replacement item", Utc2026);
            Assert.Equal("replacement_item", prior.OfferType);
            Assert.Null(prior.ReplacementItemText);

            var merged = AssistantOfferPathTerms.Merge(prior, followUp, Utc2026);

            Assert.Equal("replacement_item", merged.OfferType);
            Assert.Equal(expectedItem, merged.ReplacementItemText);
            Assert.Equal(expectedValidity, merged.Validity);
            Assert.Equal(expectsActivate, merged.WantsActivate);
        }

        [Fact]
        public void Parse_InstructionLikeReplacement_CleansThenMissingValue()
        {
            var state = AssistantOfferPathTerms.Parse(
                "offer and attach it to capture thank you page",
                Utc2026
            );

            // No replacement cue → not a replacement type from this phrase alone.
            Assert.Null(state.ReplacementItemText);
        }

        [Fact]
        public void Parse_BurgerAndAttachIt_CleansToBurgerWhenCued()
        {
            var state = AssistantOfferPathTerms.Parse(
                "replace burger and attach it",
                Utc2026
            );

            Assert.Equal("replacement_item", state.OfferType);
            Assert.Equal("burger", state.ReplacementItemText);
        }

        [Fact]
        public void Merge_AfterClearedJunk_NextFillStoresNameOnly()
        {
            var prior = AssistantOfferPathTerms.Parse(
                "replacement item offer and attach it",
                Utc2026
            );
            Assert.Null(prior.ReplacementItemText);

            var merged = AssistantOfferPathTerms.Merge(prior, "burger", Utc2026);

            Assert.Equal("burger", merged.ReplacementItemText);
            Assert.DoesNotContain("attach", merged.ReplacementItemText!);
        }

        [Theory]
        [InlineData("Item")]
        [InlineData("OFFER")]
        [InlineData("Replacement Item")]
        public void TryClean_Filler_IsCaseInsensitive(string filler)
        {
            Assert.Null(AssistantCapturedItemText.TryClean(filler));
        }

        [Fact]
        public void Parse_OpsLikeFreeItemText_ClearedBySharedCleanup()
        {
            var state = new AssistantOfferPathTermsState
            {
                OfferType = "free_item",
                FreeItemText = "offer and attach it to capture thank you page",
            };
            var cleaned = AssistantOfferPathTerms.Merge(
                state,
                "keep type only",
                Utc2026
            );

            // Message has no free-item extract; post-Apply cleanup clears junk.
            Assert.Null(cleaned.FreeItemText);
        }
 
        [Fact]
        public void Parse_ReplacementItemAttachCaptureThankYou_SetsPlacementItemNull()
        {
            var state = AssistantOfferPathTerms.Parse(
                "Create a replacement item offer and attach it to capture thank you page",
                Utc2026
            );

            Assert.Equal("replacement_item", state.OfferType);
            Assert.Null(state.ReplacementItemText);
            Assert.True(state.WantsAttach);
            Assert.Equal(
                AssistantOfferPathTermsState.PlacementGuestFormThankYou,
                state.Placement
            );
            Assert.False(AssistantOfferPathTerms.IsComplete(state));
            Assert.Contains("value", AssistantOfferPathTerms.MissingFields(state));
            AssistantOfferPathTerms.ProposeCopy(state);
            Assert.Null(state.Title);
            Assert.DoesNotContain(
                "attach",
                AssistantOfferPathTerms.ValueLabel(state),
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public void Parse_ReplaceBurgerThenAttachThankYouPage_SetsItemAndPlacement()
        {
            var state = AssistantOfferPathTerms.Parse(
                "replace the burger then attach it to the thank-you page",
                Utc2026
            );

            Assert.Equal("replacement_item", state.OfferType);
            Assert.Equal("burger", state.ReplacementItemText);
            Assert.True(state.WantsAttach);
            Assert.Equal(
                AssistantOfferPathTermsState.PlacementGuestFormThankYou,
                state.Placement
            );
        }

        [Fact]
        public void Parse_CatalogOnlyTwentyFivePercent_CompleteWithoutAttach()
        {
            var state = AssistantOfferPathTerms.Parse(
                "Create a 25% Offer valid 14 days",
                Utc2026
            );

            Assert.True(AssistantOfferPathTerms.IsComplete(state));
            Assert.False(state.WantsAttach);
            Assert.Null(state.Placement);
            Assert.DoesNotContain(
                "placement",
                AssistantOfferPathTerms.MissingFields(state)
            );
        }

        [Fact]
        public void Parse_GenericAttach_WantsAttachMissingPlacement()
        {
            var state = AssistantOfferPathTerms.Parse(
                "Create a 25% Offer valid 14 days and attach it",
                Utc2026
            );

            Assert.True(state.WantsAttach);
            Assert.Null(state.Placement);
            Assert.False(AssistantOfferPathTerms.IsComplete(state));
            Assert.Equal(
                ["placement"],
                AssistantOfferPathTerms.MissingFields(state)
            );
        }

        [Fact]
        public void Parse_CampaignAttach_DoesNotSetWantsAttach()
        {
            var state = AssistantOfferPathTerms.Parse(
                "Create a 25% Offer valid 14 days and attach it to Summer win-back campaign",
                Utc2026
            );

            Assert.False(state.WantsAttach);
            Assert.Null(state.Placement);
            Assert.True(AssistantOfferPathTerms.IsComplete(state));
            AssistantOfferPathTerms.ProposeCopy(state);
            Assert.Equal("25% off", state.Title);
            Assert.DoesNotContain("attach", state.Title, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "campaign",
                state.Description ?? string.Empty,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public void Parse_CompleteWithGuestFormThankYou_ProposeCopyOmitsAttachLanguage()
        {
            var state = AssistantOfferPathTerms.Parse(
                "Create a 25% Offer valid 14 days and attach it to the thank-you page",
                Utc2026
            );

            Assert.True(AssistantOfferPathTerms.IsComplete(state));
            Assert.True(state.WantsAttach);
            Assert.Equal(
                AssistantOfferPathTermsState.PlacementGuestFormThankYou,
                state.Placement
            );
            AssistantOfferPathTerms.ProposeCopy(state);
            Assert.Equal("25% off", state.Title);
            Assert.Equal("Save 25%.", state.Description);
            Assert.DoesNotContain("attach", state.Title!, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("thank", state.Title!, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "offer and attach",
                state.Title!,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain(
                "attach",
                state.Description!,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain(
                "thank",
                state.Description!,
                StringComparison.OrdinalIgnoreCase
            );
            var value = AssistantOfferPathTerms.ValueLabel(state);
            Assert.Equal("25%", value);
            Assert.DoesNotContain("attach", value, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("thank", value, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Merge_FirstKnownWins_WantsAttachAndPlacement()
        {
            var prior = AssistantOfferPathTerms.Parse(
                "Create a 25% Offer valid 14 days and attach it",
                Utc2026
            );
            Assert.True(prior.WantsAttach);
            Assert.Null(prior.Placement);

            var withPlacement = AssistantOfferPathTerms.Merge(
                prior,
                "Guest form thank-you",
                Utc2026
            );
            Assert.True(withPlacement.WantsAttach);
            Assert.Equal(
                AssistantOfferPathTermsState.PlacementGuestFormThankYou,
                withPlacement.Placement
            );
            Assert.True(AssistantOfferPathTerms.IsComplete(withPlacement));

            var kept = AssistantOfferPathTerms.Merge(
                withPlacement,
                "attach it to capture thank you page",
                Utc2026
            );
            Assert.Equal(
                AssistantOfferPathTermsState.PlacementGuestFormThankYou,
                kept.Placement
            );
            Assert.True(kept.WantsAttach);
        }

        [Fact]
        public void OpenRules_PlacementOnly_UsesPlacementRuleNotCatalogFields()
        {
            var state = AssistantOfferPathTerms.Parse(
                "Create a 25% Offer valid 14 days and attach it",
                Utc2026
            );

            var openRules = AssistantOfferPathTerms.OpenRuleNames(state);
            Assert.Equal(["placement"], openRules);
        }

        [Fact]
        public void OpenRules_CatalogAndPlacement_ListsBoth()
        {
            var state = AssistantOfferPathTerms.Parse(
                "Create a replacement item offer and attach it",
                Utc2026
            );

            var openRules = AssistantOfferPathTerms.OpenRuleNames(state);
            Assert.Contains("value", openRules, StringComparer.Ordinal);
            Assert.Contains("placement", openRules, StringComparer.Ordinal);
        }

        [Fact]
        public void Serialize_RoundTrip_PreservesAttachFields()
        {
            var state = AssistantOfferPathTerms.Parse(
                "Create a 25% Offer valid 14 days and attach it to the thank you page",
                Utc2026
            );

            var restored = AssistantOfferPathTerms.FromJson(
                AssistantOfferPathTerms.Serialize(state)
            );

            Assert.NotNull(restored);
            Assert.True(restored!.WantsAttach);
            Assert.Equal(
                AssistantOfferPathTermsState.PlacementGuestFormThankYou,
                restored.Placement
            );
            Assert.Equal(state.OfferType, restored.OfferType);
            Assert.Equal(state.DiscountPercentage, restored.DiscountPercentage);
            Assert.Equal(state.Validity, restored.Validity);
        }

        [Fact]
        public void ToCreateRequest_OmitsAttachFields()
        {
            var state = AssistantOfferPathTerms.Parse(
                "Create a 25% Offer valid 14 days",
                Utc2026
            );
            state.WantsAttach = true;
            state.Placement = AssistantOfferPathTermsState.PlacementGuestFormThankYou;

            var request = AssistantOfferPathTerms.ToCreateRequest(state, locationId: 9);

            Assert.Equal(9, request.LocationId);
            Assert.Equal("percentage_discount", request.OfferType);
            Assert.Equal(25m, request.DiscountPercentage);
            var json = System.Text.Json.JsonSerializer.Serialize(request);
            Assert.DoesNotContain("WantsAttach", json, StringComparison.Ordinal);
            Assert.DoesNotContain("Placement", json, StringComparison.Ordinal);
            Assert.DoesNotContain(
                AssistantOfferPathTermsState.PlacementGuestFormThankYou,
                json,
                StringComparison.Ordinal
            );
        }
    }
}
