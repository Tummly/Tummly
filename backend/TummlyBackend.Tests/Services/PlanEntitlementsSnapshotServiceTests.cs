using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class PlanEntitlementsSnapshotServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly PlanEntitlementsSnapshotService _service;

        public PlanEntitlementsSnapshotServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            _context = new ApplicationDbContext(options);
            _service = new PlanEntitlementsSnapshotService(
                _context,
                PricebookCatalog.LoadFromDirectory(PackDirectory())
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private static string PackDirectory()
        {
            var dir = Path.GetFullPath(
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
            if (!Directory.Exists(dir))
            {
                dir = Path.GetFullPath(
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

            Assert.True(Directory.Exists(dir), $"Pack directory missing: {dir}");
            return dir;
        }

        [Fact]
        public async Task GetAccountAsync_Starter_ReturnsExpectedCaps()
        {
            var restaurantId = await SeedRestaurantAsync(
                BillingSubscriptionPlans.Starter
            );

            var snapshot = await _service.GetAccountAsync(restaurantId);

            Assert.True(snapshot.TeamMembers.Available);
            Assert.Equal(3, snapshot.TeamMembers.Cap);
            Assert.Equal(1, snapshot.TeamMembers.Current);
            Assert.True(snapshot.ActiveOffers.Available);
            Assert.Equal(3, snapshot.ActiveOffers.Cap);
            Assert.Equal(0, snapshot.ActiveOffers.Current);
            Assert.True(snapshot.Locations.Available);
            Assert.Equal(1, snapshot.Locations.Cap);
            Assert.Equal(1, snapshot.Locations.Current);
        }

        [Fact]
        public async Task GetLocationAsync_Starter_ReturnsActiveQrCap()
        {
            var restaurantId = await SeedRestaurantAsync(
                BillingSubscriptionPlans.Starter
            );
            var locationId = await _context.RestaurantLocations
                .Where(row => row.RestaurantId == restaurantId)
                .Select(row => row.Id)
                .SingleAsync();

            var snapshot = await _service.GetLocationAsync(
                restaurantId,
                locationId
            );

            Assert.NotNull(snapshot);
            Assert.True(snapshot!.ActiveQrPlacements.Available);
            Assert.Equal(10, snapshot.ActiveQrPlacements.Cap);
            Assert.Equal(0, snapshot.ActiveQrPlacements.Current);
            Assert.True(snapshot.PublishedGuestForms.Available);
            Assert.Equal(1, snapshot.PublishedGuestForms.Cap);
        }

        private async Task<int> SeedRestaurantAsync(string subscriptionPlan)
        {
            var owner = new User
            {
                FullName = "Owner",
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(owner);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Entitlement Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var account = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                "TUMMLY-UK-GBP-2026-08-V3"
            );
            account.SubscriptionPlan = subscriptionPlan;
            _context.BillingAccounts.Add(account);

            _context.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = "Main",
                    Address = "1 High Street",
                    CreatedAt = DateTime.UtcNow,
                }
            );

            _context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = owner.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.Owner,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );

            await _context.SaveChangesAsync();
            return restaurant.Id;
        }
    }
}
