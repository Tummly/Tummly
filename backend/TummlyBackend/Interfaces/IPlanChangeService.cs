using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IPlanChangeService
    {
        /// <summary>
        /// Same-cadence paid upgrade after Revolut ORDER_COMPLETED.
        /// Applies plan + Contracted = Current, clears the slot, then
        /// <c>plan_migration</c> floor grants on the open Included period.
        /// </summary>
        Task ApplyImmediateSameCadenceUpgradeAsync(
            int restaurantId,
            string targetPlan,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Non-cancel Scheduled change on renewal pay, before included mint.
        /// Mutates the locked <paramref name="billingAccount"/> in the caller transaction.
        /// </summary>
        Task<ScheduledChangeApplyResult> ApplyScheduledChangeOnRenewalAsync(
            BillingAccount billingAccount,
            CancellationToken cancellationToken = default
        );

        bool HasScheduledChange(BillingAccount billingAccount);

        void ClearScheduledChange(BillingAccount billingAccount);

        void SetScheduledChange(
            BillingAccount billingAccount,
            string targetPlan,
            string targetBillingCycle,
            int targetPaidExtraLocationCount
        );

        string FormatScheduledChangeLine(
            BillingAccount billingAccount,
            string renewalDateLabel
        );

        Task EnsureEntitlementGateAsync(
            string targetPlan,
            int targetPaidExtraLocationCount,
            int restaurantId,
            CancellationToken cancellationToken = default
        );
    }

    public enum ScheduledChangeApplyResult
    {
        Empty,
        Applied,
        GateFailed,
        CancelDeferred,
    }
}
