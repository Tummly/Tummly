namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Best-effort merchant Pay on day-step 0 / 3 (lock 08 / ticket 24).
    /// Does not write lifecycle day steps or Soft lock / Dormant.
    /// </summary>
    public interface IRevolutDunningPayAdapter
    {
        Task HandleDayStepAsync(
            int restaurantId,
            int dayStep,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Immediate best-effort Pay on the stored outstanding cycle order
        /// (e.g. after Update payment method). Recover still waits for
        /// <c>ORDER_COMPLETED</c>.
        /// </summary>
        Task TryPayOutstandingAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        );
    }
}
