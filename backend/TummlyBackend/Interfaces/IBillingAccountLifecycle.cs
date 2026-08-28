namespace TummlyBackend.Interfaces
{
    public interface IBillingAccountLifecycle
    {
        Task TickAsync(
            int restaurantId,
            DateTime now,
            CancellationToken cancellationToken = default
        );

        Task<BillingLifecycleCommandResult> StartDunningEpisodeAsync(
            int restaurantId,
            DateTime now,
            CancellationToken cancellationToken = default
        );

        Task RecoverDunningAsync(
            int restaurantId,
            DateTime now,
            CancellationToken cancellationToken = default
        );

        Task ActivatePaidPlanAsync(
            int restaurantId,
            DateTime now,
            CancellationToken cancellationToken = default
        );

        Task<BillingLifecycleCommandResult> ExtendPilotActivationAsync(
            int restaurantId,
            DateTime newPeriodEnd,
            DateTime now,
            CancellationToken cancellationToken = default
        );

        Task SetChargebackRestrictionAsync(
            int restaurantId,
            bool restricted,
            CancellationToken cancellationToken = default
        );
    }

    public sealed record BillingLifecycleCommandResult(
        bool Applied,
        bool Refused,
        string? Reason
    )
    {
        public static BillingLifecycleCommandResult Ok() =>
            new(true, false, null);

        public static BillingLifecycleCommandResult NoOp() =>
            new(false, false, null);

        public static BillingLifecycleCommandResult Refuse(string reason) =>
            new(false, true, reason);
    }
}
