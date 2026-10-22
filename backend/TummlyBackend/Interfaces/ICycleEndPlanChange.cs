namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Downgrade / cadence cycle-end Revolut <c>change-plan</c> adapter
    /// (ticket 21). Tummly Scheduled change stays on
    /// <see cref="IPlanChangeService"/>.
    /// </summary>
    public interface ICycleEndPlanChange
    {
        /// <summary>
        /// When a Revolut subscription is correlated for the restaurant, call
        /// <c>change-plan</c> with <c>at_cycle_end</c> for the mapped target
        /// variation. Missing map fails closed. No-op when no subscription id.
        /// </summary>
        Task ApplyRevolutChangePlanIfNeededAsync(
            int restaurantId,
            string targetPlan,
            string targetCadenceApi,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Clear of a scheduled change: call <c>change-plan</c> with
        /// <c>at_cycle_end</c> for the live plan variation (Group with paid
        /// extras maps to the Group Location variation). Missing map fails
        /// closed. No-op when no subscription id.
        /// </summary>
        Task RestoreLiveRevolutPlanIfNeededAsync(
            int restaurantId,
            string livePlan,
            string liveCadenceApi,
            int paidExtraLocationCount,
            CancellationToken cancellationToken = default
        );
    }
}
