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
        public const string KindOffer = "offer-title";
        public const string KindAudience = "audience";
        public const string KindChannel = "channel";

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

        public static AssistantGapState CreateOffer(
            IReadOnlyList<string> options,
            string sourceUserMessage,
            string assistantTask
        )
            => CreateNamed(KindOffer, options, sourceUserMessage, assistantTask);

        public static AssistantGapState CreateAudience(
            IReadOnlyList<string> options,
            string sourceUserMessage,
            string assistantTask
        )
            => CreateNamed(KindAudience, options, sourceUserMessage, assistantTask);

        public static AssistantGapState CreateChannel(
            IReadOnlyList<string> options,
            string sourceUserMessage,
            string assistantTask
        )
            => CreateNamed(KindChannel, options, sourceUserMessage, assistantTask);

        private static AssistantGapState CreateNamed(
            string kind,
            IReadOnlyList<string> options,
            string sourceUserMessage,
            string assistantTask
        )
            => new()
            {
                Kind = kind,
                AssistantTask = assistantTask,
                Options = options.ToList(),
                SourceUserMessage = sourceUserMessage,
            };

        public static bool IsBindKind(string kind)
            => kind is KindOffer or KindAudience or KindChannel;

        private static bool IsKnownKind(string kind)
            => kind is KindCreateTarget or KindLocation
                or KindOffer or KindAudience or KindChannel;

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
                    || !IsKnownKind(state.Kind))
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

        public static string RepeatBindBody(AssistantGapState state)
            => state.Kind switch
            {
                KindOffer => AssistantCampaignDraftBind.OfferClashBody(state.Options),
                KindAudience => AssistantCampaignDraftBind.AudienceClashBody(state.Options),
                KindChannel => AssistantCampaignDraftBind.ChannelClashBody(),
                _ => RepeatLocationBody(state),
            };
    }
}
