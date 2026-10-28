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
            Assert.Equal(result.EndsAtUtc, account.PilotOriginalEndUtc);
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
            Assert.Equal(first.EndsAtUtc, (await LoadAccountAsync(restaurantId)).PilotOriginalEndUtc);
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

        [Fact]
        public async Task StartPilot_PaidPlanNullStart_RefusesWithoutChange()
        {
            var restaurantId = await SeedFreeRestaurantAsync();
            var renewal = DateTime.UtcNow.AddDays(20);
            await SetAccountAsync(restaurantId, account =>
            {
                account.SubscriptionPlan = BillingSubscriptionPlans.Growth;
                account.BillingStatus = BillingStatuses.Active;
                account.BillingCycle = BillingCycles.Monthly;
                account.RenewalDateUtc = renewal;
            });

            var result = await _service.StartPilotAsync(restaurantId);

            Assert.False(result.Started);
            Assert.Equal("pilot_already_used", result.RefusedCode);
            var reloaded = await LoadAccountAsync(restaurantId);
            Assert.Equal(BillingSubscriptionPlans.Growth, reloaded.SubscriptionPlan);
            Assert.Equal(BillingCycles.Monthly, reloaded.BillingCycle);
            Assert.Equal(renewal, reloaded.RenewalDateUtc);
            Assert.Null(reloaded.PilotStartedAtUtc);
            Assert.Equal(0, await CountPilotGrantRowsAsync(restaurantId));
        }

        [Fact]
        public async Task StartPilot_PilotNullStartWithEnd_StampsStartKeepsEnd()
        {
            var restaurantId = await SeedFreeRestaurantAsync();
            var end = DateTime.UtcNow.AddDays(10);
            var clockBefore = DateTime.UtcNow.AddDays(9);
            await SetAccountAsync(restaurantId, account =>
            {
                account.SubscriptionPlan = BillingSubscriptionPlans.Pilot;
                account.BillingStatus = BillingStatuses.Pilot;
                account.PilotPeriodEnd = end;
            });
            await SetOwnerClockAsync(restaurantId, clockBefore);

            var result = await _service.StartPilotAsync(restaurantId);

            Assert.False(result.Started);
            Assert.Null(result.RefusedCode);
            Assert.Equal(end.AddDays(-30), result.StartedAtUtc);
            Assert.Equal(end, result.EndsAtUtc);
            var reloaded = await LoadAccountAsync(restaurantId);
            Assert.Equal(end.AddDays(-30), reloaded.PilotStartedAtUtc);
            Assert.Equal(end, reloaded.PilotPeriodEnd);
            Assert.Equal(0, await CountPilotGrantRowsAsync(restaurantId));
            Assert.Equal(clockBefore, await LoadOwnerClockAsync(restaurantId));
        }

        [Fact]
        public async Task StartPilot_PilotNullStartNullEnd_StampsNowNoEmail()
        {
            var restaurantId = await SeedFreeRestaurantAsync();
            await SetAccountAsync(restaurantId, account =>
            {
                account.SubscriptionPlan = BillingSubscriptionPlans.Pilot;
                account.BillingStatus = BillingStatuses.Pilot;
                account.PilotPeriodEnd = null;
            });
            var before = DateTime.UtcNow;

            var result = await _service.StartPilotAsync(restaurantId);
            await _service.NotifyStartedAsync(restaurantId, result);

            Assert.False(result.Started);
            Assert.Null(result.RefusedCode);
            Assert.NotNull(result.StartedAtUtc);
            Assert.True(result.StartedAtUtc >= before);
            Assert.True(result.StartedAtUtc <= DateTime.UtcNow);
            Assert.Null(result.EndsAtUtc);
            var reloaded = await LoadAccountAsync(restaurantId);
            Assert.Equal(result.StartedAtUtc, reloaded.PilotStartedAtUtc);
            Assert.Null(reloaded.PilotPeriodEnd);
            Assert.Equal(0, await CountPilotGrantRowsAsync(restaurantId));
            Assert.Equal(0, _email.PilotStartedCalls);
        }

        [Fact]
        public async Task StartPilot_FreeWithPriorPilotMint_RefusesAlreadyUsed()
        {
            var restaurantId = await SeedFreeRestaurantAsync();
            _context.CreditLedgerEntries.Add(new CreditLedgerEntry
            {
                Id = Guid.NewGuid(),
                RestaurantId = restaurantId,
                Channel = CreditChannels.Email,
                EntryType = CreditLedgerEntryTypes.PilotAllocation,
                Quantity = 500,
                PricebookVersion = "TUMMLY-UK-GBP-2026-08-V3",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-60),
            });
            await _context.SaveChangesAsync();

            var result = await _service.StartPilotAsync(restaurantId);

            Assert.False(result.Started);
            Assert.Equal("pilot_already_used", result.RefusedCode);
            var reloaded = await LoadAccountAsync(restaurantId);
            Assert.Equal(BillingSubscriptionPlans.Free, reloaded.SubscriptionPlan);
            Assert.Null(reloaded.PilotStartedAtUtc);
            Assert.Null(reloaded.PilotPeriodEnd);
            Assert.Equal(1, await CountPilotGrantRowsAsync(restaurantId));
        }

        [Fact]
        public async Task NotifyStarted_ShopOrderFailure_DoesNotThrowAndStillSendsEmail()
        {
            var restaurantId = await SeedFreeRestaurantAsync();
            var failingShopOrders = new ThrowingComplimentaryShopOrders();
            var email = new RecordingPilotStartedEmailService();
            var service = PilotStartTestFactory.Create(
                _context,
                TestPricebookPaths.LoadV3(),
                new CreditLedgerService(
                    _context,
                    TimeProvider.System,
                    TestPricebookPaths.LoadV3(),
                    new AdminAuditService(_context, TimeProvider.System)
                ),
                failingShopOrders,
                NoOpPrintReadyQrMaterialsWork.Instance,
                email
            );
            var started = await service.StartPilotAsync(restaurantId);

            await service.NotifyStartedAsync(restaurantId, started);

            Assert.Equal(1, failingShopOrders.Calls);
            Assert.Equal(1, email.PilotStartedCalls);
        }

        private async Task SetAccountAsync(
            int restaurantId,
            Action<BillingAccount> change
        )
        {
            var account = await _context.BillingAccounts
                .SingleAsync(row => row.RestaurantId == restaurantId);
            change(account);
            await _context.SaveChangesAsync();
        }

        private async Task SetOwnerClockAsync(int restaurantId, DateTime clock)
        {
            var ownerId = await _context.Restaurants
                .Where(row => row.Id == restaurantId)
                .Select(row => row.OwnerUserId)
                .SingleAsync();
            var owner = await _context.Users.SingleAsync(row => row.Id == ownerId);
            owner.ActivationExpiresAt = clock;
            await _context.SaveChangesAsync();
        }

        private async Task<DateTime?> LoadOwnerClockAsync(int restaurantId)
        {
            var ownerId = await _context.Restaurants
                .Where(row => row.Id == restaurantId)
                .Select(row => row.OwnerUserId)
                .SingleAsync();
            return await _context.Users
                .AsNoTracking()
                .Where(row => row.Id == ownerId)
                .Select(row => row.ActivationExpiresAt)
                .SingleAsync();
        }

        private sealed class ThrowingComplimentaryShopOrders
            : IComplimentaryStarterShopOrderService
        {
            public int Calls { get; private set; }

            public Task<ComplimentaryStarterShopOrderResult> EnsureForLocationAsync(
                int restaurantId,
                int locationId,
                int placedByUserId,
                string placedByName,
                CancellationToken cancellationToken = default
            ) => throw new InvalidOperationException("shop order store down");

            public Task<IReadOnlyList<Guid>> EnsureAllActiveLocationsAsync(
                int restaurantId,
                int placedByUserId,
                string placedByName,
                CancellationToken cancellationToken = default
            )
            {
                Calls++;
                throw new InvalidOperationException("shop order store down");
            }
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
