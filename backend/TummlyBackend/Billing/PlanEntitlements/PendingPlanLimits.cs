using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Billing.PlanEntitlements
{
    /// <summary>
    /// Location and team limits while a downgrade (or Group extra remove) is
    /// scheduled. The live plan and the scheduled target both apply until
    /// renewal; the lower limit wins so the renewal entitlement gate cannot
    /// fail on usage added after the change was scheduled.
    /// </summary>
    public static class PendingPlanLimits
    {
        public const int ExtraUsersPerLocation = 2;

        /// <summary>
        /// Active locations use a plan slot. Downgrade and activation gates
        /// count these only.
        /// </summary>
        public static Task<int> CountActiveLocationsAsync(
            ApplicationDbContext context,
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            return context.RestaurantLocations
                .AsNoTracking()
                .CountAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && row.LifecycleStatus == LocationLifecycleStatus.Active,
                    cancellationToken
                );
        }

        /// <summary>
        /// Draft + Active rows block a new insert. Paused / Archived rows do
        /// not, so a location archived for a downgrade frees its slot.
        /// </summary>
        public static Task<int> CountInsertLocationsAsync(
            ApplicationDbContext context,
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            return context.RestaurantLocations
                .AsNoTracking()
                .CountAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && (
                            row.LifecycleStatus == LocationLifecycleStatus.Active
                            || row.LifecycleStatus == LocationLifecycleStatus.Draft
                        ),
                    cancellationToken
                );
        }

        /// <summary>
        /// Location limit of the live plan (Custom reads its configured
        /// locations).
        /// </summary>
        public static bool TryResolveLiveLocationCap(
            IPricebookCatalog pricebook,
            BillingAccount account,
            out int entitled
        )
        {
            entitled = 0;
            try
            {
                var book = pricebook.GetRequired(account.ContractedPricebookId);
                int? customLocs = null;
                if (
                    string.Equals(
                        account.SubscriptionPlan,
                        BillingSubscriptionPlans.Custom,
                        StringComparison.Ordinal
                    )
                )
                {
                    var custom = CustomPlan.CustomPlanConfig.TryParse(
                        account.CustomPlanConfigJson
                    );
                    customLocs = custom?.Quantities.Locations;
                }

                return LocationCap.TryResolve(
                    book,
                    account.SubscriptionPlan,
                    account.PaidExtraLocationCount,
                    customLocs,
                    out entitled
                );
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Deny message when a Draft activate or Paused resume would take
        /// Active locations past the plan limit; null when allowed. Free and
        /// Pilot keep their old behaviour (no activation gate).
        /// </summary>
        public static async Task<string?> DenyActivationAsync(
            ApplicationDbContext context,
            IPricebookCatalog pricebook,
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            var account = await context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (account == null || !IsPaidPlan(account.SubscriptionPlan))
            {
                return null;
            }

            if (!TryResolveLiveLocationCap(pricebook, account, out var entitled))
            {
                return null;
            }

            var scheduledCap = ScheduledLocationCap(pricebook, account);
            var scheduledLimits = scheduledCap != null && scheduledCap < entitled;
            if (scheduledLimits)
            {
                entitled = scheduledCap!.Value;
            }

            var active = await CountActiveLocationsAsync(
                context,
                restaurantId,
                cancellationToken
            );
            if (active < entitled)
            {
                return null;
            }

            return scheduledLimits
                ? LocationCapMessage(account, entitled)
                : $"Location cap reached ({active} of {entitled}). "
                    + "Pause a location or upgrade your plan first.";
        }

        private static bool IsPaidPlan(string? subscriptionPlan)
        {
            return subscriptionPlan is BillingSubscriptionPlans.Starter
                or BillingSubscriptionPlans.Growth
                or BillingSubscriptionPlans.Group
                or BillingSubscriptionPlans.Custom;
        }

        public static bool TryResolveTeamCap(
            PricebookSnapshot book,
            string subscriptionPlan,
            int paidExtraLocationCount,
            out int cap
        )
        {
            cap = 0;
            var key = subscriptionPlan.Trim().ToLowerInvariant();
            if (!book.Plans.TryGetValue(key, out var plan))
            {
                return false;
            }

            var extras = string.Equals(key, "group", StringComparison.Ordinal)
                ? Math.Max(0, paidExtraLocationCount)
                : 0;
            cap = plan.IncludedTeamMembers + (extras * ExtraUsersPerLocation);
            return true;
        }

        /// <summary>
        /// Location limit of the scheduled target, or null when no plan
        /// change is pending (cancel is handled by the cancel flow).
        /// </summary>
        public static int? ScheduledLocationCap(
            IPricebookCatalog pricebook,
            BillingAccount account
        )
        {
            if (!TryResolveScheduledTarget(pricebook, account, out var book, out var plan, out var extras))
            {
                return null;
            }

            return LocationCap.TryResolve(book, plan, extras, out var entitled)
                ? entitled
                : null;
        }

        public static int? ScheduledTeamCap(
            IPricebookCatalog pricebook,
            BillingAccount account
        )
        {
            if (!TryResolveScheduledTarget(pricebook, account, out var book, out var plan, out var extras))
            {
                return null;
            }

            return TryResolveTeamCap(book, plan, extras, out var cap) ? cap : null;
        }

        public static string LocationCapMessage(BillingAccount account, int cap)
        {
            return $"{ScheduledChangePrefix(account)} allows {cap} active "
                + $"{(cap == 1 ? "location" : "locations")}. "
                + "Cancel the scheduled change to add more.";
        }

        public static string TeamCapMessage(BillingAccount account, int cap)
        {
            return $"{ScheduledChangePrefix(account)} allows {cap} team "
                + $"{(cap == 1 ? "member" : "members")}. "
                + "Cancel the scheduled change to add more.";
        }

        private static string ScheduledChangePrefix(BillingAccount account)
        {
            return account.RenewalDateUtc == null
                ? "Your scheduled plan change"
                : $"Your plan change on {UkDateLabels.Format(account.RenewalDateUtc.Value)}";
        }

        private static bool TryResolveScheduledTarget(
            IPricebookCatalog pricebook,
            BillingAccount account,
            out PricebookSnapshot book,
            out string plan,
            out int extras
        )
        {
            book = null!;
            plan = string.Empty;
            extras = 0;
            if (!account.HasScheduledChange || account.ScheduledCancelPlan)
            {
                return false;
            }

            plan = account.ScheduledTargetSubscriptionPlan ?? account.SubscriptionPlan;
            if (string.IsNullOrWhiteSpace(plan))
            {
                return false;
            }

            // Mirrors PlanChangeService.ResolveScheduledExtraCount.
            extras = string.Equals(
                plan.Trim(),
                BillingSubscriptionPlans.Group,
                StringComparison.OrdinalIgnoreCase
            )
                ? Math.Max(0, account.ScheduledTargetExtraLocationCount ?? 0)
                : 0;

            try
            {
                // Renewal applies the target on the current pricebook.
                book = pricebook.GetRequired(pricebook.CurrentPricebookId);
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            return true;
        }
    }
}
