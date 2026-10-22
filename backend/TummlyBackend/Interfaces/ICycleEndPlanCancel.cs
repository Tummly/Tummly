namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Cycle-end Revolut cancel adapter (ticket 23). Tummly scheduled cancel
    /// stays on <see cref="IPlanChangeService"/> / BillingAccount slot.
    /// </summary>
    public interface ICycleEndPlanCancel
    {
        /// <summary>
        /// When a Revolut subscription is correlated for the restaurant, call
        /// <c>PATCH …/subscriptions/{id}</c> with a scheduled cancel at cycle
        /// end. No-op when no subscription id.
        /// </summary>
        Task ApplyRevolutCancelAtCycleEndIfNeededAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        );
    }
}
