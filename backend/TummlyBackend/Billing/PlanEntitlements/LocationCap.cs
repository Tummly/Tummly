using TummlyBackend.Billing.Pricebook;

namespace TummlyBackend.Billing.PlanEntitlements
{
    public static class LocationCap
    {
        public const int GroupSelfServeMax = 30;

        public const int CustomMax = 500;

        public const string CapReachedCode = "location_cap_reached";

        public static bool TryResolve(
            PricebookSnapshot book,
            string subscriptionPlan,
            int paidExtraLocationCount,
            out int entitled
        )
        {
            return TryResolve(
                book,
                subscriptionPlan,
                paidExtraLocationCount,
                customConfiguredLocations: null,
                out entitled
            );
        }

        public static bool TryResolve(
            PricebookSnapshot book,
            string subscriptionPlan,
            int paidExtraLocationCount,
            int? customConfiguredLocations,
            out int entitled
        )
        {
            entitled = 0;
            if (string.IsNullOrWhiteSpace(subscriptionPlan))
            {
                return false;
            }

            var key = subscriptionPlan.Trim().ToLowerInvariant();
            if (key == "custom")
            {
                var locs = Math.Max(0, customConfiguredLocations ?? 0);
                entitled = Math.Min(locs, CustomMax);
                return true;
            }

            if (!book.Plans.TryGetValue(key, out var plan))
            {
                return false;
            }

            var included = plan.IncludedLocations;
            if (included < 0)
            {
                return false;
            }

            if (included == 0)
            {
                entitled = 0;
                return true;
            }

            if (key == "group")
            {
                var extras = Math.Max(0, paidExtraLocationCount);
                entitled = Math.Min(included + extras, GroupSelfServeMax);
                return true;
            }

            entitled = included;
            return true;
        }
    }
}
