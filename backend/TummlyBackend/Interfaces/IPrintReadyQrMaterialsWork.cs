namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Application-owned queue for Starter QR materials generation.
    /// Requests return after enqueue. Processing creates a separate service
    /// scope and does not use a request DbContext.
    /// </summary>
    public interface IPrintReadyQrMaterialsWork
    {
        ValueTask RequestEnsureAsync(
            int locationId,
            CancellationToken cancellationToken = default
        );

        Task RunAsync(CancellationToken stoppingToken);

        Task DrainAsync(CancellationToken cancellationToken = default);
    }
}
