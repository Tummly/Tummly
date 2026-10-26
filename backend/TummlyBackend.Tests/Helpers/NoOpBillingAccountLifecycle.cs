using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// No-op lifecycle for tests that construct <c>SmartGuestLinkService</c>
    /// without billing clocks.
    /// </summary>
    public sealed class NoOpBillingAccountLifecycle : IBillingAccountLifecycle
    {
        public Task TickAsync(
            int restaurantId,
            DateTime now,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;

        public Task<BillingLifecycleCommandResult> StartDunningEpisodeAsync(
            int restaurantId,
            DateTime now,
            string? outstandingOrderId = null,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(BillingLifecycleCommandResult.NoOp());

        public Task RecoverDunningAsync(
            int restaurantId,
            DateTime now,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;

        public Task ActivatePaidPlanAsync(
            int restaurantId,
            DateTime now,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;

        public Task<PilotExtensionResult> ExtendPilotAsync(
            int restaurantId,
            int days,
            string? idempotencyKey,
            DateTime now,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(new PilotExtensionResult("refused", "pilot_extension_not_allowed", null, null, 0, 0));

        public Task SetChargebackRestrictionAsync(
            int restaurantId,
            bool restricted,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;

        public BillingLifecycleCommandResult ApplyPostCancelSoftLock(
            BillingAccount billingAccount,
            DateTime renewalEndUtc
        ) => BillingLifecycleCommandResult.NoOp();
    }
}
