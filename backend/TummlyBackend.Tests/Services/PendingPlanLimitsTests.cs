using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Billing;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// A scheduled downgrade caps new locations, activations and team seats
    /// at the lower of the live and target plan; only Active locations count
    /// toward the downgrade / activation limit.
    /// </summary>
    public class PendingPlanLimitsTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IPricebookCatalog _pricebook;
        private readonly OwnedLocationInsertService _insert;
        private readonly TeamMemberCapGate _teamGate;

        public PendingPlanLimitsTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            _context = new ApplicationDbContext(options);
            _pricebook = TestPricebookPaths.LoadV3();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://tummly.example",
                    }
                )
                .Build();
            var smartGuestLink = new SmartGuestLinkService(
                _context,
                configuration,
                new NoOpBillingAccountLifecycle()
            );
            _insert = new OwnedLocationInsertService(
                _context,
                _pricebook,
                new QrCodeProvisioningService(_context, smartGuestLink,
                TestActiveQrCapacity.For(_context)
            ),
                NoOpPrintReadyQrMaterialsWork.Instance
            );
            _teamGate = new TeamMemberCapGate(_context, _pricebook);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task Insert_ScheduledDowngrade_UsesTargetLocationCap()
        {
            var (restaurantId, userId) = await SeedAsync(
                BillingSubscriptionPlans.Growth,
                activeLocations: 1,
                scheduledTarget: BillingSubscriptionPlans.Starter
            );

            var result = await _insert.AddAsync(restaurantId, userId, NewRequest());

            var cap = Assert.IsType<AddOwnedLocationResult.CapReached>(result);
            Assert.Equal(1, cap.Cap);
            Assert.Equal(1, cap.Current);
            Assert.NotNull(cap.Message);
            Assert.Contains("Cancel the scheduled change", cap.Message);
        }

        [Fact]
        public async Task Insert_NoSchedule_IgnoresPausedAndArchived()
        {
            var (restaurantId, userId) = await SeedAsync(
                BillingSubscriptionPlans.Growth,
                activeLocations: 2,
                pausedLocations: 1,
                archivedLocations: 1
            );

            var result = await _insert.AddAsync(restaurantId, userId, NewRequest());

            Assert.IsType<AddOwnedLocationResult.Created>(result);
        }

        [Fact]
        public async Task DenyActivation_ScheduledDowngrade_Denies()
        {
            var (restaurantId, _) = await SeedAsync(
                BillingSubscriptionPlans.Growth,
                activeLocations: 1,
                pausedLocations: 1,
                scheduledTarget: BillingSubscriptionPlans.Starter
            );

            var deny = await PendingPlanLimits.DenyActivationAsync(
                _context,
                _pricebook,
                restaurantId
            );

            Assert.NotNull(deny);
            Assert.Contains("Cancel the scheduled change", deny);
        }

        [Fact]
        public async Task DenyActivation_UnderLiveCap_Allows()
        {
            var (restaurantId, _) = await SeedAsync(
                BillingSubscriptionPlans.Growth,
                activeLocations: 2,
                pausedLocations: 1
            );

            var deny = await PendingPlanLimits.DenyActivationAsync(
                _context,
                _pricebook,
                restaurantId
            );

            Assert.Null(deny);
        }

        [Fact]
        public async Task DenyActivation_AtLiveCap_Denies()
        {
            var (restaurantId, _) = await SeedAsync(
                BillingSubscriptionPlans.Growth,
                activeLocations: 3,
                pausedLocations: 1
            );

            var deny = await PendingPlanLimits.DenyActivationAsync(
                _context,
                _pricebook,
                restaurantId
            );

            Assert.NotNull(deny);
            Assert.Contains("3 of 3", deny);
        }

        [Fact]
        public async Task DenyActivation_Pilot_NotGated()
        {
            var (restaurantId, _) = await SeedAsync(
                BillingSubscriptionPlans.Pilot,
                activeLocations: 1,
                pausedLocations: 1
            );

            var deny = await PendingPlanLimits.DenyActivationAsync(
                _context,
                _pricebook,
                restaurantId
            );

            Assert.Null(deny);
        }

        [Fact]
        public async Task TeamGate_ScheduledDowngrade_UsesTargetTeamCap()
        {
            var (restaurantId, _) = await SeedAsync(
                BillingSubscriptionPlans.Growth,
                activeLocations: 1,
                scheduledTarget: BillingSubscriptionPlans.Starter,
                extraMembers: 2
            );

            var decision = await _teamGate.DenyIncrementAsync(restaurantId);

            Assert.False(decision.AllowIncrement);
            Assert.Equal(3, decision.Cap);
            Assert.Equal(3, decision.Current);
            Assert.NotNull(decision.Message);
        }

        [Fact]
        public async Task TeamGate_ScheduledCancel_KeepsLiveCap()
        {
            var (restaurantId, _) = await SeedAsync(
                BillingSubscriptionPlans.Growth,
                activeLocations: 1,
                extraMembers: 2
            );
            var account = await _context.BillingAccounts.SingleAsync();
            account.HasScheduledChange = true;
            account.ScheduledCancelPlan = true;
            await _context.SaveChangesAsync();

            var decision = await _teamGate.DenyIncrementAsync(restaurantId);

            Assert.True(decision.AllowIncrement);
            Assert.Equal(10, decision.Cap);
        }

        private static AddOwnedLocationRequest NewRequest() => new()
        {
            LocationName = "Branch",
            Address = "2 High Street",
            City = "Leeds",
            Postcode = "LS1 2AB",
        };

        private async Task<(int RestaurantId, int UserId)> SeedAsync(
            string plan,
            int activeLocations,
            int pausedLocations = 0,
            int archivedLocations = 0,
            string? scheduledTarget = null,
            int extraMembers = 0
        )
        {
            var owner = NewUser("Owner");
            _context.Users.Add(owner);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Pending Plan Venue",
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
                _pricebook.CurrentPricebookId
            );
            account.SubscriptionPlan = plan;
            account.BillingCycle = BillingCycles.Monthly;
            account.BillingStatus = BillingStatuses.Active;
            account.RenewalDateUtc = new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc);
            if (scheduledTarget != null)
            {
                account.HasScheduledChange = true;
                account.ScheduledTargetSubscriptionPlan = scheduledTarget;
                account.ScheduledTargetBillingCycle = BillingCycles.Monthly;
                account.ScheduledTargetExtraLocationCount = 0;
            }

            _context.BillingAccounts.Add(account);

            AddLocations(restaurant.Id, activeLocations, LocationLifecycleStatus.Active);
            AddLocations(restaurant.Id, pausedLocations, LocationLifecycleStatus.Paused);
            AddLocations(restaurant.Id, archivedLocations, LocationLifecycleStatus.Archived);

            _context.RestaurantMemberships.Add(NewMembership(owner.Id, restaurant.Id, PermissionRoles.Owner));
            for (var i = 0; i < extraMembers; i++)
            {
                var staff = NewUser($"Staff {i + 1}");
                _context.Users.Add(staff);
                await _context.SaveChangesAsync();
                _context.RestaurantMemberships.Add(
                    NewMembership(staff.Id, restaurant.Id, PermissionRoles.Staff)
                );
            }

            await _context.SaveChangesAsync();
            return (restaurant.Id, owner.Id);
        }

        private void AddLocations(
            int restaurantId,
            int count,
            LocationLifecycleStatus status
        )
        {
            for (var i = 0; i < count; i++)
            {
                _context.RestaurantLocations.Add(
                    new RestaurantLocation
                    {
                        RestaurantId = restaurantId,
                        LocationName = $"{status} {i + 1}",
                        Address = $"{i + 1} High Street",
                        LifecycleStatus = status,
                        CreatedAt = DateTime.UtcNow,
                    }
                );
            }
        }

        private static User NewUser(string name) => new()
        {
            FullName = name,
            Email = $"{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            PhoneNumber = "07700900111",
            Role = "Owner",
            AccountType = "Single",
            IsEmailVerified = true,
            IsApprovedByAdmin = true,
            CreatedAt = DateTime.UtcNow,
        };

        private static RestaurantMembership NewMembership(
            int userId,
            int restaurantId,
            string role
        ) => new()
        {
            UserId = userId,
            RestaurantId = restaurantId,
            PermissionRole = role,
            LocationScope = LocationScopeKind.AllLocations,
            NamedLocationIdsJson = "[]",
            Status = MembershipStatus.Active,
        };
    }
}
