using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Helpers;
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
        public async Task Import_DifferentNameAtSamePremises_WarnsGeneric_KeepsDraftPendingReview()
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

            var completed = Assert.IsType<ImportOwnedLocationsResult.Completed>(result);
            var created = Assert.Single(completed.Created);
            AssertGenericWarning(created.Warning);
            Assert.Null(created.Warning!.MatchedRowIndex);

            var location = await _context.RestaurantLocations
                .SingleAsync(row => row.Id == created.LocationId);
            Assert.Equal(LocationLifecycleStatus.Draft, location.LifecycleStatus);
            Assert.Equal(PremisesReviewStatus.PendingReview, location.PremisesReviewStatus);
            Assert.False(string.IsNullOrEmpty(location.PremisesKey));
        }

        [Fact]
        public async Task Add_SinglePath_RunsTheSameCheck_AndSavesPendingReview()
        {
            AddExistingLocation(
                _restaurantId,
                "Old Name",
                "10 Wardour Street",
                "W1D 6QF",
                LocationLifecycleStatus.Active
            );

            var result = await _insert.AddAsync(
                _restaurantId,
                1,
                Row("New Name", "10 Wardour St", "W1D 6QF")
            );

            var created = Assert.IsType<AddOwnedLocationResult.Created>(result);
            AssertGenericWarning(created.PremisesWarning);
            var location = await _context.RestaurantLocations
                .SingleAsync(row => row.Id == created.LocationId);
            Assert.Equal(LocationLifecycleStatus.Draft, location.LifecycleStatus);
            Assert.Equal(PremisesReviewStatus.PendingReview, location.PremisesReviewStatus);
        }

        [Fact]
        public async Task Add_SameNameDifferentAddress_NotFlagged()
        {
            AddExistingLocation(
                _restaurantId,
                "Soho",
                "10 Wardour Street",
                "W1D 6QF",
                LocationLifecycleStatus.Active
            );

            var result = await _insert.AddAsync(
                _restaurantId,
                1,
                Row("Soho", "1 Curtain Road", "EC2A 3NZ")
            );

            var created = Assert.IsType<AddOwnedLocationResult.Created>(result);
            Assert.Null(created.PremisesWarning);
            var location = await _context.RestaurantLocations
                .SingleAsync(row => row.Id == created.LocationId);
            Assert.Equal(PremisesReviewStatus.None, location.PremisesReviewStatus);
        }

        [Fact]
        public async Task Add_FlaggedLocation_MintsNoQrCodes_UnflaggedLocationDoes()
        {
            AddExistingLocation(
                _restaurantId,
                "Old Name",
                "10 Wardour Street",
                "W1D 6QF",
                LocationLifecycleStatus.Active
            );

            var flagged = Assert.IsType<AddOwnedLocationResult.Created>(
                await _insert.AddAsync(
                    _restaurantId,
                    1,
                    Row("New Name", "10 Wardour St", "W1D 6QF")
                )
            );
            var clean = Assert.IsType<AddOwnedLocationResult.Created>(
                await _insert.AddAsync(
                    _restaurantId,
                    1,
                    Row("Clean", "1 Curtain Road", "EC2A 3NZ")
                )
            );

            Assert.Equal(
                0,
                await _context.QrCodes.CountAsync(qr =>
                    qr.RestaurantLocationId == flagged.LocationId
                )
            );
            Assert.True(
                await _context.QrCodes.CountAsync(qr =>
                    qr.RestaurantLocationId == clean.LocationId
                ) > 0
            );
        }

        [Fact]
        public async Task Add_Flagged_WritesSystemAuditRow_WithDetailForAdminOnly()
        {
            var existingId = AddExistingLocation(
                _otherRestaurantId,
                "Their Venue",
                "10 Wardour Street",
                "W1D 6QF",
                LocationLifecycleStatus.Active
            );

            var created = Assert.IsType<AddOwnedLocationResult.Created>(
                await _insert.AddAsync(
                    _restaurantId,
                    1,
                    Row("Ours", "10 Wardour Street", "W1D 6QF")
                )
            );

            var audit = await _context.AdminAuditEvents.SingleAsync(row =>
                row.Action == AdminAuditActions.LocationPremisesFlagged
            );
            Assert.Equal(created.LocationId.ToString(), audit.TargetId);
            Assert.Equal(AdminAuditTargetTypes.RestaurantLocation, audit.TargetType);
            Assert.Equal(_restaurantId, audit.RestaurantId);
            Assert.Contains($"\"matchedLocationId\":{existingId}", audit.DetailJson);
            Assert.Contains($"\"matchedRestaurantId\":{_otherRestaurantId}", audit.DetailJson);
        }

        [Fact]
        public async Task Add_FsaEstablishmentMatch_FlagsEvenAtDifferentAddress()
        {
            AddExistingLocation(
                _otherRestaurantId,
                "Their Venue",
                "10 Wardour Street",
                "W1D 6QF",
                LocationLifecycleStatus.Active,
                fsaEstablishmentId: "123456"
            );

            var result = await _insert.AddAsync(
                _restaurantId,
                1,
                Row("Ours", "1 Curtain Road", "EC2A 3NZ", fsaEstablishmentId: " 123456 ")
            );

            var created = Assert.IsType<AddOwnedLocationResult.Created>(result);
            AssertGenericWarning(created.PremisesWarning);
            var location = await _context.RestaurantLocations
                .SingleAsync(row => row.Id == created.LocationId);
            Assert.Equal("123456", location.FsaEstablishmentId);
            Assert.Equal(PremisesReviewStatus.PendingReview, location.PremisesReviewStatus);
        }

        [Fact]
        public async Task Add_FsaOnOneSideOnly_DifferentAddress_NotFlagged()
        {
            AddExistingLocation(
                _otherRestaurantId,
                "Their Venue",
                "10 Wardour Street",
                "W1D 6QF",
                LocationLifecycleStatus.Active
            );

            var result = await _insert.AddAsync(
                _restaurantId,
                1,
                Row("Ours", "1 Curtain Road", "EC2A 3NZ", fsaEstablishmentId: "123456")
            );

            var created = Assert.IsType<AddOwnedLocationResult.Created>(result);
            Assert.Null(created.PremisesWarning);
        }

        [Fact]
        public async Task Add_PremisesOfAWorkspaceWithAPriorPilot_FlagsAndNotesEntitlementForAdmin()
        {
            var billing = await _context.BillingAccounts
                .SingleAsync(row => row.RestaurantId == _otherRestaurantId);
            billing.PilotStartedAtUtc = DateTime.UtcNow.AddDays(-40);
            await _context.SaveChangesAsync();
            AddExistingLocation(
                _otherRestaurantId,
                "Pilot Venue",
                "10 Wardour Street",
                "W1D 6QF",
                LocationLifecycleStatus.Archived
            );

            var result = await _insert.AddAsync(
                _restaurantId,
                1,
                Row("Fresh Start", "10 Wardour Street", "W1D 6QF")
            );

            var created = Assert.IsType<AddOwnedLocationResult.Created>(result);
            AssertGenericWarning(created.PremisesWarning);
            var audit = await _context.AdminAuditEvents.SingleAsync(row =>
                row.Action == AdminAuditActions.LocationPremisesFlagged
            );
            Assert.Contains("\"priorPilot\":true", audit.DetailJson);
        }

        [Fact]
        public async Task Add_PremisesThatAlreadyHadAStarterKit_FlagsEvenWhenTheLocationWasEditedAway()
        {
            var oldId = AddExistingLocation(
                _otherRestaurantId,
                "Kit Venue",
                "10 Wardour Street",
                "W1D 6QF",
                LocationLifecycleStatus.Active
            );
            var kit = await _complimentary.EnsureForLocationAsync(
                _otherRestaurantId,
                oldId,
                1,
                "Owner"
            );
            Assert.NotEqual(Guid.Empty, kit.ShopOrderId);

            var old = await _context.RestaurantLocations.SingleAsync(row => row.Id == oldId);
            old.Address = "99 Somewhere Else Road";
            old.Postcode = "N1 1AA";
            old.PremisesKey = null;
            await _context.SaveChangesAsync();

            var result = await _insert.AddAsync(
                _restaurantId,
                1,
                Row("Fresh Start", "10 Wardour Street", "W1D 6QF")
            );

            var created = Assert.IsType<AddOwnedLocationResult.Created>(result);
            AssertGenericWarning(created.PremisesWarning);
            var audit = await _context.AdminAuditEvents.SingleAsync(row =>
                row.Action == AdminAuditActions.LocationPremisesFlagged
            );
            Assert.Contains("\"priorStarterKit\":true", audit.DetailJson);
        }

        [Fact]
        public async Task Add_LegacyRowWithoutPersistedKey_IsMatchedAndBackfilled()
        {
            var legacyId = AddExistingLocation(
                _otherRestaurantId,
                "Legacy",
                "10 Wardour Street",
                "W1D 6QF",
                LocationLifecycleStatus.Active
            );
            var legacy = await _context.RestaurantLocations.SingleAsync(row => row.Id == legacyId);
            legacy.PremisesKey = null;
            await _context.SaveChangesAsync();

            var result = await _insert.AddAsync(
                _restaurantId,
                1,
                Row("Ours", "10 Wardour St", "W1D 6QF")
            );

            AssertGenericWarning(
                Assert.IsType<AddOwnedLocationResult.Created>(result).PremisesWarning
            );
            _context.ChangeTracker.Clear();
            var reloaded = await _context.RestaurantLocations.SingleAsync(row => row.Id == legacyId);
            Assert.False(string.IsNullOrEmpty(reloaded.PremisesKey));
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
        public async Task Import_DuplicateWithinFile_WarnsLaterRowOnly_AndKeepsBothDraft_LaterPendingReview()
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
            AssertGenericWarning(warning);
            Assert.Equal(0, warning!.MatchedRowIndex);

            var statuses = await _context.RestaurantLocations
                .Where(row => row.RestaurantId == _restaurantId)
                .Select(row => row.LifecycleStatus)
                .ToListAsync();
            Assert.Equal(2, statuses.Count);
            Assert.All(statuses, status => Assert.Equal(LocationLifecycleStatus.Draft, status));
            Assert.Equal(
                PremisesReviewStatus.None,
                (await _context.RestaurantLocations.SingleAsync(row => row.Id == completed.Created[0].LocationId)).PremisesReviewStatus
            );
            Assert.Equal(
                PremisesReviewStatus.PendingReview,
                (await _context.RestaurantLocations.SingleAsync(row => row.Id == completed.Created[1].LocationId)).PremisesReviewStatus
            );
        }

        [Fact]
        public async Task Import_FsaColumn_FlagsSecondRowInTheSameFile()
        {
            var result = await _insert.ImportAsync(
                _restaurantId,
                1,
                Request(
                    Row("One", "10 Wardour Street", "W1D 6QF", fsaEstablishmentId: "777"),
                    Row("Two", "1 Curtain Road", "EC2A 3NZ", fsaEstablishmentId: "777")
                )
            );

            var completed = Assert.IsType<ImportOwnedLocationsResult.Completed>(result);
            Assert.Null(completed.Created[0].Warning);
            AssertGenericWarning(completed.Created[1].Warning);
            Assert.Equal(0, completed.Created[1].Warning!.MatchedRowIndex);
        }

        [Fact]
        public async Task Import_OtherWorkspaceMatch_ReturnsTheSameGenericWarning_WithoutTenantData()
        {
            AddExistingLocation(
                _otherRestaurantId,
                "Their Venue",
                "10 Wardour Street",
                "W1D 6QF",
                LocationLifecycleStatus.Active
            );
            AddExistingLocation(
                _restaurantId,
                "Own Venue",
                "1 Curtain Road",
                "EC2A 3NZ",
                LocationLifecycleStatus.Active
            );

            var result = await _insert.ImportAsync(
                _restaurantId,
                1,
                Request(
                    Row("Soho", "10 Wardour Street", "W1D 6QF"),
                    Row("Own Again", "1 Curtain Road", "EC2A 3NZ")
                )
            );

            var completed = Assert.IsType<ImportOwnedLocationsResult.Completed>(result);
            Assert.Equal(2, completed.Created.Count);
            AssertGenericWarning(completed.Created[0].Warning);
            AssertGenericWarning(completed.Created[1].Warning);
            // Other-Workspace and own-Workspace matches look the same to the caller.
            Assert.Equal(completed.Created[0].Warning, completed.Created[1].Warning);
        }

        [Fact]
        public void Warning_Contract_HasNoScopeIdOrNameMembers()
        {
            var members = typeof(PremisesDuplicateWarning)
                .GetProperties()
                .Select(property => property.Name)
                .OrderBy(name => name)
                .ToArray();

            Assert.Equal(new[] { "Code", "MatchedRowIndex", "Message" }, members);
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
            string postcode,
            string? fsaEstablishmentId = null
        )
        {
            return new AddOwnedLocationRequest
            {
                LocationName = name,
                Address = address,
                City = "London",
                Postcode = postcode,
                FsaEstablishmentId = fsaEstablishmentId,
            };
        }

        private static void AssertGenericWarning(PremisesDuplicateWarning? warning)
        {
            Assert.NotNull(warning);
            Assert.Equal(PossibleDuplicatePremises.Code, warning!.Code);
            Assert.Equal(
                "Possible duplicate premises. Saved as Draft for review.",
                warning.Message
            );
        }

        private int AddExistingLocation(
            int restaurantId,
            string name,
            string address,
            string postcode,
            LocationLifecycleStatus status,
            string? fsaEstablishmentId = null
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
                FsaEstablishmentId = fsaEstablishmentId,
                PremisesKey = PremisesKey.Build(address, "London", postcode),
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
