namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Durable <c>Guest response email delivery</c> work (ADR-0026).
    /// Pending rows on <c>FeedbackGuestResponse</c> are the queue; Channel wake
    /// is best-effort only. Lifecycle: Pending → Accepted (retry until accepted).
    /// </summary>
    public interface IGuestResponseEmailDeliveryWork
    {
        /// <summary>
        /// Fire-and-forget wake after an email-channel guest-response fact is
        /// saved as Pending. Must not fail Confirm Send.
        /// </summary>
        ValueTask NotifyAsync(
            int guestResponseId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Process-lifetime loop: startup sweep, Channel wakes, periodic reclaim.
        /// Returns immediately in the Testing environment.
        /// </summary>
        Task RunAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Advance claimable Pending delivery rows toward Accepted.
        /// Returns when idle. Primary test surface when the hosted loop is off.
        /// </summary>
        Task DrainAsync(CancellationToken cancellationToken = default);
    }
}
