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

        ValueTask RequestShopOrderEnsureAsync(
            Guid shopOrderId,
            CancellationToken cancellationToken = default
        );

        Task RunAsync(CancellationToken stoppingToken);

        Task DrainAsync(CancellationToken cancellationToken = default);
    }

    public sealed class NoOpPrintReadyQrMaterialsWork : IPrintReadyQrMaterialsWork
    {
        public static readonly NoOpPrintReadyQrMaterialsWork Instance = new();

        private NoOpPrintReadyQrMaterialsWork() { }

        public ValueTask RequestEnsureAsync(
            int locationId,
            CancellationToken cancellationToken = default
        ) => ValueTask.CompletedTask;

        public ValueTask RequestShopOrderEnsureAsync(
            Guid shopOrderId,
            CancellationToken cancellationToken = default
        ) => ValueTask.CompletedTask;

        public Task RunAsync(CancellationToken stoppingToken) =>
            Task.CompletedTask;

        public Task DrainAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
