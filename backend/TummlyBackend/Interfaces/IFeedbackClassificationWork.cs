namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Deep module for async Feedback AI classification work (ADR-0010).
    /// Pending rows are the durable queue; Channel wake is best-effort only.
    /// Product lifecycle stays Pending → Succeeded | Failed.
    /// </summary>
    public interface IFeedbackClassificationWork
    {
        /// <summary>
        /// Fire-and-forget wake after Feedback is persisted as Pending.
        /// Must not fail guest submit; correctness does not depend on delivery.
        /// </summary>
        ValueTask NotifyAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Process-lifetime loop: startup sweep, Channel wakes, ~30s reclaim.
        /// Returns immediately in the Testing environment.
        /// </summary>
        Task RunAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Advance all currently claimable Pending work to a terminal state
        /// using the same claim/lease/attempt path as <see cref="RunAsync"/>.
        /// Returns when idle. Primary test surface when the hosted loop is off.
        /// </summary>
        Task DrainAsync(CancellationToken cancellationToken = default);
    }
}
