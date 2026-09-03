using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class AdminShopOrderFulfilmentService
        : IAdminShopOrderFulfilmentService
    {
        private const int MaxPageSize = 100;
        private const int MaxTrackingUrlLength = 2048;
        private const int MaxOpsNotesLength = 2000;
        private const string ReceiptStickersSkuId = "receipt-stickers";

        private static readonly HashSet<string> ValidFulfilmentFilters =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ShopFulfilmentStatuses.Processing,
                ShopFulfilmentStatuses.InTransit,
                ShopFulfilmentStatuses.Delivered,
                ShopFulfilmentStatuses.Cancelled,
            };

        private static readonly string[] ExportHeaders =
        [
            "orderNumber",
            "locationNameSnapshot",
            "shipToContactName",
            "shipToContactPhone",
            "shipToAddressLine1",
            "shipToAddressLine2",
            "shipToPostcode",
            "shipToCountry",
            "deliveryInstructions",
            "deliveryMethod",
            "catalogSkuId",
            "titleSnapshot",
            "quantity",
            "unitNetPence",
            "lineNetPence",
            "fulfilmentStatus",
        ];

        private readonly ApplicationDbContext _context;
        private readonly ISmartGuestLinkService _smartGuestLink;

        public AdminShopOrderFulfilmentService(
            ApplicationDbContext context,
            ISmartGuestLinkService smartGuestLink
        )
        {
            _context = context;
            _smartGuestLink = smartGuestLink;
        }

        public async Task<AdminShopOrderListResponseDto> GetListAsync(
            AdminShopOrdersListQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ValidateListQuery(query);

            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
            var filtered = ApplyFilters(BaseAdminQuery(), query);

            var totalCount = await filtered.CountAsync(cancellationToken);
            var pageRows = await filtered
                .OrderByDescending(row => row.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(row => row.Lines)
                .ToListAsync(cancellationToken);

            return new AdminShopOrderListResponseDto
            {
                Items = pageRows.Select(MapListItem).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<AdminShopOrderFulfilmentResult> UpdateFulfilmentAsync(
            Guid orderId,
            AdminShopOrderFulfilmentPatchDto patch,
            CancellationToken cancellationToken = default
        )
        {
            var order = await _context.ShopOrders
                .Include(row => row.Lines)
                .FirstOrDefaultAsync(row => row.Id == orderId, cancellationToken);

            if (order == null)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "order_not_found",
                    "Shop order was not found."
                );
            }

            if (
                string.IsNullOrWhiteSpace(order.FulfilmentStatus)
                || order.PaymentStatus != ShopPaymentStatuses.Paid
                    && order.PaymentStatus != ShopPaymentStatuses.Refunded
            )
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "illegal_fulfilment_transition",
                    "Shop order is not eligible for admin fulfilment updates."
                );
            }

            var currentStatus = order.FulfilmentStatus;
            var targetStatus = string.IsNullOrWhiteSpace(patch.FulfilmentStatus)
                ? currentStatus
                : patch.FulfilmentStatus.Trim().ToLowerInvariant();

            if (!IsLegalTransition(currentStatus, targetStatus))
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "illegal_fulfilment_transition",
                    $"Cannot move fulfilment from '{currentStatus}' to '{targetStatus}'."
                );
            }

            if (patch.OpsNotesSet)
            {
                var notesError = ValidateOpsNotes(patch.OpsNotes);
                if (notesError != null)
                {
                    return notesError;
                }
            }

            if (patch.TrackingUrlSet)
            {
                var trackingError = ValidateTrackingUrlMutation(
                    currentStatus,
                    targetStatus,
                    patch.TrackingUrl
                );
                if (trackingError != null)
                {
                    return trackingError;
                }
            }

            var now = DateTime.UtcNow;
            var transitioningToInTransit =
                currentStatus == ShopFulfilmentStatuses.Processing
                && targetStatus == ShopFulfilmentStatuses.InTransit;
            var transitioningToDelivered =
                currentStatus == ShopFulfilmentStatuses.InTransit
                && targetStatus == ShopFulfilmentStatuses.Delivered;

            if (transitioningToInTransit)
            {
                order.FulfilmentStatus = ShopFulfilmentStatuses.InTransit;
                order.DispatchedAtUtc = now;
            }
            else if (transitioningToDelivered)
            {
                order.FulfilmentStatus = ShopFulfilmentStatuses.Delivered;
                order.DeliveredAtUtc = now;
            }

            if (patch.TrackingUrlSet)
            {
                order.TrackingUrl = NormalizeTrackingUrl(patch.TrackingUrl);
            }

            if (patch.OpsNotesSet)
            {
                order.OpsNotes = NormalizeOpsNotes(patch.OpsNotes);
            }

            if (transitioningToDelivered)
            {
                await MintReceiptStickerIfNeededAsync(order, cancellationToken);
            }

            order.UpdatedAtUtc = now;
            await _context.SaveChangesAsync(cancellationToken);

            return AdminShopOrderFulfilmentResult.Ok(MapListItem(order));
        }

        public async Task<AdminShopOrdersExportResult> ExportCsvAsync(
            AdminShopOrdersListQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ValidateListQuery(query);

            var orders = await ApplyFilters(BaseAdminQuery(), query)
                .OrderByDescending(row => row.CreatedAtUtc)
                .Include(row => row.Lines)
                .ToListAsync(cancellationToken);

            var csvRows = new List<IReadOnlyList<string>>();
            foreach (var order in orders)
            {
                IEnumerable<ShopOrderLine?> lines =
                    order.Lines.Count == 0
                        ? new ShopOrderLine?[] { null }
                        : order.Lines
                            .OrderBy(line => line.CatalogSkuId)
                            .Cast<ShopOrderLine?>();

                foreach (var line in lines)
                {
                    csvRows.Add(
                        [
                            order.OrderNumber,
                            order.LocationNameSnapshot,
                            order.ShipToContactName,
                            order.ShipToContactPhone ?? string.Empty,
                            order.ShipToAddressLine1,
                            order.ShipToAddressLine2 ?? string.Empty,
                            order.ShipToPostcode,
                            order.ShipToCountry,
                            order.DeliveryInstructions ?? string.Empty,
                            order.DeliveryMethod,
                            line?.CatalogSkuId ?? string.Empty,
                            line?.TitleSnapshot ?? string.Empty,
                            line?.Quantity.ToString() ?? string.Empty,
                            line?.UnitNetPence.ToString() ?? string.Empty,
                            line?.LineNetPence.ToString() ?? string.Empty,
                            order.FulfilmentStatus ?? string.Empty,
                        ]
                    );
                }
            }

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            return new AdminShopOrdersExportResult
            {
                FileName = $"tummly-shop-orders-{stamp}Z.csv",
                ContentType = "text/csv",
                Content = Rfc4180Csv.WriteUtf8(ExportHeaders, csvRows),
            };
        }

        private IQueryable<ShopOrder> BaseAdminQuery()
        {
            return _context.ShopOrders
                .AsNoTracking()
                .Where(row =>
                    row.PaymentStatus == ShopPaymentStatuses.Paid
                    || row.PaymentStatus == ShopPaymentStatuses.Refunded
                );
        }

        private static IQueryable<ShopOrder> ApplyFilters(
            IQueryable<ShopOrder> query,
            AdminShopOrdersListQuery listQuery
        )
        {
            var fulfilmentFilters = listQuery.FulfilmentStatus
                .Where(status => !string.IsNullOrWhiteSpace(status))
                .Select(status => status.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            if (fulfilmentFilters.Count == 0)
            {
                fulfilmentFilters.Add(ShopFulfilmentStatuses.Processing);
            }

            query = query.Where(row =>
                row.FulfilmentStatus != null
                && fulfilmentFilters.Contains(row.FulfilmentStatus)
            );

            if (listQuery.RestaurantId is int restaurantId)
            {
                query = query.Where(row => row.RestaurantId == restaurantId);
            }

            var normalizedQuery = listQuery.Q?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(normalizedQuery))
            {
                var term = normalizedQuery.ToLowerInvariant();
                query = query.Where(row =>
                    row.OrderNumber.ToLower().Contains(term)
                    || row.LocationNameSnapshot.ToLower().Contains(term)
                    || row.PlacedByNameSnapshot.ToLower().Contains(term)
                    || row.Lines.Any(line =>
                        line.TitleSnapshot.ToLower().Contains(term)
                    )
                );
            }

            return query;
        }

        private static void ValidateListQuery(AdminShopOrdersListQuery query)
        {
            if (query.PageSize < 1 || query.PageSize > MaxPageSize)
            {
                throw new ArgumentException(
                    $"pageSize must be between 1 and {MaxPageSize}."
                );
            }

            if (
                query.FulfilmentStatus.Any(status =>
                    !string.IsNullOrWhiteSpace(status)
                    && !ValidFulfilmentFilters.Contains(status.Trim())
                )
            )
            {
                throw new ArgumentException("Invalid fulfilmentStatus filter.");
            }
        }

        private static bool IsLegalTransition(string from, string to)
        {
            if (
                from == ShopFulfilmentStatuses.Processing
                && to == ShopFulfilmentStatuses.InTransit
            )
            {
                return true;
            }

            if (
                from == ShopFulfilmentStatuses.InTransit
                && to is ShopFulfilmentStatuses.Delivered or ShopFulfilmentStatuses.InTransit
            )
            {
                return true;
            }

            if (
                from == ShopFulfilmentStatuses.Delivered
                && to == ShopFulfilmentStatuses.Delivered
            )
            {
                return true;
            }

            return false;
        }

        private static AdminShopOrderFulfilmentResult? ValidateOpsNotes(
            string? opsNotes
        )
        {
            if (opsNotes == null)
            {
                return null;
            }

            if (opsNotes.Length > MaxOpsNotesLength)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_ops_notes",
                    $"opsNotes must be at most {MaxOpsNotesLength} characters."
                );
            }

            return null;
        }

        private static AdminShopOrderFulfilmentResult? ValidateTrackingUrlMutation(
            string currentStatus,
            string targetStatus,
            string? trackingUrl
        )
        {
            var effectiveStatusForTracking =
                targetStatus == ShopFulfilmentStatuses.InTransit
                || currentStatus == ShopFulfilmentStatuses.InTransit
                    && targetStatus == ShopFulfilmentStatuses.InTransit
                    ? ShopFulfilmentStatuses.InTransit
                    : targetStatus;

            if (
                currentStatus == ShopFulfilmentStatuses.Delivered
                || targetStatus == ShopFulfilmentStatuses.Delivered
                    && currentStatus != ShopFulfilmentStatuses.InTransit
            )
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "tracking_url_read_only",
                    "trackingUrl is read-only once the order is delivered."
                );
            }

            // Delivered transition: tracking stays as-is (read-only after deliver).
            if (
                currentStatus == ShopFulfilmentStatuses.InTransit
                && targetStatus == ShopFulfilmentStatuses.Delivered
            )
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "tracking_url_read_only",
                    "trackingUrl cannot change on the delivered transition."
                );
            }

            if (
                trackingUrl == null
                && effectiveStatusForTracking != ShopFulfilmentStatuses.InTransit
            )
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_tracking_url",
                    "trackingUrl can only be cleared while in_transit."
                );
            }

            if (
                trackingUrl == null
                && effectiveStatusForTracking == ShopFulfilmentStatuses.InTransit
            )
            {
                return null;
            }

            if (
                currentStatus == ShopFulfilmentStatuses.Processing
                && targetStatus != ShopFulfilmentStatuses.InTransit
            )
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_tracking_url",
                    "trackingUrl can only be set when moving to or while in_transit."
                );
            }

            var normalized = trackingUrl!.Trim();
            if (normalized.Length == 0)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_tracking_url",
                    "trackingUrl must be a non-empty https URL or null to clear."
                );
            }

            if (normalized.Length > MaxTrackingUrlLength)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_tracking_url",
                    $"trackingUrl must be at most {MaxTrackingUrlLength} characters."
                );
            }

            if (
                !Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
                || !string.Equals(
                    uri.Scheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_tracking_url",
                    "trackingUrl must be an https URL."
                );
            }

            return null;
        }

        private static string? NormalizeTrackingUrl(string? trackingUrl)
        {
            if (trackingUrl == null)
            {
                return null;
            }

            return trackingUrl.Trim();
        }

        private static string? NormalizeOpsNotes(string? opsNotes)
        {
            if (opsNotes == null)
            {
                return null;
            }

            var trimmed = opsNotes.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }

        private async Task MintReceiptStickerIfNeededAsync(
            ShopOrder order,
            CancellationToken cancellationToken
        )
        {
            var hasReceiptStickers = order.Lines.Any(line =>
                string.Equals(
                    line.CatalogSkuId,
                    ReceiptStickersSkuId,
                    StringComparison.OrdinalIgnoreCase
                )
            );
            if (!hasReceiptStickers)
            {
                return;
            }

            var alreadyActive = await _context.QrCodes.AnyAsync(
                row =>
                    row.RestaurantLocationId == order.LocationId
                    && row.QrType == QrType.ReceiptSticker
                    && row.Status == QrCodeStatus.Active,
                cancellationToken
            );
            if (alreadyActive)
            {
                return;
            }

            var token = await _smartGuestLink.GenerateTokenAsync();
            _context.QrCodes.Add(
                new QrCode
                {
                    RestaurantLocationId = order.LocationId,
                    QrType = QrType.ReceiptSticker,
                    Token = token,
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                }
            );
        }

        private static AdminShopOrderListItemDto MapListItem(ShopOrder order)
        {
            return new AdminShopOrderListItemDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                RestaurantId = order.RestaurantId,
                LocationId = order.LocationId,
                LocationNameSnapshot = order.LocationNameSnapshot,
                FulfilmentStatus = order.FulfilmentStatus ?? string.Empty,
                PaymentStatus = order.PaymentStatus,
                RevolutOrderId = order.RevolutOrderId,
                TrackingUrl = order.TrackingUrl,
                OpsNotes = order.OpsNotes,
                PaidAtUtc = order.PaidAtUtc,
                GrossPence = order.GrossPence,
                Lines = order.Lines
                    .OrderBy(line => line.CatalogSkuId, StringComparer.OrdinalIgnoreCase)
                    .Select(line => new AdminShopOrderLineSummaryDto
                    {
                        CatalogSkuId = line.CatalogSkuId,
                        TitleSnapshot = line.TitleSnapshot,
                        Quantity = line.Quantity,
                        UnitNetPence = line.UnitNetPence,
                        LineNetPence = line.LineNetPence,
                    })
                    .ToList(),
            };
        }
    }
}
