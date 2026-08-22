using TummlyBackend.DTOs.Assistant;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Server-owned explain-why expansion. Reuses snapshot facts. Does not
    /// invent counts. Does not add Action rows.
    /// </summary>
    public static class AssistantExplainWhyCopy
    {
        public const string NeedsAttentionInterpretation =
            "These are the current Home Needs attention items at this Owned location.";

        public const string RecommendedNextStepInterpretation =
            "This is the Home Recommended next step for the saved Reporting period.";

        public const string MixInterpretation =
            "The queue is the now-queue. The next step is the Home Recommended next step for the saved Reporting period.";

        public const string WeeklyBriefInterpretation =
            "This is the Weekly brief for the closed prior week at this Owned location.";

        public const string CombinedCreateInterpretation =
            "These facts are the Campaign Draft and Offer that this turn saved.";

        public const string DomainRetrieveInterpretation =
            "These results are the retrieved Tummly facts from the prior turn in this Analysis scope.";

        public const string CapabilitiesInterpretation =
            "The AI Assistant reads facts in your current Analysis scope and saves Drafts. It does not send, schedule, or issue from chat.";

        public const string CampaignVsOfferInterpretation =
            "A Campaign carries the message and audience. An Offers catalog offer carries the benefit terms.";

        public const string StatusesInterpretation =
            "Draft means not sent or not in flight. Active on an offer means at least one live attach.";

        public const string AnalysisScopeInterpretation =
            "Analysis scope is the Assistant data window: one Owned location or All owned locations, plus one Reporting period.";

        public const string DraftVsSendInterpretation =
            "The Assistant saves Drafts and prepares recovery. Send and schedule stay in Campaigns.";

        public const string MultiTopicInterpretation =
            "These are the shipped product facts for this ask.";

        public readonly record struct ExpandedAnswer(
            string Title,
            string Body,
            IReadOnlyList<AssistantActionDto> Actions
        );

        public static ExpandedAnswer Expand(
            AssistantExplainWhyKind kind,
            AssistantExplainWhyPriorPath path,
            string priorTitle,
            string priorBody,
            IReadOnlyList<AssistantActionDto> priorActions,
            IReadOnlyList<AssistantProductExpertTopic>? productTopics = null
        )
        {
            var body = priorBody;
            var interpretation = InterpretationFor(path, productTopics);
            if (interpretation.Length > 0)
            {
                body = EnsureInterpretation(body, interpretation);
            }

            if (kind == AssistantExplainWhyKind.NeedsAttention
                || path == AssistantExplainWhyPriorPath.NeedsAttention)
            {
                body = StripRecommendation(body);
            }

            return new ExpandedAnswer(priorTitle, body, priorActions);
        }

        public static string InterpretationFor(
            AssistantExplainWhyPriorPath path,
            IReadOnlyList<AssistantProductExpertTopic>? productTopics
        )
            => path switch
            {
                AssistantExplainWhyPriorPath.NeedsAttention =>
                    NeedsAttentionInterpretation,
                AssistantExplainWhyPriorPath.RecommendedNextStep =>
                    RecommendedNextStepInterpretation,
                AssistantExplainWhyPriorPath.Mix => MixInterpretation,
                AssistantExplainWhyPriorPath.WeeklyBrief =>
                    WeeklyBriefInterpretation,
                AssistantExplainWhyPriorPath.ProductExpert =>
                    ProductExpertInterpretation(productTopics),
                AssistantExplainWhyPriorPath.CombinedCreate =>
                    CombinedCreateInterpretation,
                AssistantExplainWhyPriorPath.DomainRetrieve =>
                    DomainRetrieveInterpretation,
                _ => string.Empty,
            };

        public static string EnsureInterpretation(string body, string restatement)
        {
            if (AssistantExplainWhyFollowUp.HasInterpretationLayer(body))
            {
                return body;
            }

            var block = $"{AssistantExplainWhyFollowUp.InterpretationHeading}\n\n{restatement}";
            var dataIndex = body.IndexOf(
                AssistantExplainWhyFollowUp.DataHeading,
                StringComparison.Ordinal
            );
            if (dataIndex >= 0)
            {
                var before = body[..dataIndex].TrimEnd();
                var after = body[dataIndex..];
                return before.Length == 0
                    ? $"{block}\n\n{after}"
                    : $"{before}\n\n{block}\n\n{after}";
            }

            var trimmed = body.Trim();
            return trimmed.Length == 0
                ? block
                : $"{block}\n\n{trimmed}";
        }

        private static string StripRecommendation(string body)
        {
            var index = body.IndexOf(
                AssistantExplainWhyFollowUp.RecommendationHeading,
                StringComparison.Ordinal
            );
            if (index < 0)
            {
                return body;
            }

            return body[..index].TrimEnd();
        }

        private static string ProductExpertInterpretation(
            IReadOnlyList<AssistantProductExpertTopic>? productTopics
        )
        {
            if (productTopics is null || productTopics.Count == 0)
            {
                return CapabilitiesInterpretation;
            }

            if (productTopics.Distinct().Count() > 1)
            {
                return MultiTopicInterpretation;
            }

            return productTopics[0] switch
            {
                AssistantProductExpertTopic.CampaignVsOffer =>
                    CampaignVsOfferInterpretation,
                AssistantProductExpertTopic.Statuses => StatusesInterpretation,
                AssistantProductExpertTopic.AnalysisScope =>
                    AnalysisScopeInterpretation,
                AssistantProductExpertTopic.DraftVsSend =>
                    DraftVsSendInterpretation,
                _ => CapabilitiesInterpretation,
            };
        }
    }
}
