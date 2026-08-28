using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class BillingPlanSnapshotHelper
    {
        public const string SoftLockStatus = "Soft lock";
        public const string DormantStatus = "Dormant";
        public const string PilotStatus = "Pilot";
        public const string ActiveStatus = "Active";

        public sealed record BillingPlanLifecycle(
            string SubscriptionPlan,
            string BillingStatus,
            bool IsPilot
        );

        public static async Task<bool> IsPilotRestaurantAsync(
            ApplicationDbContext context,
            int restaurantId,
            User? owner
        )
        {
            var restaurant = await context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            return ResolveLifecycle(restaurant?.Name ?? "", owner?.ActivationExpiresAt)
                .IsPilot;
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
            var lifecycle = ResolveLifecycle(
                isPilot ? "Pilot Venue" : "Paid Growth Venue",
                isPilot ? DateTime.UtcNow.AddDays(30) : null
            );
            return (lifecycle.SubscriptionPlan, lifecycle.BillingStatus);
        }

        public static (string SubscriptionPlan, string BillingStatus) ResolveSnapshot(
            string restaurantName,
            DateTime? activationExpiresAt,
            DateTime? utcNow = null
        )
        {
            var lifecycle = ResolveLifecycle(restaurantName, activationExpiresAt, utcNow);
            return (lifecycle.SubscriptionPlan, lifecycle.BillingStatus);
        }

        /// <summary>
        /// Stub lifecycle until Billing Account Tick (credit-ledger 31) persists status.
        /// Name prefixes: Soft lock / Dormant (+ optional Paid). Else unpaid Pilot clocks.
        /// </summary>
        public static BillingPlanLifecycle ResolveLifecycle(
            string restaurantName,
            DateTime? activationExpiresAt,
            DateTime? utcNow = null
        )
        {
            var now = utcNow ?? DateTime.UtcNow;
            var name = restaurantName ?? "";

            if (StartsWithOrdinal(name, "Dormant Paid "))
            {
                return new BillingPlanLifecycle(
                    ResolvePaidPlanFromName(name),
                    DormantStatus,
                    IsPilot: false
                );
            }

            if (StartsWithOrdinal(name, "Soft lock Paid "))
            {
                return new BillingPlanLifecycle(
                    ResolvePaidPlanFromName(name),
                    SoftLockStatus,
                    IsPilot: false
                );
            }

            if (StartsWithOrdinal(name, "Dormant "))
            {
                return new BillingPlanLifecycle("Pilot", DormantStatus, IsPilot: true);
            }

            if (StartsWithOrdinal(name, "Soft lock "))
            {
                return new BillingPlanLifecycle("Pilot", SoftLockStatus, IsPilot: true);
            }

            if (StartsWithOrdinal(name, "Paid "))
            {
                return new BillingPlanLifecycle(
                    ResolvePaidPlanFromName(name),
                    ActiveStatus,
                    IsPilot: false
                );
            }

            if (activationExpiresAt == null)
            {
                return new BillingPlanLifecycle(
                    ResolvePaidSubscriptionPlan(),
                    ActiveStatus,
                    IsPilot: false
                );
            }

            if (activationExpiresAt.Value > now)
            {
                return new BillingPlanLifecycle("Pilot", PilotStatus, IsPilot: true);
            }

            if (activationExpiresAt.Value > now.AddDays(-15))
            {
                return new BillingPlanLifecycle("Pilot", SoftLockStatus, IsPilot: true);
            }

            return new BillingPlanLifecycle("Pilot", DormantStatus, IsPilot: true);
        }

        public static bool IsAccountLocked(string billingStatus)
        {
            return string.Equals(billingStatus, SoftLockStatus, StringComparison.Ordinal)
                || string.Equals(billingStatus, DormantStatus, StringComparison.Ordinal);
        }

        public static string? LockDenyCode(string billingStatus)
        {
            if (string.Equals(billingStatus, SoftLockStatus, StringComparison.Ordinal))
            {
                return "soft_lock";
            }

            if (string.Equals(billingStatus, DormantStatus, StringComparison.Ordinal))
            {
                return "dormant";
            }

            return null;
        }

        private static string ResolvePaidPlanFromName(string restaurantName)
        {
            if (restaurantName.Contains(" Group", StringComparison.Ordinal))
            {
                return "Group";
            }

            if (restaurantName.Contains(" Starter", StringComparison.Ordinal))
            {
                return "Starter";
            }

            return ResolvePaidSubscriptionPlan();
        }

        private static bool StartsWithOrdinal(string value, string prefix)
        {
            return value.StartsWith(prefix, StringComparison.Ordinal);
        }
    }
}
