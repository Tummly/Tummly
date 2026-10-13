using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Shop.MaterialsCatalog;

namespace TummlyBackend.Tests.Services
{
    public class ComplimentaryStarterShopOrderServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ComplimentaryStarterShopOrderService _service;
        private int _userId;
        private int _restaurantId;
        private int _locationId;

        public ComplimentaryStarterShopOrderServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;

            _context = new ApplicationDbContext(options);
            ShopMaterialsCatalogSeed
                .EnsureSeededAsync(_context)
                .GetAwaiter()
                .GetResult();
            _service = new ComplimentaryStarterShopOrderService(
                _context,
                new DbMaterialsCatalog(_context),
                new ShopOrderNumberAllocator(_context)
            );

            SeedAccount();
        }

        [Fact]
        public async Task EnsureForLocationAsync_CreatesPaidZeroOrderWithThreeLines()
        {
            var result = await _service.EnsureForLocationAsync(
                _restaurantId,
                _locationId,
                _userId,
                "Alex Owner"
            );

            Assert.True(result.Created);
            var order = await _context.ShopOrders
                .Include(row => row.Lines)
                .SingleAsync(row => row.Id == result.ShopOrderId);

            Assert.True(order.IsComplimentary);
            Assert.Equal(ShopPaymentStatuses.Paid, order.PaymentStatus);
            Assert.Equal(ShopFulfilmentStatuses.Processing, order.FulfilmentStatus);
            Assert.Equal(0, order.GrossPence);
            Assert.Equal(0, order.MaterialsNetPence);
            Assert.Equal(0, order.VatPence);
            Assert.Null(order.RevolutOrderId);
            Assert.Equal("ORD-1", order.OrderNumber);
            Assert.Equal(3, order.Lines.Count);
            Assert.Contains(order.Lines, line => line.MaterialType == "table-tent");
            Assert.Contains(order.Lines, line => line.MaterialType == "window-sticker");
            Assert.Contains(order.Lines, line => line.MaterialType == "offer-card");

            var billing = await _context.BillingAccounts.SingleAsync();
            Assert.Equal(
                StarterKitStates.Unused,
                billing.StarterKitState
            );
        }

        [Fact]
        public async Task EnsureForLocationAsync_UsesReplacedPublishedSkuOnRecipe()
        {
            var offer = await _context.ShopMaterialSkus
                .Include(row => row.PlacementRecipe)
                .SingleAsync(row => row.SkuId == "offer-card");
            offer.Status = ShopMaterialSkuStatuses.Deleted;
            await _context.SaveChangesAsync();

            _context.ShopMaterialSkus.Add(
                new ShopMaterialSku
                {
                    SkuId = "offer-card-v2",
                    Title = "Offer Card V2",
                    Category = "payment",
                    Description = "Replacement",
                    Material = "Card",
                    Dimensions = "85mm",
                    PlacementRecipeId = offer.PlacementRecipeId,
                    QrType = "OfferCard",
                    UnitNetPence = 1800,
                    MinOrderQty = 1,
                    Currency = "GBP",
                    Status = ShopMaterialSkuStatuses.Published,
                    AboutBody = "",
                    SpecificationsJson = "[]",
                    PublishedAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync();

            var result = await _service.EnsureForLocationAsync(
                _restaurantId,
                _locationId,
                _userId,
                "Alex Owner"
            );

            Assert.True(result.Created);
            var order = await _context.ShopOrders
                .Include(row => row.Lines)
                .SingleAsync(row => row.Id == result.ShopOrderId);
            Assert.Contains(
                order.Lines,
                line => line.CatalogSkuId == "offer-card-v2"
            );
            Assert.DoesNotContain(
                order.Lines,
                line => line.CatalogSkuId == "offer-card"
            );
        }

        [Fact]
        public async Task EnsureForLocationAsync_DoesNotCreate_WhenBillingPlanIsFree()
        {
            var billing = await _context.BillingAccounts.SingleAsync();
            billing.SubscriptionPlan = BillingSubscriptionPlans.Free;
            billing.BillingStatus = BillingStatuses.Free;
            await _context.SaveChangesAsync();

            var result = await _service.EnsureForLocationAsync(
                _restaurantId,
                _locationId,
                _userId,
                "Alex Owner"
            );

            Assert.False(result.Created);
            Assert.Equal(Guid.Empty, result.ShopOrderId);
            Assert.Equal(0, await _context.ShopOrders.CountAsync());
        }

        [Fact]
        public async Task EnsureForLocationAsync_DoesNotCreate_WhenLocationIsDraft()
        {
            var location = await _context.RestaurantLocations.SingleAsync();
            location.LifecycleStatus = LocationLifecycleStatus.Draft;
            await _context.SaveChangesAsync();

            var result = await _service.EnsureForLocationAsync(
                _restaurantId,
                _locationId,
                _userId,
                "Alex Owner"
            );

            Assert.False(result.Created);
            Assert.Equal(Guid.Empty, result.ShopOrderId);
            Assert.Equal(0, await _context.ShopOrders.CountAsync());

            var billing = await _context.BillingAccounts.SingleAsync();
            Assert.Equal(StarterKitStates.Unused, billing.StarterKitState);
        }

        [Fact]
        public async Task EnsureForLocationAsync_Creates_WhenLocationIsActive()
        {
            // Seed already Active — same as CreatesPaidZeroOrderWithThreeLines core asserts
            var result = await _service.EnsureForLocationAsync(
                _restaurantId,
                _locationId,
                _userId,
                "Alex Owner"
            );
            Assert.True(result.Created);
            Assert.NotEqual(Guid.Empty, result.ShopOrderId);
        }

        [Fact]
        public async Task EnsureForLocationAsync_DoesNotWriteBillingAccountStarterKitState()
        {
            var result = await _service.EnsureForLocationAsync(
                _restaurantId,
                _locationId,
                _userId,
                "Alex Owner"
            );
            Assert.True(result.Created);
            var billing = await _context.BillingAccounts.SingleAsync();
            Assert.Equal(StarterKitStates.Unused, billing.StarterKitState);
        }

        [Fact]
        public async Task EnsureForLocationAsync_IsIdempotent()
        {
            var first = await _service.EnsureForLocationAsync(
                _restaurantId,
                _locationId,
                _userId,
                "Alex Owner"
            );
            var second = await _service.EnsureForLocationAsync(
                _restaurantId,
                _locationId,
                _userId,
                "Alex Owner"
            );

            Assert.True(first.Created);
            Assert.False(second.Created);
            Assert.Equal(first.ShopOrderId, second.ShopOrderId);
            Assert.Equal(1, await _context.ShopOrders.CountAsync());
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedAccount()
        {
            var user = new User
            {
                FullName = "Alex Owner",
                Email = "owner@example.com",
                PasswordHash = "hash",
                Role = "Owner",
                AccountType = "Single",
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            _userId = user.Id;

            var restaurant = new Restaurant
            {
                Name = "Test",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                BillingAccount = BillingCreditsService.CreateDefaultBillingAccount(
                    restaurantId: 0,
                    "TUMMLY-UK-GBP-2026-08-V3"
                ),
            };
            restaurant.BillingAccount.SubscriptionPlan =
                BillingSubscriptionPlans.Pilot;
            restaurant.BillingAccount.BillingStatus = BillingStatuses.Pilot;
            _context.Restaurants.Add(restaurant);
            _context.SaveChanges();
            _restaurantId = restaurant.Id;

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                City = "Leeds",
                Postcode = "LS1 1AA",
                LifecycleStatus = LocationLifecycleStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            _context.SaveChanges();
            _locationId = location.Id;
        }

        private static string ResolveMaterialsPackDir()
        {
            var packDir = Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "..",
                    "docs",
                    "product",
                    "materials-catalog-v1"
                )
            );
            if (!Directory.Exists(packDir))
            {
                packDir = Path.GetFullPath(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "..",
                        "..",
                        "..",
                        "..",
                        "docs",
                        "product",
                        "materials-catalog-v1"
                    )
                );
            }

            return packDir;
        }
    }
}
