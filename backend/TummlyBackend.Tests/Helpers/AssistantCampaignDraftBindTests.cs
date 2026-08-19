using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantCampaignDraftBindTests
    {
        private const string CanonicalAsk =
            "Draft an Email Campaign to bring back all currently Email-eligible guests at Camden";

        private static readonly AssistantCampaignTemplateRef ThankTemplate =
            new("thank-recent-guests", "Thank recent guests");

        private static readonly AssistantCampaignTemplateRef QuietTemplate =
            new("quiet-time-boost", "Quiet-time boost");

        private static AssistantCatalogOfferRef Active(
            int id,
            string title,
            decimal? percent = 10m,
            decimal? amount = null,
            string? freeItem = null
        )
            => new(id, title, "active", true, percent, amount, freeItem);

        private static AssistantCatalogOfferRef NotAttachable(
            int id,
            string title,
            string status
        )
            => new(id, title, status, false, 10m, null, null);

        [Fact]
        public void CanonicalCamdenEmailWinBack_BindsAllEligibleEmailNoOffer()
        {
            var outcome = AssistantCampaignDraftBind.Resolve(
                CanonicalAsk,
                "Camden",
                [],
                []
            );

            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(outcome);
            Assert.Equal("email", bound.Fields.Channel);
            Assert.Equal("Email", bound.Fields.ChannelLabel);
            Assert.Equal("all-eligible-guests", bound.Fields.AudienceKey);
            Assert.Equal("All eligible guests", bound.Fields.AudienceLabel);
            Assert.Equal("re-engage-inactive", bound.Fields.GoalId);
            Assert.Equal("no-offer", bound.Fields.OfferStance);
            Assert.Null(bound.Fields.OfferId);
            Assert.Equal("No Offer", bound.Fields.OfferLabel);
            Assert.Null(bound.Fields.OfferNote);
            Assert.Null(bound.Fields.TemplateId);
            Assert.Equal(
                "Bring back Email-eligible guests at Camden",
                bound.Fields.Name
            );
        }

        [Fact]
        public void UnnamedChannel_DefaultsToEmail()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    "Draft a Campaign to bring back all currently eligible guests at Camden",
                    "Camden",
                    [],
                    []
                )
            );

            Assert.Equal("email", bound.Fields.Channel);
        }

        [Fact]
        public void NamedSms_BindsSmsChannel()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    "Draft an SMS Campaign to bring back eligible guests at Camden",
                    "Camden",
                    [],
                    []
                )
            );

            Assert.Equal("sms", bound.Fields.Channel);
            Assert.Equal("SMS", bound.Fields.ChannelLabel);
            Assert.Equal(
                "Bring back SMS-eligible guests at Camden",
                bound.Fields.Name
            );
        }

        [Fact]
        public void EmailAndSms_IsChannelGapTurn()
        {
            var gap = Assert.IsType<AssistantCampaignDraftBindOutcome.Gap>(
                AssistantCampaignDraftBind.Resolve(
                    "Draft an Email and SMS Campaign to bring back eligible guests at Camden",
                    "Camden",
                    [],
                    []
                )
            );

            Assert.Equal(AssistantGapTurn.KindChannel, gap.Kind);
            Assert.Equal(["Email", "SMS"], gap.Options);
            Assert.Contains("Email", gap.Body, StringComparison.Ordinal);
            Assert.Contains("SMS", gap.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("catalogue", gap.Body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void NewGuestsBeatsAllEligible()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    "Draft an Email Campaign to all eligible new guests at Camden",
                    "Camden",
                    [],
                    []
                )
            );

            Assert.Equal("new-guests", bound.Fields.AudienceKey);
        }

        [Fact]
        public void BringBackInactive_StaysAllEligible_NotDormantOrUnevaluable()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    "Draft an Email Campaign to bring back inactive guests at Camden",
                    "Camden",
                    [],
                    []
                )
            );

            Assert.Equal("all-eligible-guests", bound.Fields.AudienceKey);
            Assert.Equal("re-engage-inactive", bound.Fields.GoalId);
        }

        [Fact]
        public void DormantLapsed90Days_BindsDormantGuests()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    "Draft an Email Campaign to dormant lapsed guests with no visit in 90 days at Camden",
                    "Camden",
                    [],
                    []
                )
            );

            Assert.Equal("dormant-guests", bound.Fields.AudienceKey);
        }

        [Fact]
        public void TwoNamedEvaluableAudiences_IsAudienceGapTurn()
        {
            var gap = Assert.IsType<AssistantCampaignDraftBindOutcome.Gap>(
                AssistantCampaignDraftBind.Resolve(
                    "Draft an Email Campaign to new guests and dormant guests at Camden",
                    "Camden",
                    [],
                    []
                )
            );

            Assert.Equal(AssistantGapTurn.KindAudience, gap.Kind);
            Assert.Equal(["New guests", "Dormant guests"], gap.Options);
            Assert.DoesNotContain("all-eligible-guests", gap.Body, StringComparison.Ordinal);
        }

        [Fact]
        public void NamedUnevaluableAudience_PersistsNothing()
        {
            var unevaluable = Assert.IsType<AssistantCampaignDraftBindOutcome.UnevaluableAudience>(
                AssistantCampaignDraftBind.Resolve(
                    "Draft an Email Campaign to guests with no recent Tummly activity at Camden",
                    "Camden",
                    [],
                    []
                )
            );

            Assert.Contains(
                "cannot be evaluated yet",
                unevaluable.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain("catalogue", unevaluable.Body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SavedGroup_IsUnevaluable()
        {
            Assert.IsType<AssistantCampaignDraftBindOutcome.UnevaluableAudience>(
                AssistantCampaignDraftBind.Resolve(
                    "Draft an Email Campaign to my saved group at Camden",
                    "Camden",
                    [],
                    []
                )
            );
        }

        [Fact]
        public void OfferNotRedeemed_IsUnevaluable_NotSwappedToAllEligible()
        {
            Assert.IsType<AssistantCampaignDraftBindOutcome.UnevaluableAudience>(
                AssistantCampaignDraftBind.Resolve(
                    "Draft an Email Campaign to all eligible guests who have not redeemed at Camden",
                    "Camden",
                    [],
                    []
                )
            );
        }

        [Fact]
        public void UseAnOfferUnnamed_StaysNoOffer_EvenWhenActiveOffersExist()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    CanonicalAsk + " and use an offer",
                    "Camden",
                    [Active(4, "Weekend brunch")],
                    []
                )
            );

            Assert.Equal("no-offer", bound.Fields.OfferStance);
            Assert.Null(bound.Fields.OfferId);
            Assert.Equal("No Offer", bound.Fields.OfferLabel);
        }

        [Fact]
        public void UniqueNamedActiveOffer_Attaches()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    CanonicalAsk + " with Weekend brunch",
                    "Camden",
                    [Active(4, "Weekend brunch"), Active(5, "Lunch treat")],
                    []
                )
            );

            Assert.Equal("existing-offer", bound.Fields.OfferStance);
            Assert.Equal(4, bound.Fields.OfferId);
            Assert.Equal("Weekend brunch", bound.Fields.OfferLabel);
        }

        [Fact]
        public void UniqueNamedPercent_AttachesThatOffer()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    CanonicalAsk + " with the 15% offer",
                    "Camden",
                    [Active(4, "Weekend brunch", 10m), Active(5, "Lunch treat", 15m)],
                    []
                )
            );

            Assert.Equal(5, bound.Fields.OfferId);
        }

        [Fact]
        public void UniqueExactTitle_AttachesWhenLongerTitleAlsoExists()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    CanonicalAsk + " with Weekend brunch",
                    "Camden",
                    [Active(4, "Weekend brunch"), Active(5, "Weekend brunch special")],
                    []
                )
            );

            Assert.Equal(4, bound.Fields.OfferId);
            Assert.Equal("Weekend brunch", bound.Fields.OfferLabel);
        }

        [Fact]
        public void TwoMatchingOfferTitles_IsOfferGapTurn_BeforeAudienceGapTurn()
        {
            var gap = Assert.IsType<AssistantCampaignDraftBindOutcome.Gap>(
                AssistantCampaignDraftBind.Resolve(
                    "Draft an Email Campaign to new guests and dormant guests with Weekend brunch and Lunch treat at Camden",
                    "Camden",
                    [Active(4, "Weekend brunch"), Active(5, "Lunch treat")],
                    []
                )
            );

            Assert.Equal(AssistantGapTurn.KindOffer, gap.Kind);
            Assert.Equal(["Weekend brunch", "Lunch treat"], gap.Options);
        }

        [Fact]
        public void DraftOfferAtLocation_PersistsNoOfferAndExplains()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    CanonicalAsk + " with Weekend brunch",
                    "Camden",
                    [NotAttachable(9, "Weekend brunch", "draft")],
                    []
                )
            );

            Assert.Equal("no-offer", bound.Fields.OfferStance);
            Assert.Null(bound.Fields.OfferId);
            Assert.Contains("Weekend brunch", bound.Fields.OfferNote, StringComparison.Ordinal);
            Assert.Contains("not attachable", bound.Fields.OfferNote, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void UniqueNamedTemplate_SetsTemplateId_CopyStillGeneratedElsewhere()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    CanonicalAsk + " using Thank recent guests",
                    "Camden",
                    [],
                    [ThankTemplate, QuietTemplate]
                )
            );

            Assert.Equal("thank-recent-guests", bound.Fields.TemplateId);
        }

        [Fact]
        public void UnnamedTemplate_StaysNull()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    CanonicalAsk,
                    "Camden",
                    [],
                    [ThankTemplate, QuietTemplate]
                )
            );

            Assert.Null(bound.Fields.TemplateId);
        }

        [Fact]
        public void ThankGoal_InfersThankRecentGuests()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    "Draft an Email Campaign to thank new guests at Camden",
                    "Camden",
                    [],
                    []
                )
            );

            Assert.Equal("thank-recent-guests", bound.Fields.GoalId);
            Assert.Equal("new-guests", bound.Fields.AudienceKey);
            Assert.Contains("Email", bound.Fields.Name, StringComparison.Ordinal);
        }

        [Fact]
        public void ChosenChannel_ResolvesChannelGapTurn()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    "Draft an Email and SMS Campaign to bring back eligible guests at Camden",
                    "Camden",
                    [],
                    [],
                    new AssistantCampaignDraftBindChoice(ChannelLabel: "SMS")
                )
            );

            Assert.Equal("sms", bound.Fields.Channel);
        }

        [Fact]
        public void UnhappyGuests_DoesNotBindPositiveAudience()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    "Draft a Campaign for unhappy guests at Camden",
                    "Camden",
                    [],
                    []
                )
            );

            Assert.Equal("all-eligible-guests", bound.Fields.AudienceKey);
            Assert.NotEqual("positive-feedback", bound.Fields.AudienceKey);
        }

        [Fact]
        public void NewGuestsAsk_InfersPromoteGoal()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    "Draft a Campaign for new guests at Camden",
                    "Camden",
                    [],
                    []
                )
            );

            Assert.Equal("new-guests", bound.Fields.AudienceKey);
            Assert.Equal("promote-something-new", bound.Fields.GoalId);
        }

        [Fact]
        public void PositiveWord_BindsPositiveFeedback()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    "Draft a Campaign for positive diners at Camden",
                    "Camden",
                    [],
                    []
                )
            );

            Assert.Equal("positive-feedback", bound.Fields.AudienceKey);
        }

        [Fact]
        public void UniqueNamedAmount_AttachesThatOffer()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    CanonicalAsk + " with the £5 offer",
                    "Camden",
                    [
                        Active(4, "Weekend brunch", percent: 10m),
                        Active(5, "Lunch treat", percent: null, amount: 5m)
                    ],
                    []
                )
            );

            Assert.Equal(5, bound.Fields.OfferId);
        }

        [Fact]
        public void UniqueNamedFreeItem_AttachesThatOffer()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    CanonicalAsk + " with free dessert",
                    "Camden",
                    [
                        Active(4, "Weekend brunch"),
                        Active(5, "Sweet treat", percent: null, freeItem: "free dessert")
                    ],
                    []
                )
            );

            Assert.Equal(5, bound.Fields.OfferId);
        }

        [Fact]
        public void NamedMissingOffer_StaysNoOfferAndExplains()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    CanonicalAsk + " with the offer called Super Saver",
                    "Camden",
                    [Active(4, "Weekend brunch")],
                    []
                )
            );

            Assert.Equal("no-offer", bound.Fields.OfferStance);
            Assert.Contains(
                "No matching Active Offer",
                bound.Fields.OfferNote,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void WithTheOfferUnnamed_StaysSilentNoOffer()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    CanonicalAsk + " with the offer",
                    "Camden",
                    [Active(4, "Weekend brunch")],
                    []
                )
            );

            Assert.Equal("no-offer", bound.Fields.OfferStance);
            Assert.Null(bound.Fields.OfferNote);
        }

        [Fact]
        public void PrefixOfLongerTitle_DoesNotAttach()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    CanonicalAsk + " with Weekend brunch",
                    "Camden",
                    [Active(5, "Weekend brunch special")],
                    []
                )
            );

            Assert.Equal("no-offer", bound.Fields.OfferStance);
            Assert.Null(bound.Fields.OfferId);
        }

        [Fact]
        public void NamedOfferAtOtherOwnedLocation_StaysNoOfferAndExplains()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    CanonicalAsk + " with Weekend brunch",
                    "Camden",
                    [],
                    [],
                    otherLocationOffers: [Active(4, "Weekend brunch")]
                )
            );

            Assert.Equal("no-offer", bound.Fields.OfferStance);
            Assert.Contains("Weekend brunch", bound.Fields.OfferNote, StringComparison.Ordinal);
            Assert.Contains("not attachable", bound.Fields.OfferNote, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CustomCampaign_NameUsesChannelAndLocation()
        {
            var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
                AssistantCampaignDraftBind.Resolve(
                    "Draft a Campaign for eligible guests at Camden",
                    "Camden",
                    [],
                    []
                )
            );

            Assert.Equal("custom-campaign", bound.Fields.GoalId);
            Assert.Equal("Campaign by Email at Camden", bound.Fields.Name);
            Assert.NotEqual(
                AssistantCampaignDraftName.GoalDefault,
                bound.Fields.Name
            );
        }

        [Fact]
        public void ResolveNamedChoice_UniqueOption()
        {
            Assert.Equal(
                "Weekend brunch",
                AssistantCampaignDraftBind.ResolveNamedChoice(
                    ["Weekend brunch", "Lunch treat"],
                    "Weekend brunch"
                )
            );
        }

        [Fact]
        public void ResolveNamedChoice_NonUnique_IsNull()
        {
            Assert.Null(
                AssistantCampaignDraftBind.ResolveNamedChoice(
                    ["Weekend brunch", "Lunch treat"],
                    "ok"
                )
            );
        }
    }
}
