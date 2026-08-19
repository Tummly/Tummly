using System.Text.Json;

namespace TummlyBackend.Helpers
{
    public sealed class AssistantDraftTargetChoiceState
    {
        public string Target { get; set; } = "draft-target-choice";
        public List<string> Options { get; set; } = [];
    }

    /// <summary>
    /// Reads leftover draft-target-choice JSON from earlier interviews.
    /// New two-target questions are Gap turns.
    /// </summary>
    public static class AssistantDraftTargetChoice
    {
        public static AssistantDraftTargetChoiceState? Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var state = JsonSerializer.Deserialize<AssistantDraftTargetChoiceState>(
                    json
                );
                return state?.Target == "draft-target-choice"
                    && state.Options.Count > 1
                    ? state
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
