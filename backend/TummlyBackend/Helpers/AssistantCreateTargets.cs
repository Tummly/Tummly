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
            if (AssistantAskIntent.IsHelpCentreAsk(message)
                || AssistantAskIntent.IsFullRefusal(AssistantAskIntent.Classify(message)))
            {
                return null;
            }

            var normalized = message.Trim().Trim('.', ',', ';', ':').ToLowerInvariant();
            var exact = options
                .Where(option => IsExactChoice(option, normalized))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (exact.Count == 1)
            {
                return exact[0];
            }

            var detected = Detect(message)
                .Where(item => options.Contains(item, StringComparer.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            return detected.Count == 1 ? detected[0] : null;
        }

        private static bool IsExactChoice(string option, string normalized)
        {
            var label = option.ToLowerInvariant();
            if (normalized == label || normalized == $"the {label}")
            {
                return true;
            }

            return option switch
            {
                Campaign => normalized is "campaign"
                    or "the campaign"
                    or "a campaign"
                    or "campaign draft",
                Offer => normalized is "offer"
                    or "the offer"
                    or "an offer"
                    or "offer draft",
                Recovery => normalized is "recovery"
                    or "feedback recovery"
                    or "feedback",
                _ => false,
            };
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

        private static bool ContainsAny(string lower, params string[] needles)
            => needles.Any(needle => lower.Contains(needle, StringComparison.Ordinal));
    }
}
