using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class ShopOrderCancelReorderService : IShopOrderCancelReorderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMaterialsCatalog _catalog;

        public ShopOrderCancelReorderService(
            ApplicationDbContext context,
            IMaterialsCatalog catalog
        )
        {
            _context = context;
            _catalog = catalog;
        }

        public async Task<ShopOrderCancelResult> CancelAsync(
            int restaurantId,
            int userId,
            Guid orderId,
            int locationId,
            string reasonSlug,
            CancellationToken cancellationToken = default
        )
        {
            var order = await _context.ShopOrders
                .Include(row => row.Lines)
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == orderId
                        && row.RestaurantId == restaurantId
                        && row.LocationId == locationId,
                    cancellationToken
                );
            if (order == null)
            {
                return ShopOrderCancelResult.Fail(
                    "order_not_found",
                    "Shop order was not found."
                );
            }

            if (
                string.Equals(
                    order.FulfilmentStatus,
                    ShopFulfilmentStatuses.Cancelled,
                    StringComparison.Ordinal
                )
            )
            {
                return ShopOrderCancelResult.Ok(
                    ShopOrderDtoMapper.MapOperatorDetail(order)
                );
            }

            if (!ShopCancelReasons.IsValidSlug(reasonSlug))
            {
                return ShopOrderCancelResult.Fail(
                    "invalid_cancel_reason",
                    "reason must be a valid cancel reason slug."
                );
            }

            if (!ShopOrderCancelRules.CanCancel(order))
            {
                return ShopOrderCancelResult.Fail(
                    "shop_order_not_cancellable",
                    "This shop order cannot be cancelled."
                );
            }

            var now = DateTime.UtcNow;
            order.FulfilmentStatus = ShopFulfilmentStatuses.Cancelled;
            order.CancelReason = reasonSlug.Trim();
            order.CancelledAtUtc = now;
            order.CancelledByUserId = userId;
            order.UpdatedAtUtc = now;

            await _context.SaveChangesAsync(cancellationToken);

            return ShopOrderCancelResult.Ok(
                ShopOrderDtoMapper.MapOperatorDetail(order)
            );
        }

        public async Task<ShopOrderReorderResult> BuildReorderPrefillAsync(
            int restaurantId,
            Guid orderId,
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var order = await _context.ShopOrders
                .AsNoTracking()
                .Include(row => row.Lines)
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == orderId
                        && row.RestaurantId == restaurantId
                        && row.LocationId == locationId,
                    cancellationToken
                );
            if (order == null)
            {
                return ShopOrderReorderResult.Fail(
                    "order_not_found",
                    "Shop order was not found."
                );
            }

            if (!ShopOrderCancelRules.IsReorderEligible(order))
            {
                return ShopOrderReorderResult.Fail(
                    "shop_order_not_reorderable",
                    "This shop order cannot be reordered."
                );
            }

            var unavailable = new List<string>();
            var lines = new List<ShopReorderPrefillLineDto>();

            foreach (var line in order.Lines.OrderBy(row => row.CatalogSkuId))
            {
                var detail = _catalog.TryBuildDetail(line.CatalogSkuId);
                if (detail == null)
                {
                    unavailable.Add(line.CatalogSkuId);
                    continue;
                }

                var lineNetPence = checked(detail.UnitNetPence * line.Quantity);
                lines.Add(new ShopReorderPrefillLineDto
                {
                    SkuId = detail.SkuId,
                    Quantity = line.Quantity,
                    Title = detail.Title,
                    UnitNetPence = detail.UnitNetPence,
                    LineNetPence = lineNetPence,
                });
            }

            if (unavailable.Count > 0)
            {
                return ShopOrderReorderResult.SkuUnavailable(unavailable);
            }

            return ShopOrderReorderResult.Ok(new ShopReorderPrefillDto
            {
                LocationId = order.LocationId,
                Lines = lines,
                ShipTo = ShopOrderDtoMapper.MapShipTo(order),
                DeliveryMethod = order.DeliveryMethod,
                SourceOrderNumber = order.OrderNumber,
            });
        }
    }
}
