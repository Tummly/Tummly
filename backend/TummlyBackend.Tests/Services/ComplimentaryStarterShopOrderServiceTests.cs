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
            _service = new ComplimentaryStarterShopOrderService(
                _context,
                MaterialsCatalog.LoadFromDirectory(ResolveMaterialsPackDir()),
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

            var billing = await _context.BillingAccounts.SingleAsync();
            Assert.Equal(
                StarterKitStates.PendingDispatch,
                billing.StarterKitState
            );
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
