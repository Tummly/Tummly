namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Closed product-expert topics. Concatenation order is the enum order:
    /// Analysis scope → Campaign vs Offer → Statuses → Draft vs send →
    /// Capabilities.
    /// </summary>
    public enum AssistantProductExpertTopic
    {
        AnalysisScope = 0,
        CampaignVsOffer = 1,
        Statuses = 2,
        DraftVsSend = 3,
        Capabilities = 4,
    }

    public readonly record struct AssistantProductExpertAnswer(
        string Title,
        string Body,
        string ConversationTitle
    );

    /// <summary>
    /// Product-expert Retrieve needles. Help Centre / Support refuse first.
    /// </summary>
    public static class AssistantProductExpertTopics
    {
        private static readonly string[] CapabilitiesNeedles =
        [
            "what can you do",
            "what can you help",
            "what are your capabilities",
            "what does the assistant do",
            "what does the ai assistant do",
        ];

        private static readonly string[] CampaignVsOfferNeedles =
        [
            "campaign vs offer",
            "difference between campaign and offer",
            "difference between a campaign and an offer",
            "campaign versus offer",
        ];

        private static readonly string[] StatusesNeedles =
        [
            "campaign status",
            "offer status",
            "what is draft",
            "draft vs active",
            "active vs draft",
            "what does active mean",
            "in flight",
            "in-flight",
        ];

        private static readonly string[] AnalysisScopeNeedles =
        [
            "analysis scope",
            "change scope",
            "change analysis scope",
            "reporting period",
            "all owned locations",
            "all locations scope",
        ];

        private static readonly string[] DraftVsSendNeedles =
        [
            "does the assistant send",
            "does the ai assistant send",
            "will you send",
            "can you schedule",
            "draft vs send",
            "does it go live",
            "send from chat",
            "schedule from chat",
        ];

        private static readonly string[] CapabilityGuardNeedles =
        [
            "assistant",
            "ai assistant",
            "from chat",
            "draft vs send",
            "does",
        ];

        public static IReadOnlyList<AssistantProductExpertTopic> Detect(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            if (lower.Length == 0)
            {
                return [];
            }

            var topics = new List<AssistantProductExpertTopic>();
            if (ContainsAny(lower, AnalysisScopeNeedles))
            {
                topics.Add(AssistantProductExpertTopic.AnalysisScope);
            }

            if (ContainsAny(lower, CampaignVsOfferNeedles))
            {
                topics.Add(AssistantProductExpertTopic.CampaignVsOffer);
            }

            if (ContainsAny(lower, StatusesNeedles))
            {
                topics.Add(AssistantProductExpertTopic.Statuses);
            }

            if (ContainsAny(lower, DraftVsSendNeedles) && HasCapabilityGuard(lower))
            {
                topics.Add(AssistantProductExpertTopic.DraftVsSend);
            }

            if (ContainsAny(lower, CapabilitiesNeedles))
            {
                topics.Add(AssistantProductExpertTopic.Capabilities);
            }

            return topics;
        }

        public static bool IsMixedRetrieve(string message)
        {
            var topics = Detect(message);
            if (topics.Count == 0)
            {
                return false;
            }

            return HasRemainingRetrieveAsk(message, topics);
        }

        /// <summary>
        /// Remove matched product-expert needles so Compare does not treat
        /// "vs" / "all owned locations" product asks as a Compare turn.
        /// </summary>
        public static string StripMatchedNeedles(string message)
            => StripNeedles(message, Detect(message));

        public static AssistantProductExpertAnswer Assemble(
            IReadOnlyList<AssistantProductExpertTopic> topics
        )
        {
            var ordered = topics
                .Distinct()
                .OrderBy(topic => (int)topic)
                .ToList();
            if (ordered.Count == 0)
            {
                return new AssistantProductExpertAnswer(
                    AssistantProductExpertCopy.CapabilitiesTitle,
                    AssistantProductExpertCopy.CapabilitiesBody,
                    AssistantProductExpertCopy.CapabilitiesConversationTitle
                );
            }

            var body = string.Join("\n\n", ordered.Select(BodyFor));
            if (ordered.Count > 1)
            {
                return new AssistantProductExpertAnswer(
                    AssistantProductExpertCopy.MultiTopicTitle,
                    body,
                    AssistantProductExpertCopy.MultiTopicTitle
                );
            }

            var topic = ordered[0];
            return new AssistantProductExpertAnswer(
                TitleFor(topic),
                body,
                ConversationTitleFor(topic)
            );
        }

        private static bool HasRemainingRetrieveAsk(
            string message,
            IReadOnlyList<AssistantProductExpertTopic> topics
        )
        {
            var stripped = StripNeedles(message, topics);
            return AssistantAskIntent.HasRetrieveAsk(stripped)
                || AssistantAskIntent.HasExplicitRetrieveAsk(stripped);
        }

        private static string StripNeedles(
            string message,
            IReadOnlyList<AssistantProductExpertTopic> topics
        )
        {
            var stripped = message.Trim().ToLowerInvariant();
            foreach (var needle in NeedlesFor(topics))
            {
                stripped = stripped.Replace(needle, " ", StringComparison.Ordinal);
            }

            return stripped;
        }

        private static IEnumerable<string> NeedlesFor(
            IReadOnlyList<AssistantProductExpertTopic> topics
        )
        {
            foreach (var topic in topics)
            {
                var needles = topic switch
                {
                    AssistantProductExpertTopic.Capabilities => CapabilitiesNeedles,
                    AssistantProductExpertTopic.CampaignVsOffer => CampaignVsOfferNeedles,
                    AssistantProductExpertTopic.Statuses => StatusesNeedles,
                    AssistantProductExpertTopic.AnalysisScope => AnalysisScopeNeedles,
                    AssistantProductExpertTopic.DraftVsSend => DraftVsSendNeedles,
                    _ => Array.Empty<string>(),
                };
                foreach (var needle in needles)
                {
                    yield return needle;
                }
            }
        }

        private static string TitleFor(AssistantProductExpertTopic topic)
            => topic switch
            {
                AssistantProductExpertTopic.Capabilities =>
                    AssistantProductExpertCopy.CapabilitiesTitle,
                AssistantProductExpertTopic.CampaignVsOffer =>
                    AssistantProductExpertCopy.CampaignVsOfferTitle,
                AssistantProductExpertTopic.Statuses =>
                    AssistantProductExpertCopy.StatusesTitle,
                AssistantProductExpertTopic.AnalysisScope =>
                    AssistantProductExpertCopy.AnalysisScopeTitle,
                AssistantProductExpertTopic.DraftVsSend =>
                    AssistantProductExpertCopy.DraftVsSendTitle,
                _ => AssistantProductExpertCopy.MultiTopicTitle,
            };

        private static string ConversationTitleFor(AssistantProductExpertTopic topic)
            => topic == AssistantProductExpertTopic.Capabilities
                ? AssistantProductExpertCopy.CapabilitiesConversationTitle
                : TitleFor(topic);

        private static string BodyFor(AssistantProductExpertTopic topic)
            => topic switch
            {
                AssistantProductExpertTopic.Capabilities =>
                    AssistantProductExpertCopy.CapabilitiesBody,
                AssistantProductExpertTopic.CampaignVsOffer =>
                    AssistantProductExpertCopy.CampaignVsOfferBody,
                AssistantProductExpertTopic.Statuses =>
                    AssistantProductExpertCopy.StatusesBody,
                AssistantProductExpertTopic.AnalysisScope =>
                    AssistantProductExpertCopy.AnalysisScopeBody,
                AssistantProductExpertTopic.DraftVsSend =>
                    AssistantProductExpertCopy.DraftVsSendBody,
                _ => AssistantProductExpertCopy.CapabilitiesBody,
            };

        private static bool HasCapabilityGuard(string lower)
            => ContainsAny(lower, CapabilityGuardNeedles);

        private static bool ContainsAny(string haystack, IReadOnlyList<string> needles)
        {
            foreach (var needle in needles)
            {
                if (haystack.Contains(needle, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
