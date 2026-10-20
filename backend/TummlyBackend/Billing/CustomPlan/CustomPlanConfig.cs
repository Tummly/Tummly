using System.Text.Json;
using System.Text.Json.Serialization;

namespace TummlyBackend.Billing.CustomPlan
{
    public sealed class CustomPlanConfig
    {
        public const int MaxLocations = 500;

        [JsonPropertyName("catalogPublicId")]
        public string CatalogPublicId { get; set; } = string.Empty;

        [JsonPropertyName("cadence")]
        public string Cadence { get; set; } = "monthly";

        [JsonPropertyName("quantities")]
        public CustomPlanQuantities Quantities { get; set; } = new();

        [JsonPropertyName("monthlyNetPence")]
        public int MonthlyNetPence { get; set; }

        [JsonPropertyName("periodNetPence")]
        public int PeriodNetPence { get; set; }

        [JsonPropertyName("rateSnapshot")]
        public List<CustomPlanRateSnapshot> RateSnapshot { get; set; } = [];

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

        [JsonPropertyName("offers")]
        public int Offers { get; set; }

        [JsonPropertyName("emailCredits")]
        public int EmailCredits { get; set; }

        [JsonPropertyName("smsCredits")]
        public int SmsCredits { get; set; }

        [JsonPropertyName("aiCredits")]
        public int AiCredits { get; set; }
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
