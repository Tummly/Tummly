namespace TummlyBackend.Services
{
    /// <summary>
    /// Current pricebook recurring lookup keys for Revolut plan variations
    /// (ticket 10 / 06). Top-ups have no variation UUID.
    /// </summary>
    public static class RevolutPlanVariationKeys
    {
        public const string StarterMonthly = "tummly_starter_monthly_gbp_v3";

        public const string StarterAnnual = "tummly_starter_annual_gbp_v3";

        public const string GrowthMonthly = "tummly_growth_monthly_gbp_v3";

        public const string GrowthAnnual = "tummly_growth_annual_gbp_v3";

        public const string GroupMonthly = "tummly_group_monthly_gbp_v3";

        public const string GroupAnnual = "tummly_group_annual_gbp_v3";

        public const string GroupLocationMonthly =
            "tummly_group_location_monthly_gbp_v3";

        public const string GroupLocationAnnual =
            "tummly_group_location_annual_gbp_v3";

        public static readonly IReadOnlyList<string> All =
        [
            StarterMonthly,
            StarterAnnual,
            GrowthMonthly,
            GrowthAnnual,
            GroupMonthly,
            GroupAnnual,
            GroupLocationMonthly,
            GroupLocationAnnual,
        ];

        /// <summary>
        /// Resolve recurring SKU lookup key from plan display name and API cadence
        /// (<c>monthly</c> / <c>annual</c>).
        /// </summary>
        public static string? ForPlanCadence(string plan, string cadenceApi)
        {
            var annual = IsAnnual(cadenceApi);
            if (IsPlan(plan, "Starter"))
            {
                return annual ? StarterAnnual : StarterMonthly;
            }

            if (IsPlan(plan, "Growth"))
            {
                return annual ? GrowthAnnual : GrowthMonthly;
            }

            if (IsPlan(plan, "Group"))
            {
                return annual ? GroupAnnual : GroupMonthly;
            }

            return null;
        }

        public static string ForExtraLocation(string cadenceApi)
        {
            return IsAnnual(cadenceApi)
                ? GroupLocationAnnual
                : GroupLocationMonthly;
        }

        private static bool IsAnnual(string cadenceApi)
        {
            return string.Equals(
                cadenceApi,
                "annual",
                StringComparison.OrdinalIgnoreCase
            );
        }

        private static bool IsPlan(string plan, string expected)
        {
            return string.Equals(
                plan,
                expected,
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}
