namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Named create targets on one send. Two or more is a Gap turn.
    /// Campaign uses outcome language, not the noun "campaign" alone.
    /// </summary>
    public static class AssistantCreateTargets
    {
        public const string Campaign = "Campaign";
        public const string Offer = "Offer";
        public const string Recovery = "Feedback recovery";

        public static readonly IReadOnlyList<string> UnnamedOptions =
            [Campaign, Offer, Recovery];

        public static IReadOnlyList<string> Detect(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            var targets = new List<string>();
            if (AssistantTaskClassification.LooksLikeCreateCampaignDraft(message))
            {
                targets.Add(Campaign);
            }

            if (AssistantOfferDraftInterview.IsOfferDraftAsk(message)
                || (targets.Contains(Campaign)
                    && LooksLikeNewOfferBesideCampaign(lower)))
            {
                if (!targets.Contains(Offer, StringComparer.Ordinal))
                {
                    targets.Add(Offer);
                }
            }

            if (AssistantRecoveryDraftInterview.IsRecoveryDraftAsk(message))
            {
                targets.Add(Recovery);
            }

            if (targets.Count == 0
                && ContainsAny(
                    lower,
                    "help me draft something",
                    "help me create something",
                    "help me prepare something",
                    "what can you draft"
                ))
            {
                return UnnamedOptions;
            }

            if (targets.Count == 0
                && ContainsAny(
                    lower,
                    "help me follow up",
                    "help me reach out"
                ))
            {
                return [Recovery];
            }

            return targets;
        }

        public static string? Resolve(IReadOnlyList<string> options, string message)
        {
            var normalized = message.Trim().Trim('.', ',', ';', ':').ToLowerInvariant();
            var matches = options
                .Where(option => Matches(option, normalized))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            return matches.Count == 1 ? matches[0] : null;
        }

        private static bool LooksLikeNewOfferBesideCampaign(string lower)
            => ContainsAny(
                lower,
                "and an offer",
                "and a new offer",
                "and create an offer",
                "and draft an offer",
                "and an offer draft",
                "plus an offer"
            );

        private static bool Matches(string option, string normalized)
            => option switch
            {
                Campaign => normalized.Contains("campaign", StringComparison.Ordinal),
                Offer => normalized.Contains("offer", StringComparison.Ordinal)
                    && !normalized.Contains("campaign", StringComparison.Ordinal),
                Recovery =>
                    normalized.Contains("recovery", StringComparison.Ordinal)
                    || normalized.Contains("feedback", StringComparison.Ordinal)
                    || normalized.Contains("reply to the guest", StringComparison.Ordinal)
                    || normalized.Contains("respond to the guest", StringComparison.Ordinal),
                _ => false,
            };

        private static bool ContainsAny(string lower, params string[] needles)
            => needles.Any(needle => lower.Contains(needle, StringComparison.Ordinal));
    }
}
