using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Billing;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class TeamMemberCapGateTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TeamMemberCapGate _gate;

        public TeamMemberCapGateTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            _context = new ApplicationDbContext(options);
            _gate = new TeamMemberCapGate(
                _context,
                PricebookCatalog.LoadFromDirectory(PackDirectory())
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task DenyIncrement_CountsAccountOwner()
        {
            var restaurantId = await SeedRestaurantAsync(
                BillingSubscriptionPlans.Pilot,
                includeOwnerMembership: true
            );

            var decision = await _gate.DenyIncrementAsync(restaurantId);

            Assert.True(decision.AllowIncrement);
            Assert.Equal(1, decision.Current);
            Assert.Equal(2, decision.Cap);
        }

        [Fact]
        public async Task DenyIncrement_DoesNotCountDeactivatedMembership()
        {
            var restaurantId = await SeedRestaurantAsync(
                BillingSubscriptionPlans.Pilot,
                includeOwnerMembership: true
            );
            var staff = new User
            {
                FullName = "Staff",
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900112",
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(staff);
            await _context.SaveChangesAsync();
            _context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = staff.Id,
                    RestaurantId = restaurantId,
                    PermissionRole = PermissionRoles.Staff,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Deactivated,
                }
            );
            await _context.SaveChangesAsync();

            var decision = await _gate.DenyIncrementAsync(restaurantId);

            Assert.True(decision.AllowIncrement);
            Assert.Equal(1, decision.Current);
            Assert.Equal(2, decision.Cap);
        }

        [Fact]
        public async Task DenyIncrement_AddsTwoTeamMembersPerPaidExtraLocation()
        {
            var restaurantId = await SeedRestaurantAsync(
                BillingSubscriptionPlans.Group,
                includeOwnerMembership: true,
                paidExtraLocationCount: 1
            );

            var decision = await _gate.DenyIncrementAsync(restaurantId);

            Assert.True(decision.AllowIncrement);
            Assert.Equal(1, decision.Current);
            Assert.Equal(27, decision.Cap);
        }

        private async Task<int> SeedRestaurantAsync(
            string subscriptionPlan,
            bool includeOwnerMembership,
            int paidExtraLocationCount = 0
        )
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
                Name = "Cap Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var account = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                "TUMMLY-UK-GBP-2026-08-V3"
            );
            account.SubscriptionPlan = subscriptionPlan;
            account.PaidExtraLocationCount = paidExtraLocationCount;
            _context.BillingAccounts.Add(account);

            if (includeOwnerMembership)
            {
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
            }

            await _context.SaveChangesAsync();
            return restaurant.Id;
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
    }
}
