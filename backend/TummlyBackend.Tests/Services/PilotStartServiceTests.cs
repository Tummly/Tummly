using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class PilotStartServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly RecordingPilotStartedEmailService _email = new();
        private readonly PilotStartService _service;

        public PilotStartServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            _context = new ApplicationDbContext(options);

            var pricebook = TestPricebookPaths.LoadV3();
            _service = PilotStartTestFactory.Create(
                _context,
                pricebook,
                new CreditLedgerService(
                    _context,
                    TimeProvider.System,
                    pricebook,
                    new AdminAuditService(_context, TimeProvider.System)
                ),
                NoOpComplimentaryStarterShopOrderService.Instance,
                NoOpPrintReadyQrMaterialsWork.Instance,
                _email
            );
        }

        [Fact]
        public async Task StartPilot_FirstTime_SetsStartEndCreditsOnce()
        {
            var restaurantId = await SeedFreeRestaurantAsync();

            var result = await _service.StartPilotAsync(restaurantId);

            Assert.True(result.Started);
            Assert.Null(result.RefusedCode);
            Assert.NotNull(result.StartedAtUtc);
            Assert.Equal(
                result.StartedAtUtc!.Value.AddDays(
                    ActivationCodeHelper.ActivationPeriodDays
                ),
                result.EndsAtUtc
            );

            var account = await LoadAccountAsync(restaurantId);
            Assert.Equal(result.StartedAtUtc, account.PilotStartedAtUtc);
            Assert.Equal(BillingSubscriptionPlans.Pilot, account.SubscriptionPlan);
            Assert.Equal(BillingStatuses.Pilot, account.BillingStatus);
            Assert.Equal(result.EndsAtUtc, account.PilotPeriodEnd);
            Assert.Null(account.RenewalDateUtc);
            // One grant set: one ledger row per channel (ai, email, sms).
            Assert.Equal(PilotGrantRowCount, await CountPilotGrantRowsAsync(restaurantId));
        }

        [Fact]
        public async Task StartPilot_Twice_ReturnsSameDatesNoSecondMint()
        {
            var restaurantId = await SeedFreeRestaurantAsync();
            var first = await _service.StartPilotAsync(restaurantId);

            var second = await _service.StartPilotAsync(restaurantId);

            Assert.False(second.Started);
            Assert.Null(second.RefusedCode);
            Assert.Equal(first.StartedAtUtc, second.StartedAtUtc);
            Assert.Equal(first.EndsAtUtc, second.EndsAtUtc);
            Assert.Equal(PilotGrantRowCount, await CountPilotGrantRowsAsync(restaurantId));
        }

        [Fact]
        public async Task StartPilot_AlreadyUsed_OnFree_RefusesWithoutChange()
        {
            var restaurantId = await SeedFreeRestaurantAsync();
            var usedAt = DateTime.UtcNow.AddDays(-40);
            var account = await _context.BillingAccounts
                .SingleAsync(row => row.RestaurantId == restaurantId);
            account.PilotStartedAtUtc = usedAt;
            account.PilotPeriodEnd = usedAt.AddDays(30);
            await _context.SaveChangesAsync();

            var result = await _service.StartPilotAsync(restaurantId);

            Assert.False(result.Started);
            Assert.Equal("pilot_already_used", result.RefusedCode);

            var reloaded = await LoadAccountAsync(restaurantId);
            Assert.Equal(BillingSubscriptionPlans.Free, reloaded.SubscriptionPlan);
            Assert.Equal(usedAt, reloaded.PilotStartedAtUtc);
            Assert.Equal(0, await CountPilotGrantRowsAsync(restaurantId));
        }

        [Fact]
        public async Task StartPilot_SyncsOwnerAndMembersClock()
        {
            var restaurantId = await SeedFreeRestaurantAsync();
            var owner = await _context.Restaurants
                .AsNoTracking()
                .Where(row => row.Id == restaurantId)
                .Select(row => row.OwnerUserId)
                .FirstAsync();
            var activeMember = await SeedUserAsync("active-member@example.com");
            var removedMember = await SeedUserAsync("removed-member@example.com");
            _context.RestaurantMemberships.AddRange(
                new RestaurantMembership
                {
                    UserId = activeMember.Id,
                    RestaurantId = restaurantId,
                    PermissionRole = PermissionRoles.Staff,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                },
                new RestaurantMembership
                {
                    UserId = removedMember.Id,
                    RestaurantId = restaurantId,
                    PermissionRole = PermissionRoles.Staff,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Deactivated,
                }
            );
            await _context.SaveChangesAsync();

            var result = await _service.StartPilotAsync(restaurantId);

            var clocks = await _context.Users
                .AsNoTracking()
                .ToDictionaryAsync(row => row.Id, row => row.ActivationExpiresAt);
            Assert.Equal(result.EndsAtUtc, clocks[owner]);
            Assert.Equal(result.EndsAtUtc, clocks[activeMember.Id]);
            Assert.Null(clocks[removedMember.Id]);
        }

        [Fact]
        public async Task StartPilot_InsideAmbientTransaction_DoesNotOpenSecond()
        {
            var restaurantId = await SeedFreeRestaurantAsync();

            await using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                var result = await _service.StartPilotAsync(restaurantId);
                Assert.True(result.Started);
                await transaction.CommitAsync();
            }

            var account = await LoadAccountAsync(restaurantId);
            Assert.Equal(BillingSubscriptionPlans.Pilot, account.SubscriptionPlan);
            Assert.NotNull(account.PilotStartedAtUtc);
        }

        [Fact]
        public async Task NotifyStarted_SendsOnlyWhenStarted()
        {
            var restaurantId = await SeedFreeRestaurantAsync();
            var started = await _service.StartPilotAsync(restaurantId);
            var repeat = await _service.StartPilotAsync(restaurantId);
            var refused = new PilotStartResult(
                Started: false,
                RefusedCode: "pilot_already_used",
                StartedAtUtc: null,
                EndsAtUtc: null
            );

            await _service.NotifyStartedAsync(restaurantId, repeat);
            await _service.NotifyStartedAsync(restaurantId, refused);
            Assert.Equal(0, _email.PilotStartedCalls);

            await _service.NotifyStartedAsync(restaurantId, started);
            Assert.Equal(1, _email.PilotStartedCalls);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<int> SeedFreeRestaurantAsync()
        {
            var owner = await SeedUserAsync("owner@example.com");
            var restaurant = new Restaurant
            {
                Name = "Pilot Start Cafe",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
                BillingAccount = BillingCreditsService.CreateDefaultBillingAccount(
                    0,
                    "TUMMLY-UK-GBP-2026-08-V3"
                ),
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

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

        private async Task<User> SeedUserAsync(string email)
        {
            var user = new User
            {
                FullName = "Pilot Test User",
                Email = email,
                PasswordHash = "x",
                PhoneNumber = "5551234567",
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                TermsAccepted = true,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private Task<BillingAccount> LoadAccountAsync(int restaurantId) =>
            _context.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == restaurantId);

        private const int PilotGrantRowCount = 3;

        private Task<int> CountPilotGrantRowsAsync(int restaurantId) =>
            _context.CreditLedgerEntries.CountAsync(row =>
                row.RestaurantId == restaurantId
                && row.EntryType == CreditLedgerEntryTypes.PilotAllocation
            );
    }
}
