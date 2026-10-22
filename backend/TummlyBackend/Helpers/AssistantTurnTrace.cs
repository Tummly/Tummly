using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.Configurations;

namespace TummlyBackend.Helpers
{
    public static class AssistantTurnTrace
    {
        public const string InstructionVersion = "assistant-2026-10-09";

        public static string Serialize(
            FeedbackClassificationSettings settings,
            IReadOnlyList<string> toolNames,
            AssistantTurnTimings? timings = null
        )
        {
            var deployment = string.IsNullOrWhiteSpace(settings.AssistantDeploymentName)
                ? settings.DeploymentName
                : settings.AssistantDeploymentName.Trim();
            var trace = new JsonObject
            {
                ["modelDeployment"] = deployment,
                ["instructionVersion"] = InstructionVersion,
                ["knowledgeVersion"] = AssistantKnowledgeLayer.Version,
                ["schemaVersion"] = settings.PromptSchemaVersion,
                ["tools"] = new JsonArray(
                    toolNames
                        .Distinct(StringComparer.Ordinal)
                        .Select(name => (JsonNode?)name)
                        .ToArray()
                ),
            };
            if (timings is not null)
            {
                trace["timings"] = timings.ToJson();
            }

            return trace.ToJsonString(JsonOptions);
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
        };
    }
}
