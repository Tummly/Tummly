using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class ShopCartService : IShopCartService
    {
        private const string CurrencyGbp = "GBP";

        private readonly ApplicationDbContext _context;
        private readonly IMaterialsCatalog _catalog;

        public ShopCartService(
            ApplicationDbContext context,
            IMaterialsCatalog catalog
        )
        {
            _context = context;
            _catalog = catalog;
        }

        public async Task<ShopCartDto> GetCartAsync(
            int restaurantId,
            int locationId,
            int userId,
            CancellationToken cancellationToken = default
        )
        {
            var cart = await FindCartWithLinesAsync(
                restaurantId,
                locationId,
                userId,
                cancellationToken
            );
            return BuildDto(locationId, cart);
        }

        public async Task<ShopCartDto?> UpsertLineAsync(
            int restaurantId,
            int locationId,
            int userId,
            string skuId,
            int quantity,
            CancellationToken cancellationToken = default
        )
        {
            var detail = _catalog.TryBuildDetail(skuId);
            if (detail == null || quantity < detail.MinOrderQty)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            var cart = await FindCartWithLinesAsync(
                restaurantId,
                locationId,
                userId,
                cancellationToken
            );
            if (cart == null)
            {
                cart = new ShopCart
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurantId,
                    LocationId = locationId,
                    UserId = userId,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                };
                _context.ShopCarts.Add(cart);
            }

            var line = cart.Lines.FirstOrDefault(row =>
                string.Equals(row.SkuId, skuId, StringComparison.Ordinal)
            );
            if (line == null)
            {
                cart.Lines.Add(new ShopCartLine
                {
                    SkuId = skuId,
                    Quantity = quantity,
                });
            }
            else
            {
                line.Quantity = quantity;
            }

            cart.UpdatedAtUtc = now;
            await _context.SaveChangesAsync(cancellationToken);

            return BuildDto(locationId, cart);
        }

        public async Task<ShopCartDto> RemoveLineAsync(
            int restaurantId,
            int locationId,
            int userId,
            string skuId,
            CancellationToken cancellationToken = default
        )
        {
            var cart = await FindCartWithLinesAsync(
                restaurantId,
                locationId,
                userId,
                cancellationToken
            );
            if (cart == null)
            {
                return BuildDto(locationId, null);
            }

            var line = cart.Lines.FirstOrDefault(row =>
                string.Equals(row.SkuId, skuId, StringComparison.Ordinal)
            );
            if (line != null)
            {
                _context.ShopCartLines.Remove(line);
                cart.UpdatedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return BuildDto(locationId, cart);
        }

        public async Task ClearCartAsync(
            int restaurantId,
            int locationId,
            int userId,
            CancellationToken cancellationToken = default
        )
        {
            var cart = await FindCartWithLinesAsync(
                restaurantId,
                locationId,
                userId,
                cancellationToken
            );
            if (cart == null)
            {
                return;
            }

            _context.ShopCartLines.RemoveRange(cart.Lines);
            _context.ShopCarts.Remove(cart);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<ShopCart?> FindCartWithLinesAsync(
            int restaurantId,
            int locationId,
            int userId,
            CancellationToken cancellationToken
        )
        {
            return await _context.ShopCarts
                .Include(row => row.Lines)
                .FirstOrDefaultAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && row.LocationId == locationId
                        && row.UserId == userId,
                    cancellationToken
                );
        }

        private ShopCartDto BuildDto(int locationId, ShopCart? cart)
        {
            if (cart == null || cart.Lines.Count == 0)
            {
                return new ShopCartDto
                {
                    LocationId = locationId,
                    Lines = [],
                    MaterialsNetPence = 0,
                    Currency = CurrencyGbp,
                };
            }

            var lines = new List<ShopCartLineDto>();
            var materialsNetPence = 0;
            foreach (var row in cart.Lines.OrderBy(line => line.SkuId))
            {
                var detail = _catalog.TryBuildDetail(row.SkuId);
                if (detail == null)
                {
                    continue;
                }

                var lineNet = checked(detail.UnitNetPence * row.Quantity);
                materialsNetPence = checked(materialsNetPence + lineNet);
                lines.Add(new ShopCartLineDto
                {
                    SkuId = row.SkuId,
                    Quantity = row.Quantity,
                    Title = detail.Title,
                    UnitNetPence = detail.UnitNetPence,
                    LineNetPence = lineNet,
                });
            }

            return new ShopCartDto
            {
                LocationId = locationId,
                Lines = lines,
                MaterialsNetPence = materialsNetPence,
                Currency = CurrencyGbp,
            };
        }
    }
}
