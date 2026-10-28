using System.Globalization;
using System.Text.Json;
using Microsoft.Data.SqlClient;
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
        private readonly IAdminAuditService _audit;

        public ShopInterestService(
            ApplicationDbContext context,
            IAdminAuditService audit
        )
        {
            _context = context;
            _audit = audit;
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
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                // A concurrent request registered the same user, product and
                // Location first (IX_ShopInterestRegistrations_User_Product_Location).
                return (true, null, ThanksMessage);
            }

            return (true, null, ThanksMessage);
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            return exception.InnerException is SqlException
            {
                Number: 2601 or 2627,
            };
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

            var counts = await ListCountsByProductAsync(cancellationToken);

            return new AdminShopInterestListDto
            {
                Items = items,
                CountsByProduct = counts,
            };
        }

        public async Task<(bool Ok, string? Error)> RemoveRegistrationAsync(
            int registrationId,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        )
        {
            var row = await _context.ShopInterestRegistrations
                .FirstOrDefaultAsync(
                    registration => registration.Id == registrationId,
                    cancellationToken
                );
            if (row == null)
            {
                return (false, "Interest registration was not found.");
            }

            _context.ShopInterestRegistrations.Remove(row);
            _audit.Append(
                new AdminAuditAppendRequest(
                    AdminAuditActions.ShopInterestRegistrationRemoved,
                    actorIdentity,
                    AdminAuditTargetTypes.ShopInterestRegistration,
                    row.Id.ToString(CultureInfo.InvariantCulture),
                    ActorAdminUserId: actorAdminUserId,
                    RestaurantId: row.RestaurantId,
                    DetailJson: JsonSerializer.Serialize(
                        new
                        {
                            productKey = row.ProductKey,
                            userId = row.UserId,
                            locationId = row.LocationId,
                        }
                    )
                )
            );

            await _context.SaveChangesAsync(cancellationToken);
            return (true, null);
        }

        public async Task<(string FileName, byte[] Content)> ExportCountsCsvAsync(
            CancellationToken cancellationToken = default
        )
        {
            var counts = await ListCountsByProductAsync(cancellationToken);
            var rows = counts
                .Select(row => (IReadOnlyList<string>)
                [
                    row.ProductKey,
                    row.Title,
                    row.Count.ToString(CultureInfo.InvariantCulture),
                ])
                .ToList();

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            return (
                $"tummly-shop-interest-counts-{stamp}Z.csv",
                Rfc4180Csv.WriteUtf8(
                    ["Product key", "Product", "Interest count"],
                    rows
                )
            );
        }

        private async Task<List<AdminShopInterestProductCountDto>> ListCountsByProductAsync(
            CancellationToken cancellationToken
        )
        {
            // Every active product shows, even with no interest yet. A product
            // that left the active list keeps its row while it still has
            // registrations.
            var registered = await _context.ShopInterestRegistrations
                .AsNoTracking()
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

            return countsByKey.Values
                .OrderByDescending(row => row.Count)
                .ThenBy(row => row.Title, StringComparer.Ordinal)
                .ToList();
        }
    }
}
