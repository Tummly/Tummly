using System.Text.Json;

namespace TummlyBackend.Helpers
{
    public sealed class AssistantDraftTargetChoiceState
    {
        public string Target { get; set; } = "draft-target-choice";
        public List<string> Options { get; set; } = [];
    }

    public static class AssistantDraftTargetChoice
    {
        public static AssistantDraftTargetChoiceState Create(
            IEnumerable<string> options
        )
            => new()
            {
                Options = options
                    .Distinct(StringComparer.Ordinal)
                    .ToList(),
            };

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

        public static string Serialize(AssistantDraftTargetChoiceState state)
            => JsonSerializer.Serialize(state);

        public static string? Resolve(
            AssistantDraftTargetChoiceState state,
            string message
        )
        {
            var normalized = message.Trim().Trim('.', ',', ';', ':').ToLowerInvariant();
            var matches = state.Options
                .Where(option => Matches(option, normalized))
                .ToList();
            return matches.Count == 1 ? matches[0] : null;
        }

        private static bool Matches(string option, string normalized)
            => option switch
            {
                "Campaign" => normalized is "campaign"
                    or "campaign draft"
                    or "draft campaign"
                    or "a campaign",
                "Offer" => normalized is "offer"
                    or "offer draft"
                    or "draft offer"
                    or "an offer",
                "Feedback recovery" => normalized is "feedback recovery"
                    or "recovery"
                    or "recovery draft"
                    or "draft recovery"
                    or "feedback",
                _ => false,
            };
    }
}
