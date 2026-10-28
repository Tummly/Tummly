using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class AdminAccountDirectoryServiceTests : IDisposable
    {
        private const string Pricebook = "TUMMLY-UK-GBP-2026-08-V3";

        private readonly ApplicationDbContext _context;
        private readonly AdminAccountDirectoryService _service;

        public AdminAccountDirectoryServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new AdminAccountDirectoryService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task List_ReturnsNewestFirst_WithTotalCount_AndPages()
        {
            var oldest = SeedWorkspace("Oldest Cafe", DateTime.UtcNow.AddDays(-30));
            var middle = SeedWorkspace("Middle Bistro", DateTime.UtcNow.AddDays(-20));
            var newest = SeedWorkspace("Newest Diner", DateTime.UtcNow.AddDays(-10));
            await _context.SaveChangesAsync();

            var firstPage = await _service.ListAsync(new AdminAccountListQuery(Take: 2));
            var secondPage = await _service.ListAsync(new AdminAccountListQuery(Skip: 2, Take: 2));

            Assert.Equal(3, firstPage.TotalCount);
            Assert.Equal(new[] { newest, middle }, firstPage.Items.Select(row => row.RestaurantId));
            Assert.Equal(new[] { oldest }, secondPage.Items.Select(row => row.RestaurantId));
            Assert.Equal(3, secondPage.TotalCount);
        }

        [Fact]
        public async Task List_TakeAbove100_IsCappedAt100()
        {
            for (var index = 0; index < 101; index++)
            {
                SeedWorkspace($"Venue {index}", DateTime.UtcNow.AddMinutes(-index));
            }
            await _context.SaveChangesAsync();

            var page = await _service.ListAsync(new AdminAccountListQuery(Take: 500));

            Assert.Equal(101, page.TotalCount);
            Assert.Equal(100, page.Items.Count);
            Assert.Equal(100, page.Take);
        }

        [Fact]
        public async Task Search_WorkspaceIdExact_ReturnsOnlyThatWorkspace()
        {
            var target = SeedWorkspace("Target Venue", DateTime.UtcNow);
            SeedWorkspace("Other Venue", DateTime.UtcNow.AddDays(-1));
            await _context.SaveChangesAsync();

            var page = await _service.ListAsync(
                new AdminAccountListQuery(Search: target.ToString())
            );

            var row = Assert.Single(page.Items);
            Assert.Equal(target, row.RestaurantId);
        }

        [Fact]
        public async Task Search_NameContains_MatchesNameTradingNameAndLegalName()
        {
            var byName = SeedWorkspace("Harbour Kitchen", DateTime.UtcNow);
            var byTrading = SeedWorkspace("Plain Name", DateTime.UtcNow, trading: "Harbour Grill");
            var byLegal = SeedWorkspace("Other Name", DateTime.UtcNow, legal: "Harbour Holdings Ltd");
            SeedWorkspace("Unrelated Venue", DateTime.UtcNow);
            await _context.SaveChangesAsync();

            var page = await _service.ListAsync(new AdminAccountListQuery(Search: "Harbour"));

            Assert.Equal(3, page.TotalCount);
            Assert.Equal(
                new[] { byName, byTrading, byLegal }.OrderByDescending(id => id),
                page.Items.Select(row => row.RestaurantId)
            );
        }

        [Fact]
        public async Task Filter_PlanAndBillingStatus_BothApply()
        {
            var match = SeedWorkspace("Growth Live", DateTime.UtcNow, plan: "Growth", status: "Active");
            SeedWorkspace("Growth Past Due", DateTime.UtcNow, plan: "Growth", status: "Past due");
            SeedWorkspace("Pilot Live", DateTime.UtcNow, plan: "Pilot", status: "Active");
            await _context.SaveChangesAsync();

            var page = await _service.ListAsync(new AdminAccountListQuery(
                SubscriptionPlan: "Growth",
                BillingStatus: "Active"
            ));

            var row = Assert.Single(page.Items);
            Assert.Equal(match, row.RestaurantId);
        }

        [Fact]
        public async Task Filter_CreatedRange_IsInclusiveFromAndExclusiveBefore()
        {
            var day = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
            var onFrom = SeedWorkspace("On From", day);
            var onBefore = SeedWorkspace("On Before", day.AddDays(5));
            SeedWorkspace("Earlier", day.AddDays(-1));
            await _context.SaveChangesAsync();

            var page = await _service.ListAsync(new AdminAccountListQuery(
                CreatedFrom: day,
                CreatedBefore: day.AddDays(5)
            ));

            var row = Assert.Single(page.Items);
            Assert.Equal(onFrom, row.RestaurantId);
            Assert.DoesNotContain(onBefore, page.Items.Select(item => item.RestaurantId));
        }

        [Fact]
        public async Task Filter_CreatedFromAfterBefore_Throws()
        {
            var day = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ListAsync(new AdminAccountListQuery(
                    CreatedFrom: day,
                    CreatedBefore: day
                ))
            );
        }

        [Fact]
        public async Task Row_ShowsActiveLocationsOnly_OwnerName_AndPeriodEnd()
        {
            var pilotEnd = DateTime.UtcNow.AddDays(9);
            var pilot = SeedWorkspace(
                "Pilot Venue",
                DateTime.UtcNow,
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.Pilot,
                pilotEnd: pilotEnd,
                ownerName: "Ada Owner"
            );
            AddLocation(pilot, LocationLifecycleStatus.Active);
            AddLocation(pilot, LocationLifecycleStatus.Active);
            AddLocation(pilot, LocationLifecycleStatus.Draft);
            AddLocation(pilot, LocationLifecycleStatus.Paused);
            var renewal = DateTime.UtcNow.AddDays(20);
            var paid = SeedWorkspace(
                "Paid Venue",
                DateTime.UtcNow.AddDays(-1),
                plan: BillingSubscriptionPlans.Growth,
                status: BillingStatuses.Active,
                renewal: renewal
            );
            await _context.SaveChangesAsync();

            var page = await _service.ListAsync(new AdminAccountListQuery());

            var pilotRow = page.Items.Single(row => row.RestaurantId == pilot);
            Assert.Equal("Pilot Venue", pilotRow.DisplayName);
            Assert.Equal(2, pilotRow.ActiveLocationCount);
            Assert.Equal("Ada Owner", pilotRow.OwnerDisplayName);
            Assert.Equal(pilotEnd, pilotRow.PeriodEndUtc);

            var paidRow = page.Items.Single(row => row.RestaurantId == paid);
            Assert.Equal(0, paidRow.ActiveLocationCount);
            Assert.Equal(renewal, paidRow.PeriodEndUtc);
        }

        [Fact]
        public async Task List_DoesNotLeakOtherWorkspaceRows_WhenSearchingOneWorkspace()
        {
            var target = SeedWorkspace("Isolated Venue", DateTime.UtcNow);
            var other = SeedWorkspace("Neighbour Venue", DateTime.UtcNow);
            AddLocation(other, LocationLifecycleStatus.Active);
            await _context.SaveChangesAsync();

            var page = await _service.ListAsync(
                new AdminAccountListQuery(Search: target.ToString())
            );

            Assert.DoesNotContain(page.Items, row => row.RestaurantId == other);
            Assert.Equal(1, page.TotalCount);
        }

        private int SeedWorkspace(
            string name,
            DateTime createdAt,
            string plan = BillingSubscriptionPlans.Pilot,
            string status = BillingStatuses.Pilot,
            DateTime? pilotEnd = null,
            DateTime? renewal = null,
            string? trading = null,
            string? legal = null,
            string ownerName = "Owner Person"
        )
        {
            var owner = new User
            {
                FullName = ownerName,
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                Role = "Owner",
                PhoneNumber = "07700900111",
                AccountType = "Single",
                CreatedAt = createdAt,
            };
            _context.Users.Add(owner);
            _context.SaveChanges();

            var restaurant = new Restaurant
            {
                Name = name,
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = createdAt,
                BillingAccount = new BillingAccount
                {
                    SubscriptionPlan = plan,
                    BillingStatus = status,
                    ContractedPricebookId = Pricebook,
                    PilotPeriodEnd = pilotEnd,
                    RenewalDateUtc = renewal,
                },
            };
            if (trading != null || legal != null)
            {
                restaurant.BusinessDetails = new RestaurantBusinessDetails
                {
                    TradingName = trading,
                    LegalBusinessName = legal,
                };
            }
            _context.Restaurants.Add(restaurant);
            _context.SaveChanges();

            return restaurant.Id;
        }

        private void AddLocation(int restaurantId, LocationLifecycleStatus status)
        {
            _context.RestaurantLocations.Add(new RestaurantLocation
            {
                RestaurantId = restaurantId,
                LocationName = $"Site {Guid.NewGuid():N}",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
                LifecycleStatus = status,
            });
            _context.SaveChanges();
        }
    }
}
