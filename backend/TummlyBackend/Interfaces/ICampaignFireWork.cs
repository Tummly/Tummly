namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Durable Campaign fire work — due Scheduled + Sending drain
    /// (ticket 31). Channel wake is best-effort; DrainAsync is the test surface.
    /// Partially sent is not auto-drained (operator Retry remaining — ticket 30).
    /// </summary>
    public interface ICampaignFireWork
    {
        /// <summary>
        /// Fire-and-forget wake after send-now commit or schedule due.
        /// Must not fail the commit path.
        /// </summary>
        ValueTask NotifyAsync(
            int campaignId,
            CancellationToken cancellationToken = default
        );

        Task RunAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Fire all claimable due Scheduled + Sending campaigns.
        /// Returns when idle. Does not auto-drain Partially sent
        /// (operator Retry remaining — ticket 30).
        /// </summary>
        Task DrainAsync(CancellationToken cancellationToken = default);
    }
}
