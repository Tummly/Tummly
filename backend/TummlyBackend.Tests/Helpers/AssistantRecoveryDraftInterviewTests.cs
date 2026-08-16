using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantRecoveryDraftInterviewTests
    {
        [Fact]
        public void Apply_AsksForFeedbackThenIntent_ThenCompletesRespondToGuest()
        {
            var feedback = OneFeedback();
            var started = AssistantRecoveryDraftInterview.Apply(
                null,
                "Draft a recovery response",
                feedback,
                EmptyOffers()
            );
            Assert.False(started.IsReady);
            Assert.Contains("Which Feedback", started.Body);

            var locked = AssistantRecoveryDraftInterview.Apply(
                started.State,
                "Pat Guest",
                feedback,
                EmptyOffers()
            );
            Assert.Equal(11, locked.State.FeedbackId);
            Assert.False(locked.IsReady);
            Assert.Contains("intent", locked.Body, StringComparison.OrdinalIgnoreCase);

            var typed = AssistantRecoveryDraftInterview.Apply(
                locked.State,
                "Respond to the guest; Acknowledge the feedback; Warm and apologetic; no notes",
                feedback,
                EmptyOffers()
            );
            Assert.True(typed.IsReady);
            Assert.Equal("respond-to-guest", typed.State.Intent);
            Assert.Equal("email", typed.State.Channel);
            Assert.Equal("acknowledge_feedback", typed.State.Purpose);
            Assert.Equal("warm_and_apologetic", typed.State.Tone);
            Assert.False(string.IsNullOrWhiteSpace(typed.State.Message));
            Assert.Contains("**Feedback:**", typed.Body);
        }

        [Fact]
        public void Apply_ResolvedFeedback_StaysIncomplete_WithoutDraftAction()
        {
            var feedback = OneFeedback(workflow: "Resolved");
            var turn = AssistantRecoveryDraftInterview.Apply(
                new AssistantRecoveryDraftState
                {
                    FeedbackId = 11,
                    FeedbackLabel = "Pat Guest (16 Aug 2026)",
                    Intent = "respond-to-guest",
                    IntentLabel = "Respond to the guest",
                },
                "Acknowledge the feedback; Warm and apologetic; Draft it now",
                feedback,
                EmptyOffers()
            );

            Assert.False(turn.IsReady);
            Assert.Contains("Resolved", turn.Body);
            Assert.False(AssistantRecoveryDraftInterview.IsReady(turn.State));
        }

        [Fact]
        public void Parse_RejectsCampaignTarget()
        {
            var campaignJson = AssistantCampaignDraftInterview.Serialize(
                new AssistantCampaignDraftState { Name = "Quiet Lunch" }
            );
            Assert.Null(AssistantRecoveryDraftInterview.Parse(campaignJson));

            var recovery = new AssistantRecoveryDraftState
            {
                FeedbackId = 3,
                Intent = "record-internal-action-only",
                Category = "team_briefed",
                Note = "Briefed the floor team",
            };
            var parsed = AssistantRecoveryDraftInterview.Parse(
                AssistantRecoveryDraftInterview.Serialize(recovery)
            );
            Assert.NotNull(parsed);
            Assert.Equal("recovery", parsed!.Target);
        }

        private static AssistantFeedbackEvidence OneFeedback(string workflow = "New")
            => new(
                1,
                1,
                0,
                0,
                1,
                1,
                [],
                [
                    new AssistantFeedbackEvidenceRow(
                        11,
                        new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc),
                        "Pat Guest",
                        "negative",
                        "Succeeded",
                        ["WaitTime"],
                        workflow,
                        true,
                        "Delivery",
                        "Email",
                        "Slow service",
                        "FB-11",
                        null,
                        [],
                        null,
                        false
                    ),
                ],
                [],
                [],
                []
            );

        private static AssistantOffersEvidence EmptyOffers()
            => new(0, 0, 0, 0, 0, 0, null, [], [], [], [], []);
    }
}
