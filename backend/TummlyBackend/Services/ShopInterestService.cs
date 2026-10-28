using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Helpers;
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
                .Include(row => row.Images)
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
                        && row.WithdrawnAtUtc == null
                    )
                    .Select(row => row.ProductKey)
                    .ToListAsync(cancellationToken);
                registeredKeys = keys.ToHashSet(StringComparer.Ordinal);
            }

            return products
                .Select(row =>
                {
                    var firstImage = row.Images
                        .OrderBy(image => image.SortOrder)
                        .ThenBy(image => image.Id)
                        .FirstOrDefault();
                    return new ShopInterestProductDto
                    {
                        ProductKey = row.ProductKey,
                        Title = row.Title,
                        Description = row.Description,
                        ImageUrl =
                            firstImage == null
                                ? null
                                : ShopCatalogImageRules.BuildPublicUrl(
                                    firstImage.StorageKey
                                ),
                        InterestRegistered =
                            registeredKeys != null
                            && registeredKeys.Contains(row.ProductKey),
                    };
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

            var already = await ActiveRegistrationExistsAsync(
                userId,
                product.Id,
                restaurantId,
                locationId,
                cancellationToken
            );
            if (already)
            {
                return (true, null, ThanksMessage);
            }

            var registration = new ShopInterestRegistration
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
            };
            _context.ShopInterestRegistrations.Add(registration);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // A concurrent Register for the same key won the filtered unique index.
                // That active row is the answer, so this is a success. Any other failure
                // has no active row and is rethrown.
                _context.Entry(registration).State = EntityState.Detached;
                var raced = await ActiveRegistrationExistsAsync(
                    userId,
                    product.Id,
                    restaurantId,
                    locationId,
                    cancellationToken
                );
                if (!raced)
                {
                    throw;
                }
            }

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
                    && row.WithdrawnAtUtc == null
                )
                .ToListAsync(cancellationToken);

            if (rows.Count == 0)
            {
                return (true, null);
            }

            var withdrawnAtUtc = DateTime.UtcNow;
            foreach (var row in rows)
            {
                row.WithdrawnAtUtc = withdrawnAtUtc;
                row.WithdrawnByUserId = userId;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return (true, null);
        }

        private Task<bool> ActiveRegistrationExistsAsync(
            int userId,
            int productId,
            int restaurantId,
            int? locationId,
            CancellationToken cancellationToken
        ) =>
            _context.ShopInterestRegistrations
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.UserId == userId
                        && row.ShopInterestProductId == productId
                        && row.RestaurantId == restaurantId
                        && row.LocationId == locationId
                        && row.WithdrawnAtUtc == null,
                    cancellationToken
                );

        public async Task<AdminShopInterestListDto> ListForAdminAsync(
            bool includeWithdrawn = false,
            CancellationToken cancellationToken = default
        )
        {
            var rowsQuery = _context.ShopInterestRegistrations.AsNoTracking();
            if (!includeWithdrawn)
            {
                rowsQuery = rowsQuery.Where(row => row.WithdrawnAtUtc == null);
            }

            var rows = await rowsQuery
                .OrderByDescending(row => row.CreatedAtUtc)
                .ThenByDescending(row => row.Id)
                .ToListAsync(cancellationToken);
            var items = rows
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
                    UserEmailMasked = ShopInterestEmailMask.Mask(row.UserEmailSnapshot),
                    State = row.WithdrawnAtUtc == null
                        ? ShopInterestRegistrationStates.Active
                        : ShopInterestRegistrationStates.Withdrawn,
                    CreatedAtUtc = row.CreatedAtUtc,
                    WithdrawnAtUtc = row.WithdrawnAtUtc,
                })
                .ToList();

            // Every active product shows, even with no interest yet. A product
            // that left the active list keeps its row while it still has
            // registrations. Counts are always active registrations, whatever
            // the list filter is.
            var registered = await _context.ShopInterestRegistrations
                .AsNoTracking()
                .Where(row => row.WithdrawnAtUtc == null)
                .GroupBy(row => new { row.ProductKey, row.ProductTitleSnapshot })
                .Select(group => new
                {
                    group.Key.ProductKey,
                    Title = group.Key.ProductTitleSnapshot,
                    Count = group.Count(),
                })
                .ToListAsync(cancellationToken);
            var activeProducts = await _context.ShopInterestProducts
                .AsNoTracking()
                .Where(row => row.IsActive)
                .Select(row => new { row.ProductKey, row.Title })
                .ToListAsync(cancellationToken);

            var countsByKey = new Dictionary<string, AdminShopInterestProductCountDto>(
                StringComparer.Ordinal
            );
            foreach (var product in activeProducts)
            {
                countsByKey[product.ProductKey] = new AdminShopInterestProductCountDto
                {
                    ProductKey = product.ProductKey,
                    Title = product.Title,
                    Count = 0,
                };
            }

            foreach (var row in registered)
            {
                if (countsByKey.TryGetValue(row.ProductKey, out var existing))
                {
                    existing.Count += row.Count;
                    continue;
                }

                countsByKey[row.ProductKey] = new AdminShopInterestProductCountDto
                {
                    ProductKey = row.ProductKey,
                    Title = row.Title,
                    Count = row.Count,
                };
            }

            var counts = countsByKey.Values
                .OrderByDescending(row => row.Count)
                .ThenBy(row => row.Title, StringComparer.Ordinal)
                .ToList();

            return new AdminShopInterestListDto
            {
                Items = items,
                CountsByProduct = counts,
            };
        }
    }
}
