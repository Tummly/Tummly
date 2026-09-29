using System.Text.Json.Nodes;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Read-only Azure tool names for non-Assistant AI surfaces (drafts, brief,
    /// recommendations). Server binds location / week / offer; model cannot widen.
    /// </summary>
    public static class SurfaceReadToolCatalog
    {
        public const string ReadLocationDisplayName = "read_location_display_name";
        public const string ReadConfirmedOfferFacts = "read_confirmed_offer_facts";
        public const string ReadRecoveryFeedbackFacts = "read_recovery_feedback_facts";
        public const string ReadWeeklyBriefMetrics = "read_weekly_brief_metrics";
        public const string ReadHomeRecommendationMetrics =
            "read_home_recommendation_metrics";
        public const string ReadCampaignRecommendationMetrics =
            "read_campaign_recommendation_metrics";
        public const string ReadOfferRecommendationMetrics =
            "read_offer_recommendation_metrics";

        public static JsonArray CampaignDraftTools(bool includeOffer)
        {
            var tools = new JsonArray
            {
                Tool(
                    ReadLocationDisplayName,
                    "Read the Owned location display name for this draft."
                ),
            };
            if (includeOffer)
            {
                tools.Add(
                    Tool(
                        ReadConfirmedOfferFacts,
                        "Read confirmed Offer catalog facts for grounding. Never returns redemption codes."
                    )
                );
            }

            return tools;
        }

        public static JsonArray RecoveryDraftTools(bool includeOffer)
        {
            var tools = new JsonArray
            {
                Tool(
                    ReadLocationDisplayName,
                    "Read the Owned location display name for this recovery draft."
                ),
                Tool(
                    ReadRecoveryFeedbackFacts,
                    "Read Feedback comment, sentiment, issue tags, and guest display name. Never returns email or phone."
                ),
            };
            if (includeOffer)
            {
                tools.Add(
                    Tool(
                        ReadConfirmedOfferFacts,
                        "Read confirmed Offer catalog facts for grounding. Never returns redemption codes."
                    )
                );
            }

            return tools;
        }

        public static JsonArray WeeklyBriefTools()
            => new(
                Tool(
                    ReadWeeklyBriefMetrics,
                    "Read aggregate Weekly brief metrics for the closed week. Counts and tag rollups only — no guest PII."
                )
            );

        public static JsonArray HomeRecommendationTools()
            => new(
                Tool(
                    ReadHomeRecommendationMetrics,
                    "Read Home recommendation metrics for the selected type. Counts only — no guest PII."
                )
            );

        public static JsonArray CampaignRecommendationTools()
            => new(
                Tool(
                    ReadCampaignRecommendationMetrics,
                    "Read Campaign recommendation metrics. Counts only — no guest PII."
                )
            );

        public static JsonArray OfferRecommendationTools()
            => new(
                Tool(
                    ReadOfferRecommendationMetrics,
                    "Read Offer recommendation metrics. Counts only — no guest PII."
                )
            );

        private static JsonObject Tool(string name, string description)
            => new()
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = name,
                    ["description"] = description,
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["additionalProperties"] = false,
                        ["properties"] = new JsonObject(),
                        ["required"] = new JsonArray(),
                    },
                },
            };
    }
}
