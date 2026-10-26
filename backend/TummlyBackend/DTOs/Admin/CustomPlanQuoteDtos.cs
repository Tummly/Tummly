using System.Text.Json.Serialization;
using TummlyBackend.Billing.CustomPlan;

namespace TummlyBackend.DTOs.Admin
{
    public sealed class CustomPlanQuoteRequest
    {
        public string Cadence { get; set; } = "monthly";

        /// <summary>starter | growth | group | null (use minimum for locations).</summary>
        public string? BasePlan { get; set; }

        /// <summary>Decimal so a fractional or negative count gets our error code, not a JSON bind failure.</summary>
        public decimal Locations { get; set; } = 1;

        public Dictionary<string, decimal>? Packs { get; set; }
    }

    /// <summary>Full v2 config plus quote metadata (contract §6.2).</summary>
    public sealed class CustomPlanQuoteResult
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 2;

        [JsonPropertyName("catalogPublicId")]
        public string CatalogPublicId { get; set; } = string.Empty;

        [JsonPropertyName("cadence")]
        public string Cadence { get; set; } = "monthly";

        [JsonPropertyName("basePlan")]
        public string BasePlan { get; set; } = string.Empty;

        [JsonPropertyName("locations")]
        public int Locations { get; set; }

        [JsonPropertyName("packs")]
        public Dictionary<string, int> Packs { get; set; } = new(StringComparer.Ordinal);

        [JsonPropertyName("quantities")]
        public CustomPlanQuantities Quantities { get; set; } = new();

        [JsonPropertyName("monthlyNetPence")]
        public int MonthlyNetPence { get; set; }

        [JsonPropertyName("periodNetPence")]
        public int PeriodNetPence { get; set; }

        [JsonPropertyName("lines")]
        public List<CustomPlanLine> Lines { get; set; } = [];

        [JsonPropertyName("rateSnapshot")]
        public List<CustomPlanRateSnapshot> RateSnapshot { get; set; } = [];

        [JsonPropertyName("basePlanChanged")]
        public bool BasePlanChanged { get; set; }

        [JsonPropertyName("requestedBasePlan")]
        public string? RequestedBasePlan { get; set; }

        [JsonPropertyName("storedAsPlan")]
        public string StoredAsPlan { get; set; } = "Custom";

        [JsonPropertyName("vatRateBps")]
        public int VatRateBps { get; set; }

        /// <summary>32 lower-hex chars; see <see cref="CustomPlanQuoteHash"/>.</summary>
        [JsonPropertyName("quoteHash")]
        public string QuoteHash { get; set; } = string.Empty;

        public CustomPlanConfig ToConfig()
        {
            return new CustomPlanConfig
            {
                Version = Version,
                CatalogPublicId = CatalogPublicId,
                Cadence = Cadence,
                BasePlan = BasePlan,
                Locations = Locations,
                Packs = new Dictionary<string, int>(Packs, StringComparer.Ordinal),
                Quantities = Quantities,
                MonthlyNetPence = MonthlyNetPence,
                PeriodNetPence = PeriodNetPence,
                Lines = Lines,
                RateSnapshot = RateSnapshot,
                QuoteHash = QuoteHash,
            };
        }
    }

    public sealed class PublicCatalogPlanSummaryDto
    {
        public string PlanKey { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public int MonthlyNetPence { get; set; }

        public int AnnualNetPence { get; set; }

        public int IncludedLocations { get; set; }

        public int IncludedTeamMembers { get; set; }

        public int ActiveOffersAccount { get; set; }

        public int ActiveQrPlacementsPerLocation { get; set; }

        public int CreditsMonthlyAi { get; set; }

        public int CreditsMonthlyEmail { get; set; }

        public int CreditsMonthlySms { get; set; }
    }

    public sealed class PublicExtraLocationSummaryDto
    {
        public int UsersAdded { get; set; }

        public int CreditsMonthlyAi { get; set; }

        public int CreditsMonthlyEmail { get; set; }

        public int CreditsMonthlySms { get; set; }
    }

    public sealed class PublicCatalogPackMetaDto
    {
        public string PackKey { get; set; } = string.Empty;

        public string RateKey { get; set; } = string.Empty;

        public int PackSize { get; set; }

        /// <summary>Server price for one pack. Null when the rate row is missing.</summary>
        public int? MonthlyNetPence { get; set; }

        /// <summary>Server price for one pack. Null when the rate row is missing.</summary>
        public int? AnnualNetPence { get; set; }
    }
}
