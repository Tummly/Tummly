using TummlyBackend.Interfaces;

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

        public Task<BillingLifecycleCommandResult> ExtendPilotActivationAsync(
            int restaurantId,
            DateTime newPeriodEnd,
            DateTime now,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(BillingLifecycleCommandResult.NoOp());

        public Task SetChargebackRestrictionAsync(
            int restaurantId,
            bool restricted,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;
    }
}
