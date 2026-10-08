using Microsoft.EntityFrameworkCore;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Services
{
    public partial class AssistantConversationServiceTests
    {
        [Fact]
        public async Task SendTurn_FeedbackExplanation_KeepsModelBody()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                FeedbackSentiment.Negative,
                "[\"WaitTime\"]",
                FeedbackWorkflowStatus.New
            );
            const string modelBody =
                "The one Feedback item this week is negative and mentions wait time. Follow up with that guest before you draft a recovery.";
            _fake.ExecuteToolsBeforeForcedResult = true;
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "Wait time",
                modelBody,
                AssistantTask.Retrieve
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "How is Feedback looking this week?")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Equal("Wait time", answer.Title);
            Assert.Equal(modelBody, answer.Body);
        }

        [Fact]
        public async Task SendTurn_ProductHelp_KeepsModelBody()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            const string modelBody =
                "I can explain Feedback, offers, and Campaigns, then draft the next one when you name the location.";
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "How I can help",
                modelBody,
                AssistantTask.Retrieve,
                "Help with the assistant"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "What can you do")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal(modelBody, answer.Body);
            Assert.Equal("How I can help", answer.Title);
            Assert.DoesNotContain(
                AssistantProductExpertCopy.CapabilitiesBody,
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Equal(0, await _context.Campaigns.CountAsync());
        }
    }
}
