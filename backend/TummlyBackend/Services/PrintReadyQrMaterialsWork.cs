using System.Threading.Channels;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class PrintReadyQrMaterialsWork
        : IPrintReadyQrMaterialsWork
    {
        private readonly Channel<int> _requests =
            Channel.CreateUnbounded<int>(
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
            try
            {
                if (!_requests.Writer.TryWrite(locationId))
                {
                    _logger.LogWarning(
                        "Starter QR materials request dropped for Owned location {LocationId}",
                        locationId
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Starter QR materials request failed for Owned location {LocationId}",
                    locationId
                );
            }

            return ValueTask.CompletedTask;
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
                    var locationId in _requests.Reader.ReadAllAsync(
                        stoppingToken
                    )
                )
                {
                    await EnsureGuardedAsync(locationId, stoppingToken);
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
            while (_requests.Reader.TryRead(out var locationId))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await EnsureGuardedAsync(locationId, cancellationToken);
            }
        }

        private async Task EnsureGuardedAsync(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var materials = scope.ServiceProvider
                    .GetRequiredService<IPrintReadyQrMaterialsService>();
                await materials.EnsureStarterMaterialsAsync(
                    locationId,
                    cancellationToken
                );
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
                    "Starter QR materials generation failed for Owned location {LocationId}",
                    locationId
                );
            }
        }
    }
}
