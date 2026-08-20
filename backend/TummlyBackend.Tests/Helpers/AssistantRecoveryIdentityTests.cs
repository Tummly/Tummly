using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantRecoveryIdentityTests
    {
        private const string LastNegativeOnThisLocation =
            "Create a recovery offer for the last negative feedback we recieved on this location";

        [Fact]
        public void LastNegativeAsk_DoesNotTreatThePhraseAsAGuestName()
        {
            var older = Row(11, "Pat Guest", "negative", hoursAgo: 5);
            var newer = Row(12, "Alex Guest", "negative", hoursAgo: 1);

            var match = AssistantRecoveryIdentity.Resolve(
                LastNegativeOnThisLocation,
                [older, newer]
            );

            var one = Assert.IsType<AssistantRecoveryIdentity.Match.One>(match);
            Assert.Equal(12, one.Row.Id);
        }

        [Fact]
        public void LastNegativeAsk_BindsResolvedNegative()
        {
            var match = AssistantRecoveryIdentity.Resolve(
                LastNegativeOnThisLocation,
                [Row(11, "Pat Guest", "negative", hoursAgo: 2, workflow: "Resolved")]
            );

            var one = Assert.IsType<AssistantRecoveryIdentity.Match.One>(match);
            Assert.Equal(11, one.Row.Id);
        }

        [Fact]
        public void UnnamedAsk_WithTwoRows_IsStillAGap()
        {
            var match = AssistantRecoveryIdentity.Resolve(
                "Prepare a recovery response",
                [
                    Row(11, "Pat Guest", "negative", hoursAgo: 1),
                    Row(12, "Alex Guest", "negative", hoursAgo: 2),
                ]
            );

            Assert.IsType<AssistantRecoveryIdentity.Match.Many>(match);
        }

        [Fact]
        public void LastNegativeAsk_WithOnlyPositiveFeedback_IsNoNegative()
        {
            var match = AssistantRecoveryIdentity.Resolve(
                LastNegativeOnThisLocation,
                [Row(11, "Pat Guest", "positive", hoursAgo: 1)]
            );

            var none = Assert.IsType<AssistantRecoveryIdentity.Match.None>(match);
            Assert.Equal(AssistantRecoveryIdentity.ReasonNoNegative, none.Reason);
        }

        [Fact]
        public void NamedGuestMiss_IsStillNone()
        {
            var match = AssistantRecoveryIdentity.Resolve(
                "Prepare a recovery response for Mehmet",
                [Row(11, "Pat Guest", "negative", hoursAgo: 1)]
            );

            var none = Assert.IsType<AssistantRecoveryIdentity.Match.None>(match);
            Assert.Equal(AssistantRecoveryIdentity.ReasonNamedMiss, none.Reason);
        }

        private static AssistantFeedbackEvidenceRow Row(
            int id,
            string guestName,
            string? sentiment,
            int hoursAgo,
            string workflow = "New"
        )
            => new(
                id,
                DateTime.UtcNow.AddHours(-hoursAgo),
                guestName,
                sentiment,
                "Succeeded",
                [],
                workflow,
                sentiment == "negative" && workflow != "Resolved",
                null,
                "Email",
                "Slow service",
                $"FDB-{id.ToString().PadLeft(6, '0')}",
                null,
                [],
                null,
                false
            );
    }
}
