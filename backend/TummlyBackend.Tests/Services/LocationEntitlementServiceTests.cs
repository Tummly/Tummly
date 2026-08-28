using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class LocationEntitlementServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OwnedLocationInsertService _insert;
        private readonly CaptureQrLifecycleService _lifecycle;
        private int _userId;
        private int _restaurantId;
        private int _locationId;

        public LocationEntitlementServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;

            _context = new ApplicationDbContext(options);

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
                    "billing-pack-v3.0"
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
                        "billing-pack-v3.0"
                    )
                );
            }

            var catalog = PricebookCatalog.LoadFromDirectory(packDir);
            _insert = new OwnedLocationInsertService(_context, catalog);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://tummly.example",
                    }
                )
                .Build();
            var smartGuestLink = new SmartGuestLinkService(_context, configuration);
            _lifecycle = new CaptureQrLifecycleService(
                _context,
                smartGuestLink,
                catalog
            );

            SeedPilotAtCap();
        }

        [Fact]
        public async Task PauseLocationCapture_DoesNotFreeASlot()
        {
            var pause = await _lifecycle.PauseLocationCaptureAsync(
                new LocationCaptureLifecycleCommand
                {
                    UserId = _userId,
                    LocationId = _locationId,
                }
            );
            Assert.Equal(QrLifecycleResultKind.Ok, pause.Kind);

            var location = await _context.RestaurantLocations.FirstAsync(
                row => row.Id == _locationId
            );
            Assert.Equal(
                CaptureLocationStatus.Paused,
                location.CaptureLocationStatus
            );

            var result = await _insert.AddAsync(
                _restaurantId,
                new AddOwnedLocationRequest
                {
                    LocationName = "Second",
                    Address = "2 High Street",
                }
            );

            var denied = Assert.IsType<AddOwnedLocationResult.CapReached>(result);
            Assert.Equal(1, denied.Cap);
            Assert.Equal(1, denied.Current);

            var ownedCount = await _context.RestaurantLocations
                .CountAsync(row => row.RestaurantId == _restaurantId);
            Assert.Equal(1, ownedCount);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedPilotAtCap()
        {
            var user = new User
            {
                FullName = "Operator One",
                Email = "op-cap@example.com",
                PasswordHash = "x",
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            _userId = user.Id;

            var restaurant = new Restaurant
            {
                Name = "Pilot Cap Restaurant",
                AccountType = "Multi",
                OwnerUserId = _userId,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            _context.SaveChanges();
            _restaurantId = restaurant.Id;

            _context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    "TUMMLY-UK-GBP-2026-08-V3"
                )
            );

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
                CaptureLocationStatus = CaptureLocationStatus.Active,
            };
            _context.RestaurantLocations.Add(location);
            _context.SaveChanges();
            _locationId = location.Id;
        }
    }
}
