using System.Text.Json.Nodes;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AzureOpenAIOneWaveToolLoopTests
    {
        [Fact]
        public void BuildRoundRequestJson_FinalRound_OmitsToolChoiceWithoutTools()
        {
            var messages = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = "Write the recommendation.",
                },
            };
            var schema = new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["properties"] = new JsonObject(),
                ["required"] = new JsonArray(),
            };

            var json = AzureOpenAIOneWaveToolLoop.BuildRoundRequestJson(
                "gpt-5-mini",
                messages,
                allowTools: false,
                toolsArray: new JsonArray(),
                schemaName: "home_recommendation",
                finalSchema: schema
            );

            Assert.DoesNotContain("\"tool_choice\"", json);
            Assert.DoesNotContain("\"parallel_tool_calls\"", json);
            Assert.DoesNotContain("\"tools\"", json);
            Assert.Contains("\"response_format\"", json);
            Assert.Contains("\"home_recommendation\"", json);
        }

        [Fact]
        public void BuildRoundRequestJson_ToolRound_EnablesParallelTools()
        {
            var messages = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = "Call metrics.",
                },
            };
            var tools = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = "read_home_recommendation_metrics",
                        ["parameters"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject(),
                        },
                    },
                },
            };

            var json = AzureOpenAIOneWaveToolLoop.BuildRoundRequestJson(
                "gpt-5-mini",
                messages,
                allowTools: true,
                toolsArray: tools,
                schemaName: "home_recommendation",
                finalSchema: new JsonObject { ["type"] = "object" }
            );

            Assert.Contains("\"tool_choice\":\"auto\"", json);
            Assert.Contains("\"parallel_tool_calls\":true", json);
            Assert.Contains("read_home_recommendation_metrics", json);
            Assert.DoesNotContain("\"response_format\"", json);
        }
    }
}
