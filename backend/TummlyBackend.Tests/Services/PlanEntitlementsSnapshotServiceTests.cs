using TummlyBackend.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Configurations;
using TummlyBackend.DTOs.Admin;
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
                PricebookCatalog.LoadFromDirectory(PackDirectory()),
                TestActiveQrCapacity.For(_context)
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
        public async Task GetAccountAsync_Free_ReturnsZeroCapsAvailable()
        {
            var restaurantId = await SeedRestaurantAsync(
                BillingSubscriptionPlans.Free
            );

            var snapshot = await _service.GetAccountAsync(restaurantId);

            Assert.True(snapshot.Locations.Available);
            Assert.Equal(0, snapshot.Locations.Cap);
            Assert.Equal(1, snapshot.Locations.Current);
            Assert.True(snapshot.Locations.AtCap);
            Assert.True(snapshot.TeamMembers.Available);
            Assert.Equal(0, snapshot.TeamMembers.Cap);
            Assert.True(snapshot.ActiveOffers.Available);
            Assert.Equal(0, snapshot.ActiveOffers.Cap);
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

        [Fact]
        public async Task GetLocationAsync_Growth_ReportsWorkspacePoolForEveryLocation()
        {
            // B7: Workspace limit and Workspace used, the same for every Location.
            var restaurantId = await SeedRestaurantAsync(
                BillingSubscriptionPlans.Growth
            );
            var first = await _context.RestaurantLocations
                .Where(row => row.RestaurantId == restaurantId)
                .Select(row => row.Id)
                .SingleAsync();
            var second = new RestaurantLocation
            {
                RestaurantId = restaurantId,
                LocationName = "Second",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(second);
            await _context.SaveChangesAsync();
            for (var i = 0; i < 20; i++)
            {
                _context.QrCodes.Add(NewActiveCode(first));
            }

            for (var i = 0; i < 3; i++)
            {
                _context.QrCodes.Add(NewActiveCode(second.Id));
            }

            _context.QrCodes.Add(
                new QrCode
                {
                    RestaurantLocationId = second.Id,
                    QrType = QrType.TableTent,
                    Token = Guid.NewGuid().ToString("N"),
                    Status = QrCodeStatus.Paused,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            _context.ShopQrSlotReservations.Add(
                new ShopQrSlotReservation
                {
                    Id = Guid.NewGuid(),
                    ShopOrderId = Guid.NewGuid(),
                    RestaurantLocationId = first,
                    QrType = QrType.GuestCard,
                    Status = ShopQrSlotReservationStatuses.Open,
                    ReconcileAfterUtc = DateTime.UtcNow.AddMinutes(30),
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync();

            var atFirst = await _service.GetLocationAsync(restaurantId, first);
            var atSecond = await _service.GetLocationAsync(restaurantId, second.Id);

            foreach (var snapshot in new[] { atFirst, atSecond })
            {
                Assert.NotNull(snapshot);
                var qr = snapshot!.ActiveQrPlacements;
                Assert.True(qr.Available);
                Assert.Equal(25, qr.Cap);
                Assert.Equal(24, qr.Current);
                Assert.False(qr.AtCap);
            }
        }

        [Fact]
        public async Task GetLocationAsync_GroupWithPaidExtraLocations_ReportsV4Pool()
        {
            var restaurantId = await SeedRestaurantAsync(
                BillingSubscriptionPlans.Group
            );
            var account = await _context.BillingAccounts
                .SingleAsync(row => row.RestaurantId == restaurantId);
            account.PaidExtraLocationCount = 3;
            await _context.SaveChangesAsync();
            var locationId = await _context.RestaurantLocations
                .Where(row => row.RestaurantId == restaurantId)
                .Select(row => row.Id)
                .SingleAsync();

            var snapshot = await _service.GetLocationAsync(restaurantId, locationId);

            Assert.Equal(80, snapshot!.ActiveQrPlacements.Cap);
        }

        [Fact]
        public async Task GetAccountAndLocation_CustomSingleUnits_ReadTheQuotedCapacity()
        {
            // Commercial V4: the units a guest buys are the limits the account enforces.
            var catalogOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var catalogContext = new ApplicationDbContext(catalogOptions);
            var pricebook = PricebookCatalog.LoadFromDirectory(PackDirectory());
            catalogContext.CommercialCatalogVersions.Add(
                CommercialCatalogSeeder.FromSnapshot(
                    pricebook.GetRequired(pricebook.CurrentPricebookId),
                    DateTime.UtcNow
                )
            );
            await catalogContext.SaveChangesAsync();
            var quote = await new CustomPlanQuoteService(
                catalogContext,
                Options.Create(new TummlySellerVatSettings())
            ).QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = "starter",
                    Locations = 1,
                    Packs = new Dictionary<string, decimal>
                    {
                        [CustomPlanPacks.TeamUser] = 7,
                        [CustomPlanPacks.Offer] = 13,
                        [CustomPlanPacks.QrPlacement] = 2,
                    },
                }
            );

            var restaurantId = await SeedRestaurantAsync(BillingSubscriptionPlans.Custom);
            var account = await _context.BillingAccounts
                .SingleAsync(row => row.RestaurantId == restaurantId);
            account.CustomPlanConfigJson = quote.ToConfig().ToJson();
            // The verified payment has settled: the plan is Active, so the units count.
            account.BillingStatus = BillingStatuses.Active;
            await _context.SaveChangesAsync();
            var locationId = await _context.RestaurantLocations
                .Where(row => row.RestaurantId == restaurantId)
                .Select(row => row.Id)
                .SingleAsync();

            var accountSnapshot = await _service.GetAccountAsync(restaurantId);
            var locationSnapshot = await _service.GetLocationAsync(restaurantId, locationId);

            // Starter includes 3 team users, 3 Offers and 10 QR placements.
            Assert.Equal(3 + 7, accountSnapshot.TeamMembers.Cap);
            Assert.Equal(3 + 13, accountSnapshot.ActiveOffers.Cap);
            Assert.Equal(10 + 2, locationSnapshot!.ActiveQrPlacements.Cap);
        }

        [Fact]
        public async Task GetAccountAndLocation_CustomPlanBeforeSettlement_ReportsUnavailable()
        {
            var restaurantId = await SeedRestaurantAsync(BillingSubscriptionPlans.Custom);
            var account = await _context.BillingAccounts
                .SingleAsync(row => row.RestaurantId == restaurantId);
            account.CustomPlanConfigJson = new CustomPlanConfig
            {
                Version = 2,
                BasePlan = "starter",
                Cadence = "monthly",
                Locations = 1,
                Quantities = new CustomPlanQuantities
                {
                    Locations = 1,
                    TeamUsers = 503,
                    Offers = 503,
                    QrPlacements = 10,
                    QrPlacementsAccountExtra = 500,
                },
                MonthlyNetPence = 3900,
                PeriodNetPence = 3900,
            }.ToJson();
            Assert.Equal(BillingStatuses.Free, account.BillingStatus);
            await _context.SaveChangesAsync();
            var locationId = await _context.RestaurantLocations
                .Where(row => row.RestaurantId == restaurantId)
                .Select(row => row.Id)
                .SingleAsync();

            var accountSnapshot = await _service.GetAccountAsync(restaurantId);
            var locationSnapshot = await _service.GetLocationAsync(restaurantId, locationId);

            Assert.False(accountSnapshot.TeamMembers.Available);
            Assert.False(accountSnapshot.ActiveOffers.Available);
            Assert.False(locationSnapshot!.ActiveQrPlacements.Available);
        }

        /// <summary>
        /// ADM-04-03: Group Offers match the gate at 5, 6 and 7 Locations.
        /// </summary>
        [Theory]
        [InlineData(0, 25)]
        [InlineData(1, 28)]
        [InlineData(2, 31)]
        public async Task GetAccountAsync_GroupWithPaidExtraLocations_AddsThreeOffersEach(
            int paidExtraLocations,
            int expectedCap
        )
        {
            var restaurantId = await SeedRestaurantAsync(
                BillingSubscriptionPlans.Group
            );
            var account = await _context.BillingAccounts
                .SingleAsync(row => row.RestaurantId == restaurantId);
            account.PaidExtraLocationCount = paidExtraLocations;
            await _context.SaveChangesAsync();

            var snapshot = await _service.GetAccountAsync(restaurantId);

            Assert.True(snapshot.ActiveOffers.Available);
            Assert.Equal(expectedCap, snapshot.ActiveOffers.Cap);
        }

        private static QrCode NewActiveCode(int locationId) =>
            new()
            {
                RestaurantLocationId = locationId,
                QrType = QrType.DigitalGuestLink,
                Token = Guid.NewGuid().ToString("N"),
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };

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
