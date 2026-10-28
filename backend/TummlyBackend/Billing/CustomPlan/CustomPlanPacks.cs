using TummlyBackend.Models;

namespace TummlyBackend.Billing.CustomPlan
{
    /// <summary>
    /// Fixed pack catalogue (contract §1). Same keys on front and back.
    /// Team users, Offers and QR placements are single units (Commercial V4):
    /// the quantity is the number of units, priced at the rate per unit.
    /// </summary>
    public static class CustomPlanPacks
    {
        /// <summary>Most credit packs of one kind per quote.</summary>
        public const int MaxPackCount = 99;

        /// <summary>Most single capacity units (seats, Offers, QR placements) of one kind per quote.</summary>
        public const int MaxCapacityUnits = 500;

        public const double AnnualFallbackFactor = 0.85;

        public sealed record PackDef(
            string PackKey,
            string RateKey,
            int PackSize,
            int MaxCount = MaxPackCount
        );

        /// <summary>
        /// Capacity packs that Commercial V4 replaced by single units. They are no
        /// longer purchasable. Value = the single-unit key that replaces each one.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> LegacyCapacityPackKeys =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["team_users_5"] = TeamUser,
                ["offers_5"] = Offer,
                ["qr_10"] = QrPlacement,
            };

        public const string TeamUser = "team_user";

        public const string Offer = "offer";

        public const string QrPlacement = "qr_placement";

        public static readonly IReadOnlyList<PackDef> All =
        [
            new(TeamUser, CommercialCustomUnitRateKeys.TeamUser, 1, MaxCapacityUnits),
            new(Offer, CommercialCustomUnitRateKeys.Offer, 1, MaxCapacityUnits),
            new(QrPlacement, CommercialCustomUnitRateKeys.QrPlacement, 1, MaxCapacityUnits),
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

        public static bool IsCapacityPackKey(string packKey) =>
            packKey is TeamUser or Offer or QrPlacement;
    }
}
