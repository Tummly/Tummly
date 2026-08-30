using TummlyBackend.Billing.Pricebook;

namespace TummlyBackend.Billing.PlanEntitlements
{
    public static class LocationCap
    {
        public const int GroupSelfServeMax = 30;

        public const string CapReachedCode = "location_cap_reached";

        public static bool TryResolve(
            PricebookSnapshot book,
            string subscriptionPlan,
            int paidExtraLocationCount,
            out int entitled
        )
        {
            entitled = 0;
            if (string.IsNullOrWhiteSpace(subscriptionPlan))
            {
                return false;
            }

            var key = subscriptionPlan.Trim().ToLowerInvariant();
            if (!book.Plans.TryGetValue(key, out var plan))
            {
                return false;
            }

            var included = plan.IncludedLocations;
            if (included < 1)
            {
                return false;
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
