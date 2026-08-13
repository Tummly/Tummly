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
            var empty = new AssistantFeedbackEvidence(0, 0, 0, 0, 0, 0, [], []);
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

        private static AssistantFeedbackEvidence NonEmptyEvidence()
            => new(
                2,
                2,
                0,
                0,
                2,
                1,
                [new AssistantFeedbackTagCount("WaitTime", 2)],
                []
            );
    }
}
