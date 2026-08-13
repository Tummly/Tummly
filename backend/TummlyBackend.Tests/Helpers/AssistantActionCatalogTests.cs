using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantActionCatalogTests
    {
        [Fact]
        public void Validate_DropsUnknownTypes_CapsAtThree_AndUsesCatalogOrder()
        {
            var evidence = NonEmptyEvidence();
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
            var empty = AssistantFeedbackEvidence.Empty;
            Assert.Empty(
                AssistantActionCatalog.Validate(
                    [new AssistantActionDto { Type = "view-feedback-set" }],
                    AssistantMessageClass.Grounded,
                    empty
                )
            );
            Assert.Empty(
                AssistantActionCatalog.Validate(
                    [new AssistantActionDto { Type = "view-feedback-set" }],
                    AssistantMessageClass.Refusal,
                    NonEmptyEvidence()
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
                NonEmptyEvidence()
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
                GuestEvidence(2),
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
                GuestEvidence(2),
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
                GuestEvidence(1),
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
                GuestEvidence(2, placeholder4: true),
                AssistantGroundedAsk.Placeholder4
            );

            Assert.Single(actions);
            Assert.Equal("view-guests", actions[0].Type);
            Assert.True(actions[0].MarketingEligible);
            Assert.Null(actions[0].SmartGroup);
        }

        private static AssistantFeedbackEvidence NonEmptyEvidence()
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
