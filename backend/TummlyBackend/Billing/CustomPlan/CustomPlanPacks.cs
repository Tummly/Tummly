using TummlyBackend.Models;

namespace TummlyBackend.Billing.CustomPlan
{
    /// <summary>
    /// Fixed pack catalogue (contract §1). Same keys on front and back.
    /// </summary>
    public static class CustomPlanPacks
    {
        public const int MaxPackCount = 99;

        public const double AnnualFallbackFactor = 0.85;

        public sealed record PackDef(string PackKey, string RateKey, int PackSize);

        public static readonly IReadOnlyList<PackDef> All =
        [
            new("team_users_5", CommercialCustomUnitRateKeys.TeamUser, 5),
            new("offers_5", CommercialCustomUnitRateKeys.Offer, 5),
            new("qr_10", CommercialCustomUnitRateKeys.QrPlacement, 10),
            new("email_20k", CommercialCustomUnitRateKeys.EmailCredit, 20_000),
            new("email_50k", CommercialCustomUnitRateKeys.EmailCredit, 50_000),
            new("sms_500", CommercialCustomUnitRateKeys.SmsCredit, 500),
            new("sms_1000", CommercialCustomUnitRateKeys.SmsCredit, 1_000),
            new("sms_5000", CommercialCustomUnitRateKeys.SmsCredit, 5_000),
            new("ai_500", CommercialCustomUnitRateKeys.AiCredit, 500),
            new("ai_2000", CommercialCustomUnitRateKeys.AiCredit, 2_000),
        ];

        public static readonly IReadOnlyDictionary<string, PackDef> ByKey =
            All.ToDictionary(p => p.PackKey, StringComparer.Ordinal);

        public static bool IsKnownPackKey(string packKey) =>
            ByKey.ContainsKey(packKey);
    }
}
