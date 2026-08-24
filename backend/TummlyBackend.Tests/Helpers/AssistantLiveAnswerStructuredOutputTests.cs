using System.Text.Json.Nodes;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantLiveAnswerStructuredOutputTests
    {
        [Fact]
        public void BuildSystemPrompt_RequiresGroundedMarkdownAllowList()
        {
            var prompt = AssistantLiveAnswerStructuredOutput.BuildSystemPrompt(
                "2026-08-16"
            );

            Assert.Contains(
                "Grounded body formatting uses this Markdown allow-list only",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Use headings for distinct sections and bold for short",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "clarify bodies must be plain text",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "The server owns Gap turns",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Do not put Markdown in title",
                prompt,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void BuildSystemPrompt_RequiresConversationTitleOmitList()
        {
            var prompt = AssistantLiveAnswerStructuredOutput.BuildSystemPrompt(
                "2026-08-16"
            );

            Assert.Contains(
                "Emit conversationTitle with assistantTask",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Omit Owned location, Reporting period",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Do not copy title",
                prompt,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void BuildSchema_RequiresConversationTitle()
        {
            var schema = AssistantLiveAnswerStructuredOutput.BuildSchema();
            var required = schema["required"]!.AsArray()
                .Select(node => node!.GetValue<string>())
                .ToArray();

            Assert.Contains("conversationTitle", required);
            Assert.Contains("assistantTask", required);
        }

        [Fact]
        public void BuildSystemPrompt_DoesNotRefuseLegalCreateCampaignDraftAsMutate()
        {
            var prompt = AssistantLiveAnswerStructuredOutput.BuildSystemPrompt(
                "2026-08-16"
            );

            Assert.Contains(
                "Legal Create Campaign Draft, Create Campaign with Offer, Offer path",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "are not Mutate refusals",
                prompt,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "Mutate asks (create, send, or change records) are a refusal",
                prompt,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void BuildSystemPrompt_EmptyEvidenceTellsOperatorToChangeScope()
        {
            var prompt = AssistantLiveAnswerStructuredOutput.BuildSystemPrompt(
                "2026-08-16"
            );

            Assert.Contains(
                "Change Scope to pick another Owned location or Reporting period",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "send a more specific ask",
                prompt,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void BuildSystemPrompt_TreatsHistoryAsContextOnly()
        {
            var prompt = AssistantLiveAnswerStructuredOutput.BuildSystemPrompt(
                "2026-08-16"
            );

            Assert.Contains(
                "Prior turns are chat history for reference only",
                prompt,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void BuildRequestJson_WithoutHistory_SendsSystemThenUserPayload()
        {
            var input = new AssistantLiveAnswerInput(
                "Summarise feedback",
                "Camden",
                "the last 7 days",
                AssistantRetrievedEvidence.Empty
            );

            var request = AssistantLiveAnswerStructuredOutput.BuildRequestJson(
                "gpt-4o-mini",
                input,
                "2026-08-16"
            );
            var messages = JsonNode.Parse(request)!["messages"]!.AsArray();

            Assert.Equal(2, messages.Count);
            Assert.Equal("system", messages[0]!["role"]!.GetValue<string>());
            Assert.Equal("user", messages[1]!["role"]!.GetValue<string>());
        }

        [Fact]
        public void BuildRequestJson_WithHistory_InsertsTurnsBetweenSystemAndUserPayload()
        {
            var history = new[]
            {
                new AssistantLiveAnswerHistoryTurn(
                    AssistantMessageRole.User,
                    "How did offers do?"
                ),
                new AssistantLiveAnswerHistoryTurn(
                    AssistantMessageRole.Assistant,
                    "Two offers ran."
                ),
            };
            var input = new AssistantLiveAnswerInput(
                "Make it 25% instead",
                "Camden",
                "the last 7 days",
                AssistantRetrievedEvidence.Empty,
                History: history
            );

            var request = AssistantLiveAnswerStructuredOutput.BuildRequestJson(
                "gpt-4o-mini",
                input,
                "2026-08-16"
            );
            var messages = JsonNode.Parse(request)!["messages"]!.AsArray();

            Assert.Equal(4, messages.Count);
            Assert.Equal(
                "system",
                messages[0]!["role"]!.GetValue<string>()
            );
            Assert.Equal("user", messages[1]!["role"]!.GetValue<string>());
            Assert.Equal(
                "How did offers do?",
                messages[1]!["content"]!.GetValue<string>()
            );
            Assert.Equal(
                "assistant",
                messages[2]!["role"]!.GetValue<string>()
            );
            Assert.Equal(
                "Two offers ran.",
                messages[2]!["content"]!.GetValue<string>()
            );
            Assert.Equal("user", messages[3]!["role"]!.GetValue<string>());
            var payload = JsonNode.Parse(messages[3]!["content"]!.GetValue<string>())!;
            Assert.Equal(
                "Make it 25% instead",
                payload["userMessage"]!.GetValue<string>()
            );
        }
    }
}
