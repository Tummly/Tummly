using System.Text.Json;
using TummlyBackend.Configurations;

namespace TummlyBackend.Helpers
{
    public static class AssistantTurnTrace
    {
        public const string InstructionVersion = "assistant-2026-10-09";

        public static string Serialize(
            FeedbackClassificationSettings settings,
            IReadOnlyList<string> toolNames
        )
        {
            var deployment = string.IsNullOrWhiteSpace(settings.AssistantDeploymentName)
                ? settings.DeploymentName
                : settings.AssistantDeploymentName.Trim();
            return JsonSerializer.Serialize(new
            {
                modelDeployment = deployment,
                instructionVersion = InstructionVersion,
                knowledgeVersion = AssistantKnowledgeLayer.Version,
                schemaVersion = settings.PromptSchemaVersion,
                tools = toolNames.Distinct(StringComparer.Ordinal).ToArray(),
            });
        }
    }
}
