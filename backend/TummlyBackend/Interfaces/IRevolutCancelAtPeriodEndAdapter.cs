namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Revolut native cancel at Tummly period end (ticket 23). Confirm day
    /// only records cancel-at-period-end locally — do not call this then.
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
