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
            WriteRequest(
                PrintMaterialsRequest.ForStarter(locationId),
                "Starter QR materials",
                locationId
            );
            return ValueTask.CompletedTask;
        }

        public ValueTask RequestShopOrderEnsureAsync(
            Guid shopOrderId,
            CancellationToken cancellationToken = default
        )
        {
            WriteRequest(
                PrintMaterialsRequest.ForShopOrder(shopOrderId),
                "Shop print-ready QR materials",
                shopOrderId
            );
            return ValueTask.CompletedTask;
        }

        private void WriteRequest(
            PrintMaterialsRequest request,
            string requestName,
            object scopeId
        )
        {
            try
            {
                if (!_requests.Writer.TryWrite(request))
                {
                    _logger.LogWarning(
                        "{RequestName} request dropped for {ScopeId}",
                        requestName,
                        scopeId
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "{RequestName} request failed for {ScopeId}",
                    requestName,
                    scopeId
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
                if (request.LocationId is int locationId)
                {
                    await materials.EnsureStarterMaterialsAsync(
                        locationId,
                        cancellationToken
                    );
                }
                else
                {
                    await materials.EnsureShopOrderMaterialsAsync(
                        request.ShopOrderId!.Value,
                        cancellationToken
                    );
                }
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

        private sealed record PrintMaterialsRequest(
            int? LocationId,
            Guid? ShopOrderId
        )
        {
            public object ScopeId => LocationId is int locationId
                ? locationId
                : ShopOrderId!.Value;

            public static PrintMaterialsRequest ForStarter(int locationId) =>
                new(locationId, null);

            public static PrintMaterialsRequest ForShopOrder(Guid shopOrderId) =>
                new(null, shopOrderId);
        }
    }
}
