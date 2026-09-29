using System.Text.Json.Nodes;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantLiveAnswerRetrieveToolsTests
    {
        [Fact]
        public void TryExtractToolCalls_ParsesNativeAzureShape()
        {
            var responseJson = """
                {
                  "choices": [
                    {
                      "message": {
                        "role": "assistant",
                        "tool_calls": [
                          {
                            "id": "call_1",
                            "type": "function",
                            "function": {
                              "name": "read_feedback_summary",
                              "arguments": "{}"
                            }
                          },
                          {
                            "id": "call_2",
                            "type": "function",
                            "function": {
                              "name": "read_offers",
                              "arguments": "{\"unused\":true}"
                            }
                          }
                        ]
                      }
                    }
                  ]
                }
                """;

            Assert.True(
                AssistantLiveAnswerStructuredOutput.TryExtractToolCalls(
                    responseJson,
                    out var calls
                )
            );
            Assert.Equal(2, calls.Count);
            Assert.Equal("call_1", calls[0].Id);
            Assert.Equal(
                AssistantRetrieveToolCatalog.ReadFeedbackSummary,
                calls[0].Name
            );
            Assert.Equal("call_2", calls[1].Id);
        }

        [Fact]
        public void TryExtractToolCalls_WithoutToolCalls_ReturnsFalse()
        {
            var responseJson = """
                {
                  "choices": [
                    {
                      "message": {
                        "role": "assistant",
                        "content": "{\"answerClass\":\"clarify\",\"body\":\"What do you need?\",\"title\":null,\"actions\":[],\"assistantTask\":\"retrieve\",\"conversationTitle\":null,\"offerTerms\":null}"
                      }
                    }
                  ]
                }
                """;

            Assert.False(
                AssistantLiveAnswerStructuredOutput.TryExtractToolCalls(
                    responseJson,
                    out _
                )
            );
        }

        [Fact]
        public void BuildRetrieveToolsRoundJson_FinalRound_DisablesTools()
        {
            var messages = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = "x" },
            };
            var input = new AssistantLiveAnswerInput(
                "Summarise feedback",
                "Camden",
                "this week",
                AssistantRetrievedEvidence.Empty
            );
            var json = AssistantLiveAnswerStructuredOutput.BuildRetrieveToolsRoundJson(
                "gpt-4o-mini",
                input,
                "2026-09-28",
                messages,
                allowTools: false
            );

            Assert.DoesNotContain("\"tool_choice\"", json);
            Assert.DoesNotContain("\"parallel_tool_calls\"", json);
            Assert.Contains("\"response_format\"", json);
            Assert.DoesNotContain("\"tools\"", json);
            Assert.Contains("\"max_completion_tokens\":4096", json);
        }

        [Fact]
        public void BuildRetrieveToolsRoundJson_ToolRound_EnablesParallelTools()
        {
            var messages = AssistantLiveAnswerStructuredOutput
                .BuildRetrieveToolsSeedMessages(
                    new AssistantLiveAnswerInput(
                        "Summarise feedback",
                        "Camden",
                        "this week",
                        AssistantRetrievedEvidence.Empty
                    ),
                    "2026-09-28"
                );
            var json = AssistantLiveAnswerStructuredOutput.BuildRetrieveToolsRoundJson(
                "gpt-4o-mini",
                new AssistantLiveAnswerInput(
                    "Summarise feedback",
                    "Camden",
                    "this week",
                    AssistantRetrievedEvidence.Empty
                ),
                "2026-09-28",
                messages,
                allowTools: true
            );

            Assert.Contains("\"parallel_tool_calls\":true", json);
            Assert.Contains("\"tool_choice\":\"auto\"", json);
            Assert.Contains(AssistantRetrieveToolCatalog.ReadFeedbackSummary, json);
            Assert.DoesNotContain("\"response_format\"", json);
        }
    }
}
