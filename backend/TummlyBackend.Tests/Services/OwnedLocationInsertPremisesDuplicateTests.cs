using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Shop.MaterialsCatalog;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Import duplicate warning: a row whose premises match another row is
    /// still created, but stays Draft and gets no complimentary order.
    /// </summary>
    public class OwnedLocationInsertPremisesDuplicateTests : IDisposable
    {
        private const string CurrentPricebookId = "TUMMLY-UK-GBP-2026-08-V3";

        private readonly ApplicationDbContext _context;
        private readonly OwnedLocationInsertService _insert;
        private readonly ComplimentaryStarterShopOrderService _complimentary;
        private int _restaurantId;
        private int _otherRestaurantId;

        public OwnedLocationInsertPremisesDuplicateTests()
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

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://tummly.example",
                })
                .Build();
            var qrProvisioning = new QrCodeProvisioningService(
                _context,
                new SmartGuestLinkService(
                    _context,
                    configuration,
                    new NoOpBillingAccountLifecycle()
                ),
                TestActiveQrCapacity.For(_context)
            );

            _insert = new OwnedLocationInsertService(
                _context,
                TestPricebookPaths.LoadV3(),
                qrProvisioning,
                NoOpPrintReadyQrMaterialsWork.Instance
            );
            _complimentary = new ComplimentaryStarterShopOrderService(
                _context,
                new DbMaterialsCatalog(_context),
                new ShopOrderNumberAllocator(_context)
            );

            SeedRestaurants();
        }

        [Fact]
        public async Task Import_SameNameAtDifferentAddresses_NoWarning()
        {
            var result = await _insert.ImportAsync(
                _restaurantId,
                1,
                Request(
                    Row("Soho", "10 Wardour Street", "W1D 6QF"),
                    Row("Soho", "1 Curtain Road", "EC2A 3NZ")
                )
            );

            var completed = Assert.IsType<ImportOwnedLocationsResult.Completed>(result);
            Assert.Equal(2, completed.Created.Count);
            Assert.All(completed.Created, row => Assert.Null(row.Warning));
            Assert.Empty(completed.Errors);
        }

        [Fact]
        public async Task Import_DifferentNameAtSamePremises_WarnsWithMatchedLocation_AndKeepsDraft()
        {
            var existingId = AddExistingLocation(
                _restaurantId,
                "Old Name",
                "10 Wardour Street",
                "W1D 6QF",
                LocationLifecycleStatus.Active
            );

            var result = await _insert.ImportAsync(
                _restaurantId,
                1,
                Request(Row("New Name", "10 Wardour St", "W1D 6QF"))
            );

            var completed = Assert.IsType<ImportOwnedLocationsResult.Completed>(result);
            var created = Assert.Single(completed.Created);
            Assert.NotNull(created.Warning);
            Assert.Equal(PossibleDuplicatePremises.Code, created.Warning!.Code);
            Assert.Equal(PossibleDuplicatePremises.ScopeSameWorkspace, created.Warning.Scope);
            Assert.Equal(existingId, created.Warning.MatchedLocationId);
            Assert.Equal("Old Name", created.Warning.MatchedLocationName);

            var location = await _context.RestaurantLocations
                .SingleAsync(row => row.Id == created.LocationId);
            Assert.Equal(LocationLifecycleStatus.Draft, location.LifecycleStatus);
        }

        [Fact]
        public async Task Import_SamePostcodeDifferentUnit_NoWarning()
        {
            AddExistingLocation(
                _restaurantId,
                "Unit Three",
                "Unit 3, 10 Wardour Street",
                "W1D 6QF",
                LocationLifecycleStatus.Active
            );

            var result = await _insert.ImportAsync(
                _restaurantId,
                1,
                Request(Row("Unit Four", "Unit 4, 10 Wardour Street", "W1D 6QF"))
            );

            var completed = Assert.IsType<ImportOwnedLocationsResult.Completed>(result);
            var created = Assert.Single(completed.Created);
            Assert.Null(created.Warning);
        }

        [Fact]
        public async Task Import_DuplicateWithinFile_WarnsLaterRowOnly_AndKeepsBothDraft()
        {
            var result = await _insert.ImportAsync(
                _restaurantId,
                1,
                Request(
                    Row("Soho", "10 Wardour Street", "W1D 6QF"),
                    Row("Soho Two", "10 Wardour St", "W1D 6QF")
                )
            );

            var completed = Assert.IsType<ImportOwnedLocationsResult.Completed>(result);
            Assert.Equal(2, completed.Created.Count);
            Assert.Null(completed.Created[0].Warning);

            var warning = completed.Created[1].Warning;
            Assert.NotNull(warning);
            Assert.Equal(PossibleDuplicatePremises.ScopeSameFile, warning!.Scope);
            Assert.Equal(0, warning.MatchedRowIndex);
            Assert.Null(warning.MatchedLocationId);

            var statuses = await _context.RestaurantLocations
                .Where(row => row.RestaurantId == _restaurantId)
                .Select(row => row.LifecycleStatus)
                .ToListAsync();
            Assert.Equal(2, statuses.Count);
            Assert.All(statuses, status => Assert.Equal(LocationLifecycleStatus.Draft, status));
        }

        [Fact]
        public async Task Import_OtherWorkspaceMatch_ReturnsGenericWarning_WithoutTenantData()
        {
            AddExistingLocation(
                _otherRestaurantId,
                "Their Venue",
                "10 Wardour Street",
                "W1D 6QF",
                LocationLifecycleStatus.Active
            );

            var result = await _insert.ImportAsync(
                _restaurantId,
                1,
                Request(Row("Soho", "10 Wardour Street", "W1D 6QF"))
            );

            var completed = Assert.IsType<ImportOwnedLocationsResult.Completed>(result);
            var created = Assert.Single(completed.Created);
            var warning = created.Warning;
            Assert.NotNull(warning);
            Assert.Equal(PossibleDuplicatePremises.Code, warning!.Code);
            Assert.Equal(PossibleDuplicatePremises.ScopeOtherWorkspace, warning.Scope);
            Assert.Null(warning.MatchedLocationId);
            Assert.Null(warning.MatchedLocationName);
            Assert.Null(warning.MatchedRowIndex);
        }

        [Fact]
        public async Task Import_DraftDuplicate_StartsNoComplimentaryOrder()
        {
            AddExistingLocation(
                _restaurantId,
                "Old Name",
                "10 Wardour Street",
                "W1D 6QF",
                LocationLifecycleStatus.Active
            );

            var result = await _insert.ImportAsync(
                _restaurantId,
                1,
                Request(Row("New Name", "10 Wardour St", "W1D 6QF"))
            );

            var created = Assert.Single(
                Assert.IsType<ImportOwnedLocationsResult.Completed>(result).Created
            );
            Assert.NotNull(created.Warning);
            Assert.Equal(0, await _context.ShopOrders.CountAsync(order => order.IsComplimentary));

            var ensure = await _complimentary.EnsureForLocationAsync(
                _restaurantId,
                created.LocationId,
                1,
                "Operator One"
            );
            Assert.Equal(Guid.Empty, ensure.ShopOrderId);
            Assert.Equal(0, await _context.ShopOrders.CountAsync(order => order.IsComplimentary));
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private static ImportOwnedLocationsRequest Request(
            params AddOwnedLocationRequest[] rows
        )
        {
            return new ImportOwnedLocationsRequest { Rows = rows.ToList() };
        }

        private static AddOwnedLocationRequest Row(
            string name,
            string address,
            string postcode
        )
        {
            return new AddOwnedLocationRequest
            {
                LocationName = name,
                Address = address,
                City = "London",
                Postcode = postcode,
            };
        }

        private int AddExistingLocation(
            int restaurantId,
            string name,
            string address,
            string postcode,
            LocationLifecycleStatus status
        )
        {
            var location = new RestaurantLocation
            {
                RestaurantId = restaurantId,
                LocationName = name,
                Address = address,
                City = "London",
                Postcode = postcode,
                LifecycleStatus = status,
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            _context.SaveChanges();
            return location.Id;
        }

        private void SeedRestaurants()
        {
            _restaurantId = SeedRestaurant("Import Duplicate Venue");
            _otherRestaurantId = SeedRestaurant("Other Workspace Venue");
        }

        private int SeedRestaurant(string name)
        {
            var user = new User
            {
                FullName = $"Owner {name}",
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var restaurant = new Restaurant
            {
                Name = name,
                AccountType = "Multi",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            _context.SaveChanges();

            var billing = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                CurrentPricebookId
            );
            billing.SubscriptionPlan = BillingSubscriptionPlans.Group;
            _context.BillingAccounts.Add(billing);
            _context.SaveChanges();

            return restaurant.Id;
        }
    }
}
