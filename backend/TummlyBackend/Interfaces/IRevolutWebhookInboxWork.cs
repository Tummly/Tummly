namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Processes the durable Revolut webhook inbox. Database rows are the
    /// queue; notifications only reduce normal processing latency.
    /// </summary>
    public interface IRevolutWebhookInboxWork
    {
        ValueTask NotifyAsync(
            Guid inboxItemId,
            CancellationToken cancellationToken = default
        );

        Task RunAsync(CancellationToken stoppingToken);

        Task DrainAsync(CancellationToken cancellationToken = default);
    }
}
