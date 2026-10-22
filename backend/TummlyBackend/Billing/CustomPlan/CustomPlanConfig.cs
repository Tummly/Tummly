using System.Text.Json;
using System.Text.Json.Serialization;

namespace TummlyBackend.Billing.CustomPlan
{
    public sealed class CustomPlanConfig
    {
        public const int MaxLocations = 500;

        /// <summary>1 = à-la-carte; 2 = base package + packs.</summary>
        [JsonPropertyName("version")]
        public int? Version { get; set; }

        [JsonPropertyName("catalogPublicId")]
        public string CatalogPublicId { get; set; } = string.Empty;

        [JsonPropertyName("cadence")]
        public string Cadence { get; set; } = "monthly";

        /// <summary>v2: starter | growth | group</summary>
        [JsonPropertyName("basePlan")]
        public string? BasePlan { get; set; }

        /// <summary>v2: configured Locations (1..500).</summary>
        [JsonPropertyName("locations")]
        public int? Locations { get; set; }

        /// <summary>v2: packKey → pack count (0..99).</summary>
        [JsonPropertyName("packs")]
        public Dictionary<string, int>? Packs { get; set; }

        [JsonPropertyName("quantities")]
        public CustomPlanQuantities Quantities { get; set; } = new();

        [JsonPropertyName("monthlyNetPence")]
        public int MonthlyNetPence { get; set; }

        [JsonPropertyName("periodNetPence")]
        public int PeriodNetPence { get; set; }

        [JsonPropertyName("lines")]
        public List<CustomPlanLine>? Lines { get; set; }

        [JsonPropertyName("rateSnapshot")]
        public List<CustomPlanRateSnapshot> RateSnapshot { get; set; } = [];

        public bool IsV2 => Version == 2 || !string.IsNullOrWhiteSpace(BasePlan);

        public static CustomPlanConfig? TryParse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<CustomPlanConfig>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    }
                );
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    public sealed class CustomPlanQuantities
    {
        [JsonPropertyName("locations")]
        public int Locations { get; set; }

        [JsonPropertyName("teamUsers")]
        public int TeamUsers { get; set; }

        [JsonPropertyName("qrPlacements")]
        public int QrPlacements { get; set; }

        /// <summary>
        /// Account-wide QR pool from qr_10 packs (v2). Not a per-Location cap.
        /// </summary>
        [JsonPropertyName("qrPlacementsAccountExtra")]
        public int QrPlacementsAccountExtra { get; set; }

        [JsonPropertyName("offers")]
        public int Offers { get; set; }

        [JsonPropertyName("emailCredits")]
        public int EmailCredits { get; set; }

        [JsonPropertyName("smsCredits")]
        public int SmsCredits { get; set; }

        [JsonPropertyName("aiCredits")]
        public int AiCredits { get; set; }
    }

    public sealed class CustomPlanLine
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("netPence")]
        public int NetPence { get; set; }
    }

    public sealed class CustomPlanRateSnapshot
    {
        [JsonPropertyName("rateKey")]
        public string RateKey { get; set; } = string.Empty;

        [JsonPropertyName("unitQuantity")]
        public int UnitQuantity { get; set; } = 1;

        [JsonPropertyName("monthlyNetPence")]
        public int MonthlyNetPence { get; set; }

        [JsonPropertyName("annualNetPence")]
        public int? AnnualNetPence { get; set; }
    }
}
