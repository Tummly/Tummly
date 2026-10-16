using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class ShopInterestService : IShopInterestService
    {
        public const string ThanksMessage =
            "Thanks — we have noted your interest.";

        private readonly ApplicationDbContext _context;

        public ShopInterestService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ShopInterestProductDto>> ListActiveProductsAsync(
            int? userId = null,
            int? locationId = null,
            CancellationToken cancellationToken = default
        )
        {
            var products = await _context.ShopInterestProducts
                .AsNoTracking()
                .Where(row => row.IsActive)
                .OrderBy(row => row.Id)
                .ToListAsync(cancellationToken);

            HashSet<string>? registeredKeys = null;
            if (userId is > 0 && locationId is > 0)
            {
                var keys = await _context.ShopInterestRegistrations
                    .AsNoTracking()
                    .Where(row =>
                        row.UserId == userId.Value
                        && row.LocationId == locationId.Value
                    )
                    .Select(row => row.ProductKey)
                    .ToListAsync(cancellationToken);
                registeredKeys = keys.ToHashSet(StringComparer.Ordinal);
            }

            return products
                .Select(row => new ShopInterestProductDto
                {
                    ProductKey = row.ProductKey,
                    Title = row.Title,
                    Description = row.Description,
                    ImageUrl = row.ImageUrl,
                    InterestRegistered =
                        registeredKeys != null
                        && registeredKeys.Contains(row.ProductKey),
                })
                .ToList();
        }

        public async Task<(bool Ok, string? Error, string? Message)> RegisterAsync(
            int userId,
            int restaurantId,
            string restaurantName,
            RegisterShopInterestRequest request,
            string? locationName,
            string userFullName,
            string userEmail,
            CancellationToken cancellationToken = default
        )
        {
            var productKey = (request.ProductKey ?? string.Empty).Trim();
            if (productKey.Length == 0)
            {
                return (false, "productKey is required.", null);
            }

            var product = await _context.ShopInterestProducts
                .FirstOrDefaultAsync(
                    row =>
                        row.ProductKey == productKey
                        && row.IsActive,
                    cancellationToken
                );
            if (product == null)
            {
                return (false, "Interest product was not found.", null);
            }

            var locationId = request.LocationId is > 0
                ? request.LocationId
                : null;

            var already = await _context.ShopInterestRegistrations
                .AnyAsync(
                    row =>
                        row.UserId == userId
                        && row.ShopInterestProductId == product.Id
                        && row.LocationId == locationId,
                    cancellationToken
                );
            if (already)
            {
                return (true, null, ThanksMessage);
            }

            _context.ShopInterestRegistrations.Add(
                new ShopInterestRegistration
                {
                    ShopInterestProductId = product.Id,
                    ProductKey = product.ProductKey,
                    ProductTitleSnapshot = product.Title,
                    RestaurantId = restaurantId,
                    RestaurantNameSnapshot = restaurantName,
                    LocationId = locationId,
                    LocationNameSnapshot = locationName,
                    UserId = userId,
                    UserFullNameSnapshot = userFullName,
                    UserEmailSnapshot = userEmail,
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync(cancellationToken);

            return (true, null, ThanksMessage);
        }

        public async Task<(bool Ok, string? Error)> UnregisterAsync(
            int userId,
            string productKey,
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var key = (productKey ?? string.Empty).Trim();
            if (key.Length == 0)
            {
                return (false, "productKey is required.");
            }

            var rows = await _context.ShopInterestRegistrations
                .Where(row =>
                    row.UserId == userId
                    && row.ProductKey == key
                    && row.LocationId == locationId
                )
                .ToListAsync(cancellationToken);

            if (rows.Count == 0)
            {
                return (true, null);
            }

            _context.ShopInterestRegistrations.RemoveRange(rows);
            await _context.SaveChangesAsync(cancellationToken);
            return (true, null);
        }

        public async Task<AdminShopInterestListDto> ListForAdminAsync(
            CancellationToken cancellationToken = default
        )
        {
            var items = await _context.ShopInterestRegistrations
                .AsNoTracking()
                .OrderByDescending(row => row.CreatedAtUtc)
                .ThenByDescending(row => row.Id)
                .Select(row => new AdminShopInterestRegistrationDto
                {
                    Id = row.Id,
                    ProductKey = row.ProductKey,
                    ProductTitle = row.ProductTitleSnapshot,
                    RestaurantId = row.RestaurantId,
                    RestaurantName = row.RestaurantNameSnapshot,
                    LocationId = row.LocationId,
                    LocationName = row.LocationNameSnapshot,
                    UserId = row.UserId,
                    UserFullName = row.UserFullNameSnapshot,
                    UserEmail = row.UserEmailSnapshot,
                    CreatedAtUtc = row.CreatedAtUtc,
                })
                .ToListAsync(cancellationToken);

            var counts = await _context.ShopInterestRegistrations
                .AsNoTracking()
                .GroupBy(row => new { row.ProductKey, row.ProductTitleSnapshot })
                .Select(group => new AdminShopInterestProductCountDto
                {
                    ProductKey = group.Key.ProductKey,
                    Title = group.Key.ProductTitleSnapshot,
                    Count = group.Count(),
                })
                .OrderByDescending(row => row.Count)
                .ThenBy(row => row.Title)
                .ToListAsync(cancellationToken);

            return new AdminShopInterestListDto
            {
                Items = items,
                CountsByProduct = counts,
            };
        }
    }
}
