using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Shop.MaterialsCatalog;

namespace TummlyBackend.Tests.Services
{
    public class LocationsLifecycleWriteActivateStarterTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ComplimentaryStarterShopOrderService _complimentary;
        private readonly RecordingPrintReadyQrMaterialsWork _printWork = new();
        private readonly LocationsLifecycleWriteService _write;
        private int _userId;
        private int _restaurantId;
        private int _locationId;

        public LocationsLifecycleWriteActivateStarterTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;

            _context = new ApplicationDbContext(options);
            _complimentary = new ComplimentaryStarterShopOrderService(
                _context,
                MaterialsCatalog.LoadFromDirectory(ResolveMaterialsPackDir()),
                new ShopOrderNumberAllocator(_context)
            );
            _write = new LocationsLifecycleWriteService(
                _context,
                _complimentary,
                _printWork
            );

            SeedDraftLocation();
        }

        [Fact]
        public async Task ActivateDraftAsync_CreatesComplimentaryStarterOrder()
        {
            var result = await _write.ActivateDraftAsync(
                _restaurantId,
                _locationId,
                _userId
            );

            Assert.IsType<LocationLifecycleWriteResult.Ok>(result);
            var location = await _context.RestaurantLocations.SingleAsync();
            Assert.Equal(LocationLifecycleStatus.Active, location.LifecycleStatus);

            var order = await _context.ShopOrders.SingleAsync(o => o.IsComplimentary);
            Assert.Equal(_locationId, order.LocationId);
            Assert.Equal(0, order.GrossPence);
            Assert.Contains(order.Id, _printWork.RequestedShopOrderIds);
        }

        [Fact]
        public async Task ActivateDraftAsync_IsIdempotent_ForComplimentaryOrder()
        {
            var result = await _write.ActivateDraftAsync(
                _restaurantId,
                _locationId,
                _userId
            );
            Assert.IsType<LocationLifecycleWriteResult.Ok>(result);

            var second = await _complimentary.EnsureForLocationAsync(
                _restaurantId,
                _locationId,
                _userId,
                "Alex Owner"
            );

            Assert.False(second.Created);
            Assert.Equal(1, await _context.ShopOrders.CountAsync());
            Assert.NotEqual(Guid.Empty, second.ShopOrderId);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedDraftLocation()
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
                LifecycleStatus = LocationLifecycleStatus.Draft,
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

        private sealed class RecordingPrintReadyQrMaterialsWork
            : IPrintReadyQrMaterialsWork
        {
            public List<Guid> RequestedShopOrderIds { get; } = [];

            public ValueTask RequestEnsureAsync(
                int locationId,
                CancellationToken cancellationToken = default
            ) => ValueTask.CompletedTask;

            public ValueTask RequestShopOrderEnsureAsync(
                Guid shopOrderId,
                CancellationToken cancellationToken = default
            )
            {
                RequestedShopOrderIds.Add(shopOrderId);
                return ValueTask.CompletedTask;
            }

            public Task RunAsync(CancellationToken stoppingToken)
                => Task.CompletedTask;

            public Task DrainAsync(
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;
        }
    }
}
