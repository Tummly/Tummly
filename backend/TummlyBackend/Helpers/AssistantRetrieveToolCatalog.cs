using System.Text.Json.Nodes;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Azure OpenAI native tool definitions for Assistant live-answer turns.
    /// Names map to KOL logical reads; location/period come from server context.
    /// </summary>
    public static class AssistantRetrieveToolCatalog
    {
        public const string ReadFeedbackSummary = "read_feedback_summary";
        public const string ReadOffers = "read_offers";
        public const string ReadCampaigns = "read_campaigns";
        public const string ReadCapturePerformance = "read_capture_performance";
        public const string ReadHomeKpis = "read_home_kpis";
        public const string ReadGuests = "read_guests";
        public const string ReadBillingPlan = "read_billing_plan";
        public const string CompareLocations = "compare_locations";
        public const string CompareAllLocations = "compare_all_locations";

        public static readonly string[] All =
        [
            ReadFeedbackSummary,
            ReadOffers,
            ReadCampaigns,
            ReadCapturePerformance,
            ReadHomeKpis,
            ReadGuests,
            ReadBillingPlan,
            CompareLocations,
            CompareAllLocations,
        ];

        public static readonly string[] DomainReads =
        [
            ReadFeedbackSummary,
            ReadOffers,
            ReadCampaigns,
            ReadCapturePerformance,
            ReadHomeKpis,
            ReadGuests,
        ];

        public static bool IsKnown(string? name)
            => name is not null
                && All.Contains(name, StringComparer.Ordinal);

        public static bool IsCompareTool(string? name)
            => name is CompareLocations or CompareAllLocations;

        /// <summary>
        /// Forced / Fake tool wave for a detected ask focus. Billing is not in
        /// <see cref="DomainReads"/> so ordinary venue asks do not load it.
        /// </summary>
        public static IReadOnlyList<string> DomainReadsForFocus(
            AssistantAskFocusKind focus
        )
            => focus switch
            {
                AssistantAskFocusKind.Feedback
                    => [ReadFeedbackSummary],
                AssistantAskFocusKind.OffersClaims
                    or AssistantAskFocusKind.OffersRedemptions
                    => [ReadOffers],
                AssistantAskFocusKind.CampaignsActive
                    or AssistantAskFocusKind.CampaignsAny
                    => [ReadCampaigns],
                AssistantAskFocusKind.CaptureQr
                    => [ReadCapturePerformance],
                AssistantAskFocusKind.Performance
                    => [ReadHomeKpis],
                AssistantAskFocusKind.Guests
                    => [ReadGuests],
                AssistantAskFocusKind.Billing
                    => [ReadBillingPlan],
                AssistantAskFocusKind.CreateCampaign
                    => [ReadCampaigns, ReadOffers],
                AssistantAskFocusKind.CreateOffer
                    => [ReadOffers],
                _ => DomainReads,
            };

        public static JsonArray BuildToolsArray()
            => new(
                Tool(
                    ReadFeedbackSummary,
                    "Read Feedback summary counts, themes, and sample rows for the Analysis scope Reporting period."
                ),
                Tool(
                    ReadOffers,
                    "Read Offers catalog and Offers Performance (claims, redemptions, logs) for the Analysis scope Reporting period."
                ),
                Tool(
                    ReadCampaigns,
                    "Read Campaigns list, eligibility, and detail metadata for the Analysis scope. Use includeCampaignCopy only when the ask needs message subject or body."
                ),
                Tool(
                    ReadCapturePerformance,
                    "Read Capture QR scan and funnel KPIs for the Analysis scope Reporting period."
                ),
                Tool(
                    ReadHomeKpis,
                    "Read Home Performance overview KPIs (Feedback submitted, Guests joined, QR scans) for the Analysis scope Reporting period."
                ),
                Tool(
                    ReadGuests,
                    "Read Location Guest sample rows and counts at the scoped Owned location. Not limited to the Reporting period."
                ),
                Tool(
                    ReadBillingPlan,
                    "Read the restaurant plan name, subscription or billing status, and Email / SMS / AI credit balances the operator may view. Read only — do not purchase, top up, change plan, or claim Revolut payment success."
                ),
                Tool(
                    CompareLocations,
                    "Compare authorised Owned locations from the server compare set. Do not invent Location ids."
                ),
                Tool(
                    CompareAllLocations,
                    "Compare all owned Locations in Analysis All scope with thin packs, retrieve budget, and failed or not-started names."
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
                    ["parameters"] = ParametersFor(name),
                },
            };

        private static JsonObject ParametersFor(string name)
        {
            if (name == ReadCampaigns)
            {
                return new JsonObject
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["properties"] = new JsonObject
                    {
                        ["includeCampaignCopy"] = new JsonObject
                        {
                            ["type"] = "boolean",
                            ["description"] =
                                "True only when the operator needs Campaign message subject or body.",
                        },
                    },
                    ["required"] = new JsonArray(),
                };
            }

            return new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["properties"] = new JsonObject(),
                ["required"] = new JsonArray(),
            };
        }
    }
}
