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
            Assert.Contains("### Recovery intent catalogue", locked.Body);
            Assert.Contains("- Respond to the guest", locked.Body);

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
        public void NaturalReplies_FillFeedbackIntentPurposeToneAndNotes()
        {
            var feedback = OneFeedback();
            var started = AssistantRecoveryDraftInterview.Apply(
                null,
                "Help me with a recovery",
                feedback,
                EmptyOffers()
            );
            var selected = AssistantRecoveryDraftInterview.Apply(
                started.State,
                "Use the latest one",
                feedback,
                EmptyOffers()
            );
            Assert.Equal(11, selected.State.FeedbackId);

            var intent = AssistantRecoveryDraftInterview.Apply(
                selected.State,
                "Reply to them",
                feedback,
                EmptyOffers()
            );
            Assert.Equal("respond-to-guest", intent.State.Intent);

            var response = AssistantRecoveryDraftInterview.Apply(
                intent.State,
                "Say sorry and keep it warm",
                feedback,
                EmptyOffers()
            );
            Assert.Equal(
                "apologise_and_confirm_follow_up",
                response.State.Purpose
            );
            Assert.Equal("warm_and_apologetic", response.State.Tone);

            var completed = AssistantRecoveryDraftInterview.Apply(
                response.State,
                "Nothing else to add",
                feedback,
                EmptyOffers()
            );
            Assert.True(completed.IsReady);
            Assert.Equal("", completed.State.IncludeNotes);
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
