using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantActionCatalogTests
    {
        [Fact]
        public void ValidateCampaignDraft_KeepsOnlyServerDraftAction_WithoutEvidence()
        {
            var actions = AssistantActionCatalog.ValidateCampaignDraft(
                [
                    new AssistantActionDto { Type = "view-campaigns" },
                    new AssistantActionDto { Type = "draft-campaign" },
                ],
                AssistantMessageClass.Grounded
            );

            var action = Assert.Single(actions);
            Assert.Equal("draft-campaign", action.Type);
            Assert.Equal("Create campaign draft", action.Label);
        }

        [Fact]
        public void ValidateOfferDraft_KeepsOnlyServerDraftAction_WithoutEvidence()
        {
            var actions = AssistantActionCatalog.ValidateOfferDraft(
                [
                    new AssistantActionDto { Type = "view-offers" },
                    new AssistantActionDto { Type = "draft-offer" },
                ],
                AssistantMessageClass.Grounded
            );

            var action = Assert.Single(actions);
            Assert.Equal("draft-offer", action.Type);
            Assert.Equal("Create offer draft", action.Label);
        }

        [Fact]
        public void Validate_DropsModelProposedDraftAction_OnRetrieveTurns()
        {
            var actions = AssistantActionCatalog.Validate(
                [
                    new AssistantActionDto { Type = "draft-campaign" },
                    new AssistantActionDto { Type = "draft-offer" },
                ],
                AssistantMessageClass.Grounded,
                WithFeedback(NonEmptyFeedback())
            );

            Assert.Empty(actions);
        }

        [Fact]
        public void Validate_DropsUnknownTypes_CapsAtThree_AndUsesCatalogOrder()
        {
            var evidence = WithFeedback(NonEmptyFeedback());
            var actions = AssistantActionCatalog.Validate(
                [
                    new AssistantActionDto { Type = "invented-home" },
                    new AssistantActionDto { Type = "view-offers" },
                    new AssistantActionDto { Type = "view-campaigns" },
                    new AssistantActionDto { Type = "view-feedback-set", Count = 6 },
                    new AssistantActionDto { Type = "prepare-recovery" },
                    new AssistantActionDto { Type = "view-capture" },
                ],
                AssistantMessageClass.Grounded,
                evidence
            );

            Assert.Equal(
                new[] { "view-feedback-set", "prepare-recovery", "view-campaigns" },
                actions.Select(action => action.Type)
            );
            Assert.Equal("View 2 feedback items", actions[0].Label);
        }

        [Fact]
        public void Validate_HidesActions_OnEmptyOrNonGrounded()
        {
            Assert.Empty(
                AssistantActionCatalog.Validate(
                    [new AssistantActionDto { Type = "view-feedback-set" }],
                    AssistantMessageClass.Grounded,
                    AssistantRetrievedEvidence.Empty
                )
            );
            Assert.Empty(
                AssistantActionCatalog.Validate(
                    [new AssistantActionDto { Type = "view-feedback-set" }],
                    AssistantMessageClass.Refusal,
                    WithFeedback(NonEmptyFeedback())
                )
            );
        }

        [Fact]
        public void Validate_DropsPrepareRecovery_WhenFeedbackSetIsNeedsAttention()
        {
            var actions = AssistantActionCatalog.Validate(
                [
                    new AssistantActionDto
                    {
                        Type = "view-feedback-set",
                        Tab = "needs-attention",
                    },
                    new AssistantActionDto { Type = "prepare-recovery" },
                ],
                AssistantMessageClass.Grounded,
                WithFeedback(NonEmptyFeedback())
            );

            Assert.Single(actions);
            Assert.Equal("view-feedback-set", actions[0].Type);
            Assert.Equal("needs-attention", actions[0].Tab);
        }

        [Fact]
        public void Validate_DropsGuestActions_OnSummariseAsk()
        {
            var actions = AssistantActionCatalog.Validate(
                [
                    new AssistantActionDto { Type = "view-guests" },
                    new AssistantActionDto { Type = "view-guest", GuestId = 9 },
                ],
                AssistantMessageClass.Grounded,
                WithListGuests(2),
                AssistantGroundedAsk.Summarise
            );

            Assert.DoesNotContain(actions, action => action.Type is "view-guests" or "view-guest");
        }

        [Fact]
        public void Validate_DropsViewGuest_WhenViewGuestsIsPresent()
        {
            var actions = AssistantActionCatalog.Validate(
                [
                    new AssistantActionDto { Type = "view-guests", MarketingEligible = true },
                    new AssistantActionDto { Type = "view-guest", GuestId = 4 },
                ],
                AssistantMessageClass.Grounded,
                WithListGuests(2),
                AssistantGroundedAsk.ListGuests
            );

            Assert.Single(actions);
            Assert.Equal("view-guests", actions[0].Type);
        }

        [Fact]
        public void Validate_KeepsViewGuest_ForExactlyOneLocationGuest()
        {
            var actions = AssistantActionCatalog.Validate(
                [new AssistantActionDto { Type = "view-guest" }],
                AssistantMessageClass.Grounded,
                WithListGuests(1),
                AssistantGroundedAsk.ListGuests
            );

            Assert.Single(actions);
            Assert.Equal("view-guest", actions[0].Type);
            Assert.Equal(101, actions[0].GuestId);
            Assert.Equal("View guest", actions[0].Label);
        }

        [Fact]
        public void Validate_Placeholder4_ForcesMarketingEligible_AndOmitsNeedsRecovery()
        {
            var actions = AssistantActionCatalog.Validate(
                [
                    new AssistantActionDto
                    {
                        Type = "view-guests",
                        SmartGroup = "needs-recovery",
                        MarketingEligible = false,
                    },
                ],
                AssistantMessageClass.Grounded,
                WithFeedback(GuestEvidence(2, placeholder4: true)),
                AssistantGroundedAsk.Placeholder4
            );

            Assert.Single(actions);
            Assert.Equal("view-guests", actions[0].Type);
            Assert.True(actions[0].MarketingEligible);
            Assert.Null(actions[0].SmartGroup);
        }

        [Fact]
        public void Validate_ViewOffer_RequiresExactlyOneNamedCatalogOffer()
        {
            var one = WithOffers(Catalog("Weekend brunch", 11));
            var kept = AssistantActionCatalog.Validate(
                [new AssistantActionDto { Type = "view-offer", OfferId = 11 }],
                AssistantMessageClass.Grounded,
                one
            );
            Assert.Equal("view-offer", Assert.Single(kept).Type);
            Assert.Equal(11, kept[0].OfferId);

            var missingId = AssistantActionCatalog.Validate(
                [new AssistantActionDto { Type = "view-offer" }],
                AssistantMessageClass.Grounded,
                one
            );
            Assert.Empty(missingId);

            var unknownId = AssistantActionCatalog.Validate(
                [new AssistantActionDto { Type = "view-offer", OfferId = 99 }],
                AssistantMessageClass.Grounded,
                one
            );
            Assert.Empty(unknownId);
        }

        [Fact]
        public void Validate_DropsViewOffer_WhenPairedWithViewOffers()
        {
            var evidence = WithOffers(Catalog("Weekend brunch", 11));
            var actions = AssistantActionCatalog.Validate(
                [
                    new AssistantActionDto { Type = "view-offers" },
                    new AssistantActionDto { Type = "view-offer", OfferId = 11 },
                ],
                AssistantMessageClass.Grounded,
                evidence
            );

            Assert.Equal("view-offers", Assert.Single(actions).Type);
        }

        [Fact]
        public void Validate_ViewCapture_RequiresCaptureFacts()
        {
            var without = WithFeedback(NonEmptyFeedback());
            Assert.Empty(
                AssistantActionCatalog.Validate(
                    [new AssistantActionDto { Type = "view-capture" }],
                    AssistantMessageClass.Grounded,
                    without
                )
            );

            var withCapture = AssistantRetrievedEvidence.Empty with
            {
                Capture = new AssistantCaptureEvidence(
                    4,
                    1,
                    2,
                    0,
                    1,
                    0,
                    [new AssistantCaptureQrRow(3, "SmartGuest", "Active", 4, 2, 1)]
                ),
            };
            var kept = AssistantActionCatalog.Validate(
                [new AssistantActionDto { Type = "view-capture" }],
                AssistantMessageClass.Grounded,
                withCapture
            );
            Assert.Equal("view-capture", Assert.Single(kept).Type);
        }

        [Fact]
        public void Validate_ViewOffersAndViewCampaigns_KeepNextStepOnOtherDomainFacts()
        {
            var feedbackOnly = WithFeedback(NonEmptyFeedback());
            var nextStep = AssistantActionCatalog.Validate(
                [
                    new AssistantActionDto { Type = "view-offers" },
                    new AssistantActionDto { Type = "view-campaigns" },
                ],
                AssistantMessageClass.Grounded,
                feedbackOnly
            );
            Assert.Equal(
                new[] { "view-campaigns", "view-offers" },
                nextStep.Select(action => action.Type)
            );

            var offers = WithOffers(Catalog("Weekend brunch", 11));
            var evidenceOffers = AssistantActionCatalog.Validate(
                [new AssistantActionDto { Type = "view-offers" }],
                AssistantMessageClass.Grounded,
                offers
            );
            Assert.Equal("view-offers", Assert.Single(evidenceOffers).Type);

            var campaigns = AssistantRetrievedEvidence.Empty with
            {
                Campaigns = new AssistantCampaignsEvidence(
                    1,
                    1,
                    1,
                    0,
                    0,
                    [new AssistantCampaignListRow(4, "Lunch push", "scheduled", DateTime.UtcNow, DateTime.UtcNow, null)],
                    [],
                    []
                ),
            };
            var evidenceCampaigns = AssistantActionCatalog.Validate(
                [new AssistantActionDto { Type = "view-campaigns" }],
                AssistantMessageClass.Grounded,
                campaigns
            );
            Assert.Equal("view-campaigns", Assert.Single(evidenceCampaigns).Type);
        }

        private static AssistantRetrievedEvidence WithListGuests(int guestCount)
        {
            var guests = Enumerable
                .Range(0, guestCount)
                .Select(index => new AssistantGuestEvidenceRow(
                    101 + index,
                    $"Guest {index + 1}",
                    "Eligible — Email",
                    [],
                    true
                ))
                .ToList();

            return AssistantRetrievedEvidence.Empty with
            {
                Feedback = NonEmptyFeedback(),
                Guests = new AssistantGuestsEvidence(
                    guestCount,
                    guestCount,
                    guests,
                    []
                ),
            };
        }

        private static AssistantRetrievedEvidence WithFeedback(
            AssistantFeedbackEvidence feedback
        )
            => AssistantRetrievedEvidence.Empty with { Feedback = feedback };

        private static AssistantRetrievedEvidence WithOffers(
            AssistantOffersEvidence offers
        )
            => AssistantRetrievedEvidence.Empty with { Offers = offers };

        private static AssistantOffersEvidence Catalog(string title, int id)
            => new(
                1,
                1,
                1,
                0,
                0,
                0,
                null,
                [new AssistantOfferCatalogRow(id, title, "active", DateTime.UtcNow)],
                [],
                [],
                [],
                []
            );

        private static AssistantFeedbackEvidence NonEmptyFeedback()
            => GuestEvidence(0);

        private static AssistantFeedbackEvidence GuestEvidence(
            int guestCount,
            bool placeholder4 = false
        )
        {
            var guests = Enumerable
                .Range(0, guestCount)
                .Select(index => new AssistantGuestEvidenceRow(
                    101 + index,
                    $"Guest {index + 1}",
                    "Eligible — Email",
                    [],
                    true
                ))
                .ToList();

            return new AssistantFeedbackEvidence(
                2,
                2,
                0,
                0,
                2,
                1,
                [new AssistantFeedbackTagCount("WaitTime", 2)],
                [],
                guests,
                placeholder4 ? guests : [],
                []
            );
        }
    }
}
