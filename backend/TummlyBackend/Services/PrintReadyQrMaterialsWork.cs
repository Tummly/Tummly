using System.Threading.Channels;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class PrintReadyQrMaterialsWork
        : IPrintReadyQrMaterialsWork
    {
        private readonly Channel<PrintMaterialsRequest> _requests =
            Channel.CreateUnbounded<PrintMaterialsRequest>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                }
            );

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<PrintReadyQrMaterialsWork> _logger;

        public PrintReadyQrMaterialsWork(
            IServiceScopeFactory scopeFactory,
            IHostEnvironment environment,
            ILogger<PrintReadyQrMaterialsWork> logger
        )
        {
            _scopeFactory = scopeFactory;
            _environment = environment;
            _logger = logger;
        }

        public ValueTask RequestEnsureAsync(
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            WriteRequest(new StarterMaterialsRequest(locationId));
            return ValueTask.CompletedTask;
        }

        public ValueTask RequestShopOrderEnsureAsync(
            Guid shopOrderId,
            CancellationToken cancellationToken = default
        )
        {
            WriteRequest(new ShopOrderMaterialsRequest(shopOrderId));
            return ValueTask.CompletedTask;
        }

        private void WriteRequest(PrintMaterialsRequest request)
        {
            if (!_requests.Writer.TryWrite(request))
            {
                throw new InvalidOperationException(
                    $"Print-ready QR materials request could not be queued for {request.ScopeId}."
                );
            }
        }

        public async Task RunAsync(CancellationToken stoppingToken)
        {
            if (_environment.IsEnvironment("Testing"))
            {
                return;
            }

            try
            {
                await foreach (
                    var request in _requests.Reader.ReadAllAsync(
                        stoppingToken
                    )
                )
                {
                    await EnsureGuardedAsync(request, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (
                stoppingToken.IsCancellationRequested
            )
            {
                // The host controls the bounded shutdown window.
            }
        }

        public async Task DrainAsync(
            CancellationToken cancellationToken = default
        )
        {
            while (_requests.Reader.TryRead(out var request))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await EnsureGuardedAsync(request, cancellationToken);
            }
        }

        private async Task EnsureGuardedAsync(
            PrintMaterialsRequest request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var materials = scope.ServiceProvider
                    .GetRequiredService<IPrintReadyQrMaterialsService>();
                await request.EnsureAsync(materials, cancellationToken);
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested
            )
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Print-ready QR materials generation failed for {ScopeId}",
                    request.ScopeId
                );
            }
        }

        private abstract record PrintMaterialsRequest
        {
            public abstract object ScopeId { get; }

            public abstract Task EnsureAsync(
                IPrintReadyQrMaterialsService materials,
                CancellationToken cancellationToken
            );
        }

        private sealed record StarterMaterialsRequest(int LocationId)
            : PrintMaterialsRequest
        {
            public override object ScopeId => LocationId;

            public override Task EnsureAsync(
                IPrintReadyQrMaterialsService materials,
                CancellationToken cancellationToken
            ) => materials.EnsureStarterMaterialsAsync(
                LocationId,
                cancellationToken
            );
        }

        private sealed record ShopOrderMaterialsRequest(Guid ShopOrderId)
            : PrintMaterialsRequest
        {
            public override object ScopeId => ShopOrderId;

            public override Task EnsureAsync(
                IPrintReadyQrMaterialsService materials,
                CancellationToken cancellationToken
            ) => materials.EnsureShopOrderMaterialsAsync(
                ShopOrderId,
                cancellationToken
            );
        }
    }
}
