using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.PrintReadyQrMaterials;

namespace TummlyBackend.Services
{
    public sealed class PrintReadyQrMaterialsService : IPrintReadyQrMaterialsService
    {
        private static readonly ConcurrentDictionary<
            (int LocationId, QrType QrType, Guid? ShopOrderId),
            SemaphoreSlim
        > EnsureLocks = new();
        private const int AutomaticGenerationAttempts = 2;

        private readonly ApplicationDbContext _context;
        private readonly PrintTemplatePack _pack;
        private readonly IMaterialsCatalog _materialsCatalog;
        private readonly IQueryAttachmentStorage _storage;
        private readonly IQrCodeRasterizer _rasterizer;
        private readonly ISmartGuestLinkService _guestLinks;
        private readonly IQrCodeProvisioningService _qrCodeProvisioning;
        private readonly IPrintReadyQrMaterialsWork _work;
        private readonly IAdminAuditService _audit;
        private readonly ILogger<PrintReadyQrMaterialsService> _logger;

        public PrintReadyQrMaterialsService(
            ApplicationDbContext context,
            PrintTemplatePack pack,
            IMaterialsCatalog materialsCatalog,
            IQueryAttachmentStorage storage,
            IQrCodeRasterizer rasterizer,
            ISmartGuestLinkService guestLinks,
            IQrCodeProvisioningService qrCodeProvisioning,
            IPrintReadyQrMaterialsWork work,
            IAdminAuditService audit,
            ILogger<PrintReadyQrMaterialsService> logger
        )
        {
            _context = context;
            _pack = pack;
            _materialsCatalog = materialsCatalog;
            _storage = storage;
            _rasterizer = rasterizer;
            _guestLinks = guestLinks;
            _qrCodeProvisioning = qrCodeProvisioning;
            _work = work;
            _audit = audit;
            _logger = logger;
        }

        public async Task EnsureStarterMaterialsAsync(
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var location = await _context.RestaurantLocations
                .Include(row => row.Restaurant)
                .FirstOrDefaultAsync(
                    row => row.Id == locationId,
                    cancellationToken
                );

            if (location?.Restaurant == null)
            {
                return;
            }

            // Mint missing Capture placements for the current starter-kit set
            // before print-ready ensure (idempotent).
            await _qrCodeProvisioning.MintDefaultQrCodesAsync(location);
            await _context.SaveChangesAsync(cancellationToken);

            var ensureTypes = (
                await StarterQrMaterialTypes.LoadAsync(
                    _context,
                    cancellationToken
                )
            ).ToHashSet();

            // Also regenerate print assets for existing physical placements
            // (legacy Tent/Window, shop-ordered types after rotate, etc.).
            var existingTypes = await _context.QrCodes
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantLocationId == locationId
                    && (
                        row.Status == QrCodeStatus.Active
                        || row.Status == QrCodeStatus.Paused
                    )
                )
                .Select(row => row.QrType)
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (
                var qrType in existingTypes.Where(
                    PhysicalShopPrintQrTypes.Contains
                )
            )
            {
                ensureTypes.Add(qrType);
            }

            foreach (var qrType in ensureTypes.OrderBy(row => row))
            {
                await EnsureOneAsync(
                    location,
                    qrType,
                    shopOrderId: null,
                    cancellationToken
                );
            }
        }

        public async Task EnsureShopOrderMaterialsAsync(
            Guid shopOrderId,
            CancellationToken cancellationToken = default
        )
        {
            var order = await _context.ShopOrders
                .AsNoTracking()
                .Include(row => row.Location)
                    .ThenInclude(row => row.Restaurant)
                .Include(row => row.Lines)
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == shopOrderId
                        && (
                            row.PaymentStatus == ShopPaymentStatuses.Paid
                            || row.PaymentStatus == ShopPaymentStatuses.Refunded
                        ),
                    cancellationToken
                );

            if (order?.Location?.Restaurant == null)
            {
                return;
            }

            foreach (
                var qrType in ResolveOrderedQrTypes(
                    order.Lines,
                    _materialsCatalog
                ).Keys
            )
            {
                await EnsureOneAsync(
                    order.Location,
                    qrType,
                    shopOrderId,
                    cancellationToken
                );
            }
        }

        public async Task<bool> TryInvalidateAfterQrRotationAsync(
            int locationId,
            QrType qrType,
            CancellationToken cancellationToken = default
        )
        {
            if (!PhysicalShopPrintQrTypes.Contains(qrType))
            {
                return false;
            }

            var assets = await _context.PrintReadyQrAssets
                .Where(row =>
                    row.RestaurantLocationId == locationId
                    && row.QrType == qrType
                )
                .ToListAsync(cancellationToken);
            foreach (var asset in assets)
            {
                asset.Status = PrintReadyQrAssetStatus.Preparing;
                asset.StorageKey = null;
                asset.FileName = null;
                asset.QrTokenFingerprint = null;
                asset.TemplatePackVersion = null;
                asset.OfferCopyVersion = null;
                asset.ApprovedAtUtc = null;
                asset.ApprovedByAdminUserId = null;
                asset.LastError = null;
                asset.UpdatedAtUtc = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync(cancellationToken);

            await QueueRegenerationAsync(
                () => _work.RequestEnsureAsync(locationId, cancellationToken),
                assets.Where(asset => asset.ShopOrderId == null),
                $"Owned location {locationId}",
                cancellationToken
            );
            foreach (
                var shopOrderId in assets
                    .Where(asset => asset.ShopOrderId.HasValue)
                    .Select(asset => asset.ShopOrderId!.Value)
                    .Distinct()
            )
            {
                await QueueRegenerationAsync(
                    () => _work.RequestShopOrderEnsureAsync(
                        shopOrderId,
                        cancellationToken
                    ),
                    assets.Where(asset => asset.ShopOrderId == shopOrderId),
                    $"Shop order {shopOrderId}",
                    cancellationToken
                );
            }

            return true;
        }

        private async Task QueueRegenerationAsync(
            Func<ValueTask> request,
            IEnumerable<PrintReadyQrAsset> affectedAssets,
            string scope,
            CancellationToken cancellationToken
        )
        {
            try
            {
                await request();
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(
                    ex,
                    "Could not queue print-ready QR material regeneration for {Scope}",
                    scope
                );
                foreach (var asset in affectedAssets)
                {
                    asset.Status = PrintReadyQrAssetStatus.Failed;
                    asset.LastError = TruncateError(
                        $"Could not queue regeneration: {ex.Message}"
                    );
                    asset.UpdatedAtUtc = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task EnsureAllStarterMaterialsForOperatorAsync(
            int operatorUserId,
            CancellationToken cancellationToken = default
        )
        {
            var locationIds = await OwnedLocationIdsAsync(
                operatorUserId,
                cancellationToken
            );
            foreach (var locationId in locationIds)
            {
                await EnsureStarterMaterialsAsync(locationId, cancellationToken);
            }
        }

        public async Task<IReadOnlyList<PrintMaterialsLocationReadinessDto>>
            ListReadinessAsync(
                int operatorUserId,
                CancellationToken cancellationToken = default
            )
        {
            var locations = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Restaurant!.OwnerUserId == operatorUserId)
                .OrderBy(row => row.CreatedAt)
                .Select(row => new { row.Id, row.LocationName })
                .ToListAsync(cancellationToken);

            var locationIds = locations.Select(row => row.Id).ToList();
            var starterTypes = await StarterQrMaterialTypes.LoadAsync(
                _context,
                cancellationToken
            );
            var assets = await _context.PrintReadyQrAssets
                .AsNoTracking()
                .Where(row =>
                    locationIds.Contains(row.RestaurantLocationId)
                    && row.ShopOrderId == null
                    && starterTypes.Contains(row.QrType)
                )
                .ToListAsync(cancellationToken);

            var byLocation = assets
                .GroupBy(row => row.RestaurantLocationId)
                .ToDictionary(group => group.Key, group => group.ToList());

            return locations
                .Select(location =>
                {
                    byLocation.TryGetValue(location.Id, out var rows);
                    rows ??= [];
                    return new PrintMaterialsLocationReadinessDto
                    {
                        LocationId = location.Id,
                        LocationName = location.LocationName,
                        Assets = starterTypes
                            .Select(qrType =>
                            {
                                var match = rows.FirstOrDefault(row =>
                                    row.QrType == qrType
                                );
                                return new PrintMaterialsAssetReadinessDto
                                {
                                    QrType = qrType.ToString(),
                                    Status = match is null
                                        ? PrintReadyQrAssetStatus.Preparing.ToString()
                                        : match.Status.ToString(),
                                    FileName = match?.FileName,
                                    LastError = match?.LastError,
                                };
                            })
                            .ToList(),
                    };
                })
                .ToList();
        }

        public async Task<IReadOnlyList<ShopPrintAssetReadinessDto>>
            ListShopOrderReadinessAsync(
                Guid shopOrderId,
                CancellationToken cancellationToken = default
            )
        {
            var order = await _context.ShopOrders
                .AsNoTracking()
                .Include(row => row.Lines)
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == shopOrderId
                        && (
                            row.PaymentStatus == ShopPaymentStatuses.Paid
                            || row.PaymentStatus == ShopPaymentStatuses.Refunded
                        ),
                    cancellationToken
                );
            if (order == null)
            {
                return Array.Empty<ShopPrintAssetReadinessDto>();
            }

            var orderedTypes = ResolveOrderedQrTypes(
                order.Lines,
                _materialsCatalog
            );
            var assets = await _context.PrintReadyQrAssets
                .AsNoTracking()
                .Where(row => row.ShopOrderId == shopOrderId)
                .ToListAsync(cancellationToken);

            return orderedTypes
                .Select(pair =>
                {
                    var asset = assets.FirstOrDefault(row =>
                        row.QrType == pair.Key
                    );
                    return MapShopReadiness(
                        pair.Key,
                        pair.Value,
                        asset,
                        shopOrderId
                    );
                })
                .ToList();
        }

        public async Task<PrintReadyQrDownload?> DownloadAsync(
            int operatorUserId,
            int locationId,
            QrType qrType,
            CancellationToken cancellationToken = default
        )
        {
            if (!PhysicalShopPrintQrTypes.Contains(qrType))
            {
                return null;
            }

            var owned = await IsOwnedLocationAsync(
                operatorUserId,
                locationId,
                cancellationToken
            );
            if (!owned)
            {
                return null;
            }

            var asset = await _context.PrintReadyQrAssets
                .FirstOrDefaultAsync(
                    row =>
                        row.RestaurantLocationId == locationId
                        && row.QrType == qrType
                        && row.ShopOrderId == null,
                    cancellationToken
                );

            if (asset == null)
            {
                return null;
            }

            if (asset.Status != PrintReadyQrAssetStatus.Ready
                || string.IsNullOrWhiteSpace(asset.StorageKey)
                || string.IsNullOrWhiteSpace(asset.FileName))
            {
                throw new PrintReadyQrNotReadyException(asset.Status);
            }

            return new PrintReadyQrDownload(
                await ReadStoredBytesAsync(asset, cancellationToken),
                asset.ContentType,
                BuildDistinctFileName(asset.FileName, qrType)
            );
        }

        public async Task<PrintReadyQrDownload?> DownloadShopOrderAsync(
            Guid shopOrderId,
            QrType qrType,
            CancellationToken cancellationToken = default
        )
        {
            if (!PhysicalShopPrintQrTypes.Contains(qrType))
            {
                return null;
            }

            var order = await _context.ShopOrders
                .AsNoTracking()
                .Include(row => row.Lines)
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == shopOrderId
                        && (
                            row.PaymentStatus == ShopPaymentStatuses.Paid
                            || row.PaymentStatus == ShopPaymentStatuses.Refunded
                        ),
                    cancellationToken
                );
            if (
                order == null
                || !ResolveOrderedQrTypes(
                    order.Lines,
                    _materialsCatalog
                ).ContainsKey(qrType)
            )
            {
                return null;
            }

            var asset = await _context.PrintReadyQrAssets
                .FirstOrDefaultAsync(
                    row =>
                        row.ShopOrderId == shopOrderId
                        && row.QrType == qrType,
                    cancellationToken
                );
            if (asset == null)
            {
                return null;
            }

            if (
                asset.Status != PrintReadyQrAssetStatus.Ready
                || string.IsNullOrWhiteSpace(asset.StorageKey)
                || string.IsNullOrWhiteSpace(asset.FileName)
            )
            {
                throw new PrintReadyQrNotReadyException(asset.Status);
            }

            if (asset.ApprovedAtUtc == null)
            {
                throw new PrintReadyQrNotApprovedException();
            }

            return new PrintReadyQrDownload(
                await ReadStoredBytesAsync(asset, cancellationToken),
                asset.ContentType,
                BuildDistinctFileName(asset.FileName, qrType)
            );
        }

        public async Task<PrintMaterialsAssetReadinessDto?> RetryAsync(
            int operatorUserId,
            int locationId,
            QrType qrType,
            CancellationToken cancellationToken = default
        )
        {
            if (!PhysicalShopPrintQrTypes.Contains(qrType))
            {
                return null;
            }

            var location = await _context.RestaurantLocations
                .Include(row => row.Restaurant)
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == locationId
                        && row.Restaurant!.OwnerUserId == operatorUserId,
                    cancellationToken
                );

            if (location?.Restaurant == null)
            {
                return null;
            }

            var asset = await _context.PrintReadyQrAssets
                .FirstOrDefaultAsync(
                    row =>
                        row.RestaurantLocationId == locationId
                        && row.QrType == qrType
                        && row.ShopOrderId == null,
                    cancellationToken
                );

            if (asset == null || asset.Status != PrintReadyQrAssetStatus.Failed)
            {
                throw new InvalidOperationException(
                    "Only Failed print-ready QR assets can be retried."
                );
            }

            await GenerateAndStoreAsync(location, asset, cancellationToken);

            return new PrintMaterialsAssetReadinessDto
            {
                QrType = qrType.ToString(),
                Status = asset.Status.ToString(),
                FileName = asset.FileName,
                LastError = asset.LastError,
            };
        }

        public async Task<ShopPrintAssetReadinessDto?> RetryShopOrderAsync(
            Guid shopOrderId,
            QrType qrType,
            CancellationToken cancellationToken = default
        )
        {
            if (!PhysicalShopPrintQrTypes.Contains(qrType))
            {
                return null;
            }

            var order = await _context.ShopOrders
                .Include(row => row.Location)
                    .ThenInclude(row => row.Restaurant)
                .Include(row => row.Lines)
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == shopOrderId
                        && (
                            row.PaymentStatus == ShopPaymentStatuses.Paid
                            || row.PaymentStatus == ShopPaymentStatuses.Refunded
                        ),
                    cancellationToken
                );
            if (order?.Location?.Restaurant == null)
            {
                return null;
            }

            var orderedTypes = ResolveOrderedQrTypes(
                order.Lines,
                _materialsCatalog
            );
            if (!orderedTypes.TryGetValue(qrType, out var quantity))
            {
                return null;
            }

            var asset = await _context.PrintReadyQrAssets
                .FirstOrDefaultAsync(
                    row =>
                        row.ShopOrderId == shopOrderId
                        && row.QrType == qrType,
                    cancellationToken
                );
            if (asset == null || asset.Status != PrintReadyQrAssetStatus.Failed)
            {
                throw new InvalidOperationException(
                    "Only Failed Shop print-ready QR assets can be retried."
                );
            }

            await GenerateAndStoreAsync(order.Location, asset, cancellationToken);
            return MapShopReadiness(qrType, quantity, asset, shopOrderId);
        }

        public async Task<ShopPrintAssetPreviewDto?> GetShopOrderPreviewAsync(
            Guid shopOrderId,
            QrType qrType,
            CancellationToken cancellationToken = default
        )
        {
            if (!PhysicalShopPrintQrTypes.Contains(qrType))
            {
                return null;
            }

            var order = await _context.ShopOrders
                .AsNoTracking()
                .Include(row => row.Lines)
                .Include(row => row.Location)
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == shopOrderId
                        && (
                            row.PaymentStatus == ShopPaymentStatuses.Paid
                            || row.PaymentStatus == ShopPaymentStatuses.Refunded
                        ),
                    cancellationToken
                );
            if (
                order?.Location == null
                || !ResolveOrderedQrTypes(
                    order.Lines,
                    _materialsCatalog
                ).TryGetValue(qrType, out var quantity)
            )
            {
                return null;
            }

            var asset = await _context.PrintReadyQrAssets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row =>
                        row.ShopOrderId == shopOrderId
                        && row.QrType == qrType,
                    cancellationToken
                );
            if (
                asset == null
                || asset.Status != PrintReadyQrAssetStatus.Ready
            )
            {
                return null;
            }

            var qrCode = await EligibleQrCodes(
                    order.LocationId,
                    qrType,
                    shopOrderId
                )
                .FirstOrDefaultAsync(cancellationToken);
            if (qrCode == null)
            {
                return null;
            }

            var printRoute = ResolvePrintRoute(qrType, shopOrderId);
            var productTitle = order.Lines
                .Where(line =>
                {
                    var catalog = _materialsCatalog.GetRequired(
                        _materialsCatalog.CurrentCatalogId
                    );
                    var sku = catalog.Skus.FirstOrDefault(row =>
                        string.Equals(
                            row.SkuId,
                            line.CatalogSkuId,
                            StringComparison.OrdinalIgnoreCase
                        )
                    );
                    return sku != null
                        && string.Equals(
                            sku.QrType,
                            qrType.ToString(),
                            StringComparison.Ordinal
                        );
                })
                .Select(line => line.TitleSnapshot)
                .FirstOrDefault()
                ?? qrType.ToString();

            string? offerHeadline = null;
            if (
                string.Equals(
                    printRoute,
                    PlacementPrintRoutes.GuestCardCsv,
                    StringComparison.Ordinal
                )
            )
            {
                offerHeadline = await ResolveOfferCardHeadlineAsync(
                    order.LocationId,
                    qrType,
                    cancellationToken
                );
            }

            return new ShopPrintAssetPreviewDto
            {
                ProductTitle = productTitle,
                LocationId = order.LocationId,
                LocationName =
                    order.LocationNameSnapshot
                    ?? order.Location.LocationName,
                Placement = qrType.ToString(),
                Quantity = quantity,
                QrDestinationUrl = _guestLinks.BuildGuestUrl(qrCode.Token),
                OfferHeadline = offerHeadline,
                PrintRoute = printRoute,
                ApprovedAtUtc = asset.ApprovedAtUtc,
            };
        }

        public async Task<ShopPrintAssetReadinessDto?> ApproveShopOrderAsync(
            Guid shopOrderId,
            QrType qrType,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        )
        {
            if (!PhysicalShopPrintQrTypes.Contains(qrType))
            {
                return null;
            }

            var order = await _context.ShopOrders
                .Include(row => row.Lines)
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == shopOrderId
                        && (
                            row.PaymentStatus == ShopPaymentStatuses.Paid
                            || row.PaymentStatus == ShopPaymentStatuses.Refunded
                        ),
                    cancellationToken
                );
            if (
                order == null
                || !ResolveOrderedQrTypes(
                    order.Lines,
                    _materialsCatalog
                ).TryGetValue(qrType, out var quantity)
            )
            {
                return null;
            }

            var asset = await _context.PrintReadyQrAssets
                .FirstOrDefaultAsync(
                    row =>
                        row.ShopOrderId == shopOrderId
                        && row.QrType == qrType,
                    cancellationToken
                );
            if (
                asset == null
                || asset.Status != PrintReadyQrAssetStatus.Ready
            )
            {
                return null;
            }

            var now = DateTime.UtcNow;
            asset.ApprovedAtUtc = now;
            asset.ApprovedByAdminUserId = actorAdminUserId;
            asset.UpdatedAtUtc = now;

            _audit.Append(
                new AdminAuditAppendRequest(
                    AdminAuditActions.ShopPrintAssetApproved,
                    actorIdentity,
                    AdminAuditTargetTypes.ShopOrder,
                    shopOrderId.ToString("D"),
                    ActorAdminUserId: actorAdminUserId,
                    RestaurantId: order.RestaurantId,
                    DetailJson: $"{{\"qrType\":\"{qrType}\"}}"
                )
            );

            await _context.SaveChangesAsync(cancellationToken);
            return MapShopReadiness(qrType, quantity, asset, shopOrderId);
        }

        private async Task EnsureOneAsync(
            RestaurantLocation location,
            QrType qrType,
            Guid? shopOrderId,
            CancellationToken cancellationToken
        )
        {
            var generationLock = EnsureLocks.GetOrAdd(
                (location.Id, qrType, shopOrderId),
                static _ => new SemaphoreSlim(1, 1)
            );
            await generationLock.WaitAsync(cancellationToken);
            try
            {
                await EnsureOneWithinLockAsync(
                    location,
                    qrType,
                    shopOrderId,
                    cancellationToken
                );
            }
            finally
            {
                generationLock.Release();
            }
        }

        private async Task EnsureOneWithinLockAsync(
            RestaurantLocation location,
            QrType qrType,
            Guid? shopOrderId,
            CancellationToken cancellationToken
        )
        {
            if (qrType == QrType.OfferCard)
            {
                var hasOfferCardOffer = await _context.RestaurantLocations
                    .AsNoTracking()
                    .AnyAsync(
                        row =>
                            row.Id == location.Id
                            && row.OfferCardCatalogOfferId != null,
                        cancellationToken
                    );
                if (!hasOfferCardOffer)
                {
                    // Starter / shop Offer Card PDFs wait for the print offer.
                    // Missing readiness row stays Preparing (no Ready default).
                    return;
                }
            }

            var qrCode = await EligibleQrCodes(
                    location.Id,
                    qrType,
                    shopOrderId
                )
                .FirstOrDefaultAsync(cancellationToken);

            var asset = await _context.PrintReadyQrAssets
                .FirstOrDefaultAsync(
                    row =>
                        row.RestaurantLocationId == location.Id
                        && row.QrType == qrType
                        && row.ShopOrderId == shopOrderId,
                    cancellationToken
                );

            if (qrCode == null && shopOrderId == null)
            {
                // List readiness maps a missing row to Preparing, which leaves
                // Download disabled with no Retry. Persist Failed so Admin sees
                // why the PDF cannot be built (no Active/Paused QR).
                var missingMessage =
                    $"No Active or Paused QR code for {qrType} at location {location.Id}.";
                if (asset == null)
                {
                    asset = new PrintReadyQrAsset
                    {
                        RestaurantLocationId = location.Id,
                        QrType = qrType,
                        ShopOrderId = null,
                        Status = PrintReadyQrAssetStatus.Failed,
                        LastError = TruncateError(missingMessage),
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow,
                    };
                    _context.PrintReadyQrAssets.Add(asset);
                }
                else if (
                    asset.Status != PrintReadyQrAssetStatus.Failed
                    || !string.Equals(
                        asset.LastError,
                        TruncateError(missingMessage),
                        StringComparison.Ordinal
                    )
                )
                {
                    asset.Status = PrintReadyQrAssetStatus.Failed;
                    asset.StorageKey = null;
                    asset.FileName = null;
                    asset.QrTokenFingerprint = null;
                    asset.TemplatePackVersion = null;
                    asset.OfferCopyVersion = null;
                    asset.LastError = TruncateError(missingMessage);
                    asset.UpdatedAtUtc = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            var fingerprint = qrCode == null
                ? null
                : FingerprintToken(qrCode.Token);
            var packId = _pack.CurrentPackId;
            var offerCopyVersion = _pack.Snapshot.OfferCopyVersion;
            var expectedContentType = ContentTypeForPrintRoute(
                ResolvePrintRoute(qrType, shopOrderId)
            );

            var matchesCurrentGeneration =
                fingerprint != null
                && asset != null
                && asset.Status == PrintReadyQrAssetStatus.Ready
                && string.Equals(asset.QrTokenFingerprint, fingerprint, StringComparison.Ordinal)
                && string.Equals(asset.TemplatePackVersion, packId, StringComparison.Ordinal)
                && string.Equals(
                    asset.OfferCopyVersion,
                    offerCopyVersion,
                    StringComparison.Ordinal
                )
                && string.Equals(
                    asset.ContentType,
                    expectedContentType,
                    StringComparison.OrdinalIgnoreCase
                )
                && !string.IsNullOrWhiteSpace(asset.StorageKey);
            if (
                matchesCurrentGeneration
                && await CanOpenStoredObjectAsync(
                    asset!.StorageKey!,
                    cancellationToken
                )
            )
            {
                return;
            }

            if (fingerprint != null && shopOrderId != null)
            {
                var reusable = await _context.PrintReadyQrAssets
                    .AsNoTracking()
                    .Where(row =>
                        row.RestaurantLocationId == location.Id
                        && row.QrType == qrType
                        && row.Status == PrintReadyQrAssetStatus.Ready
                        && row.QrTokenFingerprint == fingerprint
                        && row.TemplatePackVersion == packId
                        && row.OfferCopyVersion == offerCopyVersion
                        && row.StorageKey != null
                        && row.ContentType == expectedContentType
                    )
                    .OrderByDescending(row => row.UpdatedAtUtc)
                    .FirstOrDefaultAsync(cancellationToken);
                if (
                    reusable != null
                    && await CanOpenStoredObjectAsync(
                        reusable.StorageKey!,
                        cancellationToken
                    )
                )
                {
                    var reusedIsNew = asset == null;
                    asset ??= new PrintReadyQrAsset
                    {
                        RestaurantLocationId = location.Id,
                        QrType = qrType,
                        ShopOrderId = shopOrderId,
                        CreatedAtUtc = DateTime.UtcNow,
                    };
                    var reuseExtension = string.Equals(
                        reusable.ContentType,
                        PrintReadyPlacementCsvComposer.ContentType,
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? "csv"
                        : "pdf";
                    asset.Status = PrintReadyQrAssetStatus.Ready;
                    asset.StorageKey = reusable.StorageKey;
                    asset.ContentType = reusable.ContentType;
                    asset.FileName = BuildStorageFileName(
                        location,
                        qrType,
                        reuseExtension
                    );
                    asset.QrTokenFingerprint = reusable.QrTokenFingerprint;
                    asset.TemplatePackVersion = reusable.TemplatePackVersion;
                    asset.OfferCopyVersion = reusable.OfferCopyVersion;
                    asset.ApprovedAtUtc = null;
                    asset.ApprovedByAdminUserId = null;
                    asset.LastError = null;
                    asset.UpdatedAtUtc = DateTime.UtcNow;
                    if (reusedIsNew)
                    {
                        _context.PrintReadyQrAssets.Add(asset);
                    }

                    try
                    {
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                    catch (DbUpdateException ex) when (
                        reusedIsNew && IsUniqueConstraintViolation(ex)
                    )
                    {
                        _context.Entry(asset).State = EntityState.Detached;
                    }
                    return;
                }
            }

            var isNewAsset = false;
            if (asset == null)
            {
                isNewAsset = true;
                asset = new PrintReadyQrAsset
                {
                    RestaurantLocationId = location.Id,
                    QrType = qrType,
                    ShopOrderId = shopOrderId,
                    Status = PrintReadyQrAssetStatus.Preparing,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                };
                _context.PrintReadyQrAssets.Add(asset);
            }
            else
            {
                asset!.Status = PrintReadyQrAssetStatus.Preparing;
                asset.LastError = null;
                asset.UpdatedAtUtc = DateTime.UtcNow;
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (
                isNewAsset && IsUniqueConstraintViolation(ex)
            )
            {
                // Another API instance won the location/type scope insert.
                // That request owns generation; this ensure is already satisfied.
                _context.Entry(asset!).State = EntityState.Detached;
                return;
            }

            await GenerateAndStoreAsync(location, asset!, cancellationToken);
        }

        private async Task GenerateAndStoreAsync(
            RestaurantLocation location,
            PrintReadyQrAsset asset,
            CancellationToken cancellationToken
        )
        {
            Exception? lastFailure = null;
            for (
                var attempt = 1;
                attempt <= AutomaticGenerationAttempts;
                attempt++
            )
            {
                try
                {
                    await GenerateAndStoreOnceAsync(
                        location,
                        asset,
                        cancellationToken
                    );
                    return;
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    lastFailure = ex;
                    if (attempt < AutomaticGenerationAttempts)
                    {
                        _logger.LogWarning(
                            ex,
                            "Print-ready QR asset attempt {Attempt} failed for {QrType} at location {LocationId}; retrying",
                            attempt,
                            asset.QrType,
                            location.Id
                        );
                        await Task.Delay(
                            TimeSpan.FromMilliseconds(50 * attempt),
                            cancellationToken
                        );
                    }
                }
            }

            _logger.LogError(
                lastFailure,
                "Failed to generate print-ready QR asset {QrType} for location {LocationId} after {Attempts} attempts",
                asset.QrType,
                location.Id,
                AutomaticGenerationAttempts
            );
            asset.Status = PrintReadyQrAssetStatus.Failed;
            asset.LastError = TruncateError(
                lastFailure?.Message ?? "Generation failed."
            );
            asset.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task GenerateAndStoreOnceAsync(
            RestaurantLocation location,
            PrintReadyQrAsset asset,
            CancellationToken cancellationToken
        )
        {
            var qrCode = await EligibleQrCodes(
                    location.Id,
                    asset.QrType,
                    asset.ShopOrderId
                )
                .FirstOrDefaultAsync(cancellationToken);

            if (qrCode == null)
            {
                throw new InvalidOperationException(
                    asset.ShopOrderId == null
                        ? $"No Active or Paused QR code for {asset.QrType} at location {location.Id}."
                        : $"No Active QR code for {asset.QrType} at location {location.Id}."
                );
            }

            if (!_storage.IsConfigured)
            {
                throw new InvalidOperationException(
                    "Object storage is not configured for print-ready QR assets."
                );
            }

            var guestUrl = _guestLinks.BuildGuestUrl(qrCode.Token);
            var printRoute = ResolvePrintRoute(asset.QrType, asset.ShopOrderId);
            var prtQty = await ResolvePrintQuantityAsync(
                asset.ShopOrderId,
                asset.QrType,
                cancellationToken
            );
            var offerHeadline = await ResolveOfferCardHeadlineAsync(
                location.Id,
                asset.QrType,
                cancellationToken
            );

            byte[] bytes;
            string contentType;
            string extension;
            if (
                string.Equals(
                    printRoute,
                    PlacementPrintRoutes.PatchCsv60,
                    StringComparison.Ordinal
                )
            )
            {
                bytes = PrintReadyPlacementCsvComposer.ComposePatchCsv60(
                    BuildCsvRow(
                        asset,
                        location,
                        qrCode,
                        guestUrl,
                        prtQty,
                        offerHeadline: null
                    )
                );
                contentType = PrintReadyPlacementCsvComposer.ContentType;
                extension = "csv";
            }
            else if (
                string.Equals(
                    printRoute,
                    PlacementPrintRoutes.GuestCardCsv,
                    StringComparison.Ordinal
                )
            )
            {
                bytes = PrintReadyPlacementCsvComposer.ComposeGuestCardCsv(
                    BuildCsvRow(
                        asset,
                        location,
                        qrCode,
                        guestUrl,
                        prtQty,
                        offerHeadline
                    )
                );
                contentType = PrintReadyPlacementCsvComposer.ContentType;
                extension = "csv";
            }
            else
            {
                // EmbeddedPdf = Counter Display A5 with Location branding (logo/name/address).
                var raster = _rasterizer.Render(guestUrl);
                var branding = await BuildCounterCardBrandingAsync(
                    location,
                    cancellationToken
                );
                bytes = PrintReadyQrPdfComposer.ComposeCounterCard(
                    raster,
                    branding,
                    ResolveCounterCardAssets()
                );
                contentType = PrintReadyQrPdfComposer.ContentType;
                extension = "pdf";
            }

            var fingerprint = FingerprintToken(qrCode.Token);
            var fileName = BuildStorageFileName(
                location,
                asset.QrType,
                extension
            );
            var storageKey =
                $"print-ready-qr/{location.Id}/{asset.QrType}/{fingerprint}/"
                + $"{Slug(_pack.CurrentPackId)}-"
                + $"{Slug(_pack.Snapshot.OfferCopyVersion)}.{extension}";

            await using var upload = new MemoryStream(bytes);
            await _storage.UploadAsync(
                storageKey,
                upload,
                contentType,
                bytes.Length,
                cancellationToken
            );

            asset.Status = PrintReadyQrAssetStatus.Ready;
            asset.StorageKey = storageKey;
            asset.ContentType = contentType;
            asset.FileName = fileName;
            asset.QrTokenFingerprint = fingerprint;
            asset.TemplatePackVersion = _pack.CurrentPackId;
            asset.OfferCopyVersion = _pack.Snapshot.OfferCopyVersion;
            asset.ApprovedAtUtc = null;
            asset.ApprovedByAdminUserId = null;
            asset.LastError = null;
            asset.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        private CounterCardAssetPaths? ResolveCounterCardAssets()
        {
            var packDir = Path.GetDirectoryName(_pack.Snapshot.CardSvgPath);
            if (string.IsNullOrWhiteSpace(packDir))
            {
                return null;
            }

            var counterDir = Path.Combine(packDir, "counter-card");
            string? Pick(string fileName)
            {
                var path = Path.Combine(counterDir, fileName);
                return File.Exists(path) ? path : null;
            }

            return new CounterCardAssetPaths(
                PatternSvgPath: Pick("group6.svg"),
                StripSvgPath: Pick("strip.svg"),
                PoweredSvgPath: Pick("powered.svg")
            );
        }

        private async Task<CounterCardBranding> BuildCounterCardBrandingAsync(
            RestaurantLocation location,
            CancellationToken cancellationToken
        )
        {
            var restaurant = location.Restaurant;
            if (restaurant == null)
            {
                restaurant = await _context.Restaurants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        row => row.Id == location.RestaurantId,
                        cancellationToken
                    );
            }

            byte[]? logoBytes = null;
            string? logoContentType = null;
            var logoKey = restaurant?.BrandLogoObjectKey;
            if (
                !string.IsNullOrWhiteSpace(logoKey)
                && _storage.IsConfigured
            )
            {
                try
                {
                    await using var stream = await _storage.OpenReadAsync(
                        logoKey,
                        cancellationToken
                    );
                    using var memory = new MemoryStream();
                    await stream.CopyToAsync(memory, cancellationToken);
                    logoBytes = memory.ToArray();
                    logoContentType = restaurant?.BrandLogoContentType;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Could not load brand logo for Counter Display at location {LocationId}",
                        location.Id
                    );
                }
            }

            var addressParts = new[]
            {
                location.Address,
                location.City,
                location.Postcode,
            }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim());

            return new CounterCardBranding(
                LocationName: location.LocationName?.Trim() ?? string.Empty,
                AddressLine: string.Join(", ", addressParts),
                LogoBytes: logoBytes,
                LogoContentType: logoContentType
            );
        }

        private static PrintReadyPlacementCsvRow BuildCsvRow(
            PrintReadyQrAsset asset,
            RestaurantLocation location,
            QrCode qrCode,
            string guestUrl,
            int prtQty,
            string? offerHeadline
        )
        {
            var orderId = asset.ShopOrderId?.ToString("D") ?? "starter";
            var jobId = asset.ShopOrderId is Guid shopOrderId
                ? $"{shopOrderId:N}-{asset.QrType}"
                : $"starter-{location.Id}-{asset.QrType}";

            return new PrintReadyPlacementCsvRow(
                JobId: jobId,
                OrderId: orderId,
                LocationId: location.Id.ToString(),
                LocationName: location.LocationName,
                PlacementId: qrCode.Id.ToString(),
                PlacementType: asset.QrType.ToString(),
                QrUrl: guestUrl,
                PrtQty: prtQty,
                OfferHeadline: offerHeadline
            );
        }

        private string ResolvePrintRoute(QrType qrType, Guid? shopOrderId)
        {
            var catalog = _materialsCatalog.GetRequired(
                _materialsCatalog.CurrentCatalogId
            );

            if (shopOrderId is Guid orderId)
            {
                var lineSkuIds = _context.ShopOrderLines
                    .AsNoTracking()
                    .Where(row => row.ShopOrderId == orderId)
                    .Select(row => row.CatalogSkuId)
                    .ToList();

                foreach (var skuId in lineSkuIds)
                {
                    var sku = catalog.Skus.FirstOrDefault(row =>
                        string.Equals(
                            row.SkuId,
                            skuId,
                            StringComparison.OrdinalIgnoreCase
                        )
                    );
                    if (
                        sku != null
                        && string.Equals(
                            sku.QrType,
                            qrType.ToString(),
                            StringComparison.Ordinal
                        )
                    )
                    {
                        return string.IsNullOrWhiteSpace(sku.PrintRoute)
                            ? PlacementPrintRoutes.EmbeddedPdf
                            : sku.PrintRoute;
                    }
                }
            }

            var byType = catalog.Skus.FirstOrDefault(row =>
                string.Equals(
                    row.QrType,
                    qrType.ToString(),
                    StringComparison.Ordinal
                )
            );
            return string.IsNullOrWhiteSpace(byType?.PrintRoute)
                ? PlacementPrintRoutes.EmbeddedPdf
                : byType!.PrintRoute;
        }

        private async Task<int> ResolvePrintQuantityAsync(
            Guid? shopOrderId,
            QrType qrType,
            CancellationToken cancellationToken
        )
        {
            if (shopOrderId == null)
            {
                return 1;
            }

            var order = await _context.ShopOrders
                .AsNoTracking()
                .Include(row => row.Lines)
                .FirstOrDefaultAsync(
                    row => row.Id == shopOrderId.Value,
                    cancellationToken
                );
            if (order == null)
            {
                return 1;
            }

            return ResolveOrderedQrTypes(order.Lines, _materialsCatalog)
                .GetValueOrDefault(qrType, 1);
        }

        private async Task<string> ResolveOfferCardHeadlineAsync(
            int locationId,
            QrType qrType,
            CancellationToken cancellationToken
        )
        {
            if (qrType is not (QrType.OfferCard or QrType.GuestCard))
            {
                return _pack.Snapshot.DefaultOfferHeadline;
            }

            var title = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == locationId)
                .Select(row =>
                    row.OfferCardCatalogOffer != null
                        ? row.OfferCardCatalogOffer.Title
                        : null
                )
                .FirstOrDefaultAsync(cancellationToken);

            return string.IsNullOrWhiteSpace(title)
                ? _pack.Snapshot.DefaultOfferHeadline
                : title.Trim();
        }

        private IQueryable<QrCode> EligibleQrCodes(
            int locationId,
            QrType qrType,
            Guid? shopOrderId
        )
        {
            var matchingCodes = _context.QrCodes
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantLocationId == locationId
                    && row.QrType == qrType
                );

            return shopOrderId == null
                ? matchingCodes.Where(row =>
                    row.Status == QrCodeStatus.Active
                    || row.Status == QrCodeStatus.Paused
                )
                : matchingCodes.Where(row =>
                    row.Status == QrCodeStatus.Active
                );
        }

        public static IReadOnlyDictionary<QrType, int> ResolveOrderedQrTypes(
            IEnumerable<ShopOrderLine> lines,
            IMaterialsCatalog materialsCatalog
        )
        {
            var catalog = materialsCatalog.GetRequired(
                materialsCatalog.CurrentCatalogId
            );
            var skus = catalog.Skus.ToDictionary(
                row => row.SkuId,
                StringComparer.OrdinalIgnoreCase
            );
            var result = new Dictionary<QrType, int>();

            foreach (var line in lines)
            {
                // Prefer recipe-backed catalog QrType (denormalized PlacementKey).
                // Fall back to legacy sku-id map for older complimentary orders.
                QrType qrType;
                if (
                    skus.TryGetValue(line.CatalogSkuId, out var sku)
                    && Enum.TryParse<QrType>(
                        sku.QrType,
                        ignoreCase: false,
                        out qrType
                    )
                )
                {
                    // ok
                }
                else if (
                    !TryResolveLegacySkuQrType(line.CatalogSkuId, out qrType)
                )
                {
                    continue;
                }

                if (!PhysicalShopPrintQrTypes.Contains(qrType))
                {
                    continue;
                }

                result[qrType] =
                    result.GetValueOrDefault(qrType) + Math.Max(0, line.Quantity);
            }

            return result;
        }

        /// <summary>
        /// Legacy complimentary SKU ids before PlacementRecipe existed.
        /// </summary>
        private static bool TryResolveLegacySkuQrType(
            string catalogSkuId,
            out QrType qrType
        )
        {
            qrType = catalogSkuId.Trim().ToLowerInvariant() switch
            {
                "table-tents" => QrType.TableTent,
                "table-talker" => QrType.TableTalker,
                "window-stickers" => QrType.WindowSticker,
                "offer-card" => QrType.OfferCard,
                "qr-counter-display" => QrType.OfferCard,
                "qr-guest-cards" => QrType.GuestCard,
                _ => default,
            };
            return catalogSkuId.Trim().ToLowerInvariant()
                is "table-tents"
                    or "table-talker"
                    or "window-stickers"
                    or "offer-card"
                    or "qr-counter-display"
                    or "qr-guest-cards";
        }

        private async Task<bool> CanOpenStoredObjectAsync(
            string storageKey,
            CancellationToken cancellationToken
        )
        {
            try
            {
                await using var stream = await _storage.OpenReadAsync(
                    storageKey,
                    cancellationToken
                );
                return true;
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested
            )
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Print-ready QR storage object {StorageKey} could not be opened; it will be regenerated",
                    storageKey
                );
                return false;
            }
        }

        private async Task<byte[]> ReadStoredBytesAsync(
            PrintReadyQrAsset asset,
            CancellationToken cancellationToken
        )
        {
            try
            {
                await using var stream = await _storage.OpenReadAsync(
                    asset.StorageKey!,
                    cancellationToken
                );
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, cancellationToken);
                return buffer.ToArray();
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
                    "Print-ready QR storage object {StorageKey} could not be downloaded",
                    asset.StorageKey
                );
                asset.Status = PrintReadyQrAssetStatus.Failed;
                asset.LastError = TruncateError(
                    $"Stored print asset is missing or unavailable: {ex.Message}"
                );
                asset.UpdatedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                throw new PrintReadyQrNotReadyException(
                    PrintReadyQrAssetStatus.Failed
                );
            }
        }

        private ShopPrintAssetReadinessDto MapShopReadiness(
            QrType qrType,
            int quantity,
            PrintReadyQrAsset? asset,
            Guid? shopOrderId
        )
        {
            return new ShopPrintAssetReadinessDto
            {
                QrType = qrType.ToString(),
                Quantity = quantity,
                Status = asset?.Status.ToString()
                    ?? PrintReadyQrAssetStatus.Preparing.ToString(),
                FileName = asset?.FileName,
                LastError = asset?.LastError,
                Approved = asset?.ApprovedAtUtc != null,
                PrintRoute = ResolvePrintRoute(
                    qrType,
                    shopOrderId ?? asset?.ShopOrderId
                ),
            };
        }

        private async Task<List<int>> OwnedLocationIdsAsync(
            int operatorUserId,
            CancellationToken cancellationToken
        )
        {
            return await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Restaurant!.OwnerUserId == operatorUserId)
                .Select(row => row.Id)
                .ToListAsync(cancellationToken);
        }

        private async Task<bool> IsOwnedLocationAsync(
            int operatorUserId,
            int locationId,
            CancellationToken cancellationToken
        )
        {
            return await _context.RestaurantLocations
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.Id == locationId
                        && row.Restaurant!.OwnerUserId == operatorUserId,
                    cancellationToken
                );
        }

        private static string FingerprintToken(string token)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string BuildStorageFileName(
            RestaurantLocation location,
            QrType qrType,
            string extension = "pdf"
        )
        {
            var restaurant = Slug(
                location.Restaurant?.Name ?? "restaurant"
            );
            var locationSlug = Slug(location.LocationName);
            var typeSlug = QrTypeSlug(qrType);
            return $"tummly-{restaurant}-{locationSlug}-{typeSlug}.{extension}";
        }

        /// <summary>
        /// Distinct type-specific download names when shared storage bytes
        /// would otherwise collide (tent / sticker).
        /// </summary>
        private static string BuildDistinctFileName(string storedName, QrType qrType)
        {
            var typeSlug = QrTypeSlug(qrType);

            if (storedName.Contains(typeSlug, StringComparison.OrdinalIgnoreCase))
            {
                return storedName;
            }

            var extension = storedName.EndsWith(
                ".csv",
                StringComparison.OrdinalIgnoreCase
            )
                ? ".csv"
                : ".pdf";
            var stem = storedName.EndsWith(
                extension,
                StringComparison.OrdinalIgnoreCase
            )
                ? storedName[..^extension.Length]
                : storedName;
            return $"{stem}-{typeSlug}{extension}";
        }

        private static string QrTypeSlug(QrType qrType) =>
            qrType switch
            {
                QrType.CounterCard => "counter-card",
                QrType.TableTent => "table-tent",
                QrType.TableTalker => "table-talker",
                QrType.WindowSticker => "window-sticker",
                QrType.OfferCard => "offer-card",
                QrType.GuestCard => "guest-card",
                _ => qrType.ToString().ToLowerInvariant(),
            };

        private static string Slug(string value)
        {
            var sb = new StringBuilder(value.Length);
            var lastDash = false;
            foreach (var ch in value.ToLowerInvariant())
            {
                if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
                {
                    sb.Append(ch);
                    lastDash = false;
                }
                else if (!lastDash && sb.Length > 0)
                {
                    sb.Append('-');
                    lastDash = true;
                }
            }

            var slug = sb.ToString().Trim('-');
            return string.IsNullOrWhiteSpace(slug) ? "item" : slug;
        }

        private static string ContentTypeForPrintRoute(string printRoute)
        {
            return string.Equals(
                printRoute,
                PlacementPrintRoutes.EmbeddedPdf,
                StringComparison.Ordinal
            )
                ? PrintReadyQrPdfComposer.ContentType
                : PrintReadyPlacementCsvComposer.ContentType;
        }

        private static string TruncateError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "Generation failed.";
            }

            return message.Length <= 1000 ? message : message[..1000];
        }

        private static bool IsUniqueConstraintViolation(
            DbUpdateException exception
        )
        {
            return exception.InnerException is SqlException
            {
                Number: 2601 or 2627,
            };
        }
    }

    public sealed class PrintReadyQrNotReadyException : Exception
    {
        public PrintReadyQrNotReadyException(PrintReadyQrAssetStatus status)
            : base($"Print-ready QR asset is not ready ({status}).")
        {
            Status = status;
        }

        public PrintReadyQrAssetStatus Status { get; }
    }

    public sealed class PrintReadyQrNotApprovedException : Exception
    {
        public PrintReadyQrNotApprovedException()
            : base("Print-ready QR asset is not approved for download.")
        {
        }
    }
}
