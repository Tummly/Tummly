using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

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

            await RecoverPendingRequestsGuardedAsync(stoppingToken);

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

        private async Task RecoverPendingRequestsGuardedAsync(
            CancellationToken cancellationToken
        )
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var catalog = scope.ServiceProvider
                    .GetRequiredService<IMaterialsCatalog>();

                var starterQrScopes = await context.QrCodes
                    .AsNoTracking()
                    .Where(row =>
                        (
                            row.QrType == QrType.TableTent
                            || row.QrType == QrType.WindowSticker
                            || row.QrType == QrType.OfferCard
                        )
                        && (
                            row.Status == QrCodeStatus.Active
                            || row.Status == QrCodeStatus.Paused
                        )
                    )
                    .Select(row => new
                    {
                        LocationId = row.RestaurantLocationId,
                        row.QrType,
                    })
                    .ToListAsync(cancellationToken);
                var starterAssets = await context.PrintReadyQrAssets
                    .AsNoTracking()
                    .Where(row => row.ShopOrderId == null)
                    .Select(row => new
                    {
                        LocationId = row.RestaurantLocationId,
                        row.QrType,
                        row.Status,
                    })
                    .ToListAsync(cancellationToken);
                var starterByScope = starterAssets.ToDictionary(
                    row => (row.LocationId, row.QrType)
                );

                foreach (
                    var locationId in starterQrScopes
                        .Where(scopeRow =>
                            !starterByScope.TryGetValue(
                                (scopeRow.LocationId, scopeRow.QrType),
                                out var asset
                            )
                            || asset.Status
                                == PrintReadyQrAssetStatus.Preparing
                        )
                        .Select(row => row.LocationId)
                        .Distinct()
                )
                {
                    WriteRequest(new StarterMaterialsRequest(locationId));
                }

                var paidOrders = await context.ShopOrders
                    .AsNoTracking()
                    .Include(row => row.Lines)
                    .Where(row =>
                        row.PaymentStatus == ShopPaymentStatuses.Paid
                        || row.PaymentStatus == ShopPaymentStatuses.Refunded
                    )
                    .ToListAsync(cancellationToken);
                var paidOrderIds = paidOrders.Select(row => row.Id).ToList();
                var shopAssets = await context.PrintReadyQrAssets
                    .AsNoTracking()
                    .Where(row =>
                        row.ShopOrderId != null
                        && paidOrderIds.Contains(row.ShopOrderId.Value)
                    )
                    .Select(row => new
                    {
                        ShopOrderId = row.ShopOrderId!.Value,
                        row.QrType,
                        row.Status,
                    })
                    .ToListAsync(cancellationToken);
                var shopByScope = shopAssets.ToDictionary(
                    row => (row.ShopOrderId, row.QrType)
                );

                foreach (var order in paidOrders)
                {
                    var orderedTypes =
                        PrintReadyQrMaterialsService.ResolveOrderedQrTypes(
                            order.Lines,
                            catalog
                        );
                    if (
                        orderedTypes.Keys.Any(qrType =>
                            !shopByScope.TryGetValue(
                                (order.Id, qrType),
                                out var asset
                            )
                            || asset.Status
                                == PrintReadyQrAssetStatus.Preparing
                        )
                    )
                    {
                        WriteRequest(
                            new ShopOrderMaterialsRequest(order.Id)
                        );
                    }
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
                    "Could not recover pending print-ready QR material requests"
                );
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
