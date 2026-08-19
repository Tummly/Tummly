using System.Text.Json;

namespace TummlyBackend.Helpers
{
    public sealed class AssistantGapState
    {
        public string Target { get; set; } = AssistantGapTurn.Target;
        public string Kind { get; set; } = string.Empty;
        public string AssistantTask { get; set; } = string.Empty;
        public List<string> Options { get; set; } = [];
        public string SourceUserMessage { get; set; } = string.Empty;
        public string? LocationKind { get; set; }
    }

    public static class AssistantGapTurn
    {
        public const string Target = "gap";
        public const string KindCreateTarget = "create-target";
        public const string KindLocation = "location";

        public static AssistantGapState CreateTarget(
            IReadOnlyList<string> options,
            string sourceUserMessage,
            string assistantTask
        )
            => new()
            {
                Kind = KindCreateTarget,
                AssistantTask = assistantTask,
                Options = options.Distinct(StringComparer.Ordinal).ToList(),
                SourceUserMessage = sourceUserMessage,
            };

        public static AssistantGapState CreateLocation(
            string locationKind,
            IReadOnlyList<string> options,
            string sourceUserMessage,
            string assistantTask
        )
            => new()
            {
                Kind = KindLocation,
                LocationKind = locationKind,
                AssistantTask = assistantTask,
                Options = options.ToList(),
                SourceUserMessage = sourceUserMessage,
            };

        public static AssistantGapState? Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var state = JsonSerializer.Deserialize<AssistantGapState>(json);
                if (state is null
                    || !string.Equals(state.Target, Target, StringComparison.Ordinal)
                    || (state.Kind != KindCreateTarget && state.Kind != KindLocation))
                {
                    return null;
                }

                return state;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static string Serialize(AssistantGapState state)
            => JsonSerializer.Serialize(state);

        public static string CreateTargetBody(IReadOnlyList<string> options)
        {
            var labels = options.Count == 0
                ? AssistantCreateTargets.UnnamedOptions
                : options;
            return $"Which should I create: {AssistantCreateLocationGap.Join(labels)}?";
        }

        public static bool LooksLikeContinueAnswer(string message)
        {
            var normalized = message.Trim().Trim('.', ',', ';', ':').ToLowerInvariant();
            return normalized is "ok"
                or "okay"
                or "yes"
                or "y"
                or "yep"
                or "sure"
                or "continue"
                or "go ahead"
                or "proceed";
        }

        public static string RepeatLocationBody(AssistantGapState state)
            => AssistantCreateLocationGap.RepeatBody(state.LocationKind, state.Options);
    }
}
