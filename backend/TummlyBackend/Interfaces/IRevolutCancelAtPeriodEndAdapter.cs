namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Revolut native cancel helpers (ticket 23). Confirm day uses
    /// <see cref="ICycleEndPlanCancel"/>; this adapter remains for explicit
    /// immediate cancel when needed.
    /// </summary>
    public interface IRevolutCancelAtPeriodEndAdapter
    {
        /// <summary>
        /// Cancels the live Revolut subscription for the restaurant (native
        /// immediate API). Resolves subscription id from pay-session
        /// correlation. No-op when no subscription id is stored.
        /// </summary>
        Task CancelNativeSubscriptionAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        );
    }
}
