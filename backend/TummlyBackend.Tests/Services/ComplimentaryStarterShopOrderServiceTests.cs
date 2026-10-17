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
        public async Task EnsureForLocationAsync_CreatesPaidZeroOrderWithCounterDisplayLine()
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
            Assert.Equal(0, order.DeliveryNetPence);
            Assert.Null(order.RevolutOrderId);
            Assert.Equal("ORD-1", order.OrderNumber);
            var line = Assert.Single(order.Lines);
            Assert.Equal(
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                line.CatalogSkuId
            );
            Assert.Equal(
                ShopMaterialsCatalogSeed.CounterDisplayRecipeSlug,
                line.MaterialType
            );
            Assert.Equal(1, line.Quantity);
            Assert.Equal(0, line.UnitNetPence);
            Assert.Equal(0, line.LineNetPence);

            var billing = await _context.BillingAccounts.SingleAsync();
            Assert.Equal(
                StarterKitStates.Unused,
                billing.StarterKitState
            );
        }

        [Fact]
        public async Task EnsureForLocationAsync_UsesReplacedPublishedSkuOnRecipe()
        {
            var counter = await _context.ShopMaterialSkus
                .Include(row => row.PlacementRecipe)
                .SingleAsync(row =>
                    row.SkuId == ShopMaterialsCatalogSeed.CounterDisplaySkuId
                );
            counter.Status = ShopMaterialSkuStatuses.Deleted;
            await _context.SaveChangesAsync();

            _context.ShopMaterialSkus.Add(
                new ShopMaterialSku
                {
                    SkuId = "qr-counter-display-v2",
                    Title = "QR Counter Display V2",
                    Category = "payment",
                    Description = "Replacement",
                    Material = "Card",
                    Dimensions = "A5",
                    PlacementRecipeId = counter.PlacementRecipeId,
                    QrType = "OfferCard",
                    UnitNetPence = 999,
                    MinOrderQty = 1,
                    Currency = "GBP",
                    Status = ShopMaterialSkuStatuses.Published,
                    AvailableInShop = true,
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
                line => line.CatalogSkuId == "qr-counter-display-v2"
            );
            Assert.DoesNotContain(
                order.Lines,
                line =>
                    line.CatalogSkuId
                    == ShopMaterialsCatalogSeed.CounterDisplaySkuId
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

        [Fact]
        public async Task EnsureAllActiveLocationsAsync_DoesNotDuplicateExistingComplimentary()
        {
            var first = await _service.EnsureForLocationAsync(
                _restaurantId,
                _locationId,
                _userId,
                "Alex Owner"
            );
            Assert.True(first.Created);

            // Paid conversion / Pilot mint re-runs Ensure for all Active locations.
            var shopOrderIds = await _service.EnsureAllActiveLocationsAsync(
                _restaurantId,
                _userId,
                "Alex Owner"
            );

            Assert.Equal([first.ShopOrderId], shopOrderIds);
            Assert.Equal(
                1,
                await _context.ShopOrders.CountAsync(row => row.IsComplimentary)
            );
            var order = await _context.ShopOrders
                .Include(row => row.Lines)
                .SingleAsync(row => row.IsComplimentary);
            Assert.Equal(first.ShopOrderId, order.Id);
            Assert.Equal(
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                Assert.Single(order.Lines).CatalogSkuId
            );
        }

        [Fact]
        public async Task EnsureForLocationAsync_PaidShopOrderDoesNotAddAnotherComplimentary()
        {
            var complimentary = await _service.EnsureForLocationAsync(
                _restaurantId,
                _locationId,
                _userId,
                "Alex Owner"
            );
            Assert.True(complimentary.Created);

            // Additional / replacement units use the paid Shop flow (not complimentary).
            _context.ShopOrders.Add(
                new ShopOrder
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = "ORD-2",
                    RestaurantId = _restaurantId,
                    LocationId = _locationId,
                    LocationNameSnapshot = "Main",
                    PlacedByUserId = _userId,
                    PlacedByNameSnapshot = "Alex Owner",
                    MaterialsNetPence = 1800,
                    VatPence = 360,
                    DeliveryNetPence = 0,
                    GrossPence = 2160,
                    DeliveryMethod = ShopDeliveryMethods.Standard,
                    PaymentStatus = ShopPaymentStatuses.Paid,
                    PaidAtUtc = DateTime.UtcNow,
                    FulfilmentStatus = ShopFulfilmentStatuses.Processing,
                    ProcessingStartedAtUtc = DateTime.UtcNow,
                    ShipToContactName = "Alex Owner",
                    ShipToAddressLine1 = "1 High Street",
                    ShipToPostcode = "LS1 1AA",
                    ShipToCountry = "United Kingdom",
                    IsComplimentary = false,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                    Lines =
                    {
                        new ShopOrderLine
                        {
                            Id = Guid.NewGuid(),
                            CatalogSkuId =
                                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                            TitleSnapshot = "QR Counter Display",
                            MaterialType =
                                ShopMaterialsCatalogSeed.CounterDisplayRecipeSlug,
                            Quantity = 1,
                            UnitNetPence = 1800,
                            LineNetPence = 1800,
                        },
                    },
                }
            );
            await _context.SaveChangesAsync();

            var again = await _service.EnsureForLocationAsync(
                _restaurantId,
                _locationId,
                _userId,
                "Alex Owner"
            );

            Assert.False(again.Created);
            Assert.Equal(complimentary.ShopOrderId, again.ShopOrderId);
            Assert.Equal(2, await _context.ShopOrders.CountAsync());
            Assert.Equal(
                1,
                await _context.ShopOrders.CountAsync(row => row.IsComplimentary)
            );
        }

        [Fact]
        public async Task EnsureForLocationAsync_OmitsPublishedSkusOutsideStarterSet()
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
            var line = Assert.Single(order.Lines);
            Assert.Equal(
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                line.CatalogSkuId
            );
            Assert.DoesNotContain(
                order.Lines,
                row =>
                    row.CatalogSkuId
                        is "table-tents"
                            or "table-talker"
                            or "window-stickers"
                            or "qr-guest-cards"
            );
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
