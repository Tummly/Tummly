using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Free (no-plan) gate for live operator writes (WS3b).
    /// A Free account has no Pilot or paid plan and cannot run live writes.
    /// An account that is missing entirely is not gated here (matches the lock gate).
    /// </summary>
    public static class OperatorPlanGate
    {
        public const string PlanRequiredCode = "plan_required";

        public const string PlanRequiredMessage =
            "Choose a Pilot or paid plan to continue.";

        /// <summary>
        /// True only when the account has a Pilot or a paid plan. An unpaid Custom
        /// signup has plan Custom but status Free until payment, so it has no plan yet.
        /// </summary>
        public static bool HasPlan(BillingAccount account)
        {
            return account.SubscriptionPlan != BillingSubscriptionPlans.Free
                && account.BillingStatus != BillingStatuses.Free;
        }

        public static string? EvaluatePlanDeny(BillingAccount account)
        {
            return HasPlan(account) ? null : PlanRequiredCode;
        }

        /// <summary>Operator-facing message for a gate code (plan copy or the code itself).</summary>
        public static string MessageFor(string code)
        {
            return code == PlanRequiredCode ? PlanRequiredMessage : code;
        }

        public static async Task EnsurePlanAndPaidWriteForLocationAsync(
            ApplicationDbContext context,
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == locationId)
                .Select(row => row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (restaurantId == 0)
            {
                return;
            }

            var account = await context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (account != null && EvaluatePlanDeny(account) is string planDeny)
            {
                throw new OperatorBillingLockedException(planDeny);
            }

            await OperatorBillingLockGate.EnsurePaidWriteAllowedForLocationAsync(
                context,
                locationId,
                cancellationToken
            );
        }
    }
}
