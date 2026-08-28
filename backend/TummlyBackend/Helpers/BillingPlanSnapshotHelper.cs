using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class BillingPlanSnapshotHelper
    {
        public static async Task<bool> IsPilotRestaurantAsync(
            ApplicationDbContext context,
            int restaurantId,
            User? owner
        )
        {
            var restaurant = await context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (
                restaurant?.Name.StartsWith(
                    "Paid ",
                    StringComparison.Ordinal
                ) == true
            )
            {
                return false;
            }

            return owner?.ActivationExpiresAt != null
                && owner.ActivationExpiresAt.Value > DateTime.UtcNow;
        }

        public static string ResolvePaidSubscriptionPlan()
        {
            // Stub seam until BillingAccount persistence lands (credit-ledger map).
            return "Growth";
        }

        public static (string SubscriptionPlan, string BillingStatus) ResolveSnapshot(
            bool isPilot
        )
        {
            if (isPilot)
            {
                return ("Pilot", "Pilot");
            }

            return (ResolvePaidSubscriptionPlan(), "Active");
        }
    }
}
