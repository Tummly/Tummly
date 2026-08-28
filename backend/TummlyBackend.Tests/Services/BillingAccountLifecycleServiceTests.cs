using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class BillingAccountLifecycleServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly RecordingBillingAccountNoticeNotifier _notifier = new();
        private readonly BillingAccountLifecycleService _lifecycle;

        public BillingAccountLifecycleServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;

            _context = new ApplicationDbContext(options);
            _lifecycle = new BillingAccountLifecycleService(_context, _notifier);
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public async Task Tick_AtPilotPeriodEnd_WritesSoftLock()
        {
            var now = new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedPilotAsync(now.AddHours(-1));

            await _lifecycle.TickAsync(seeded.RestaurantId, now);

            var account = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingStatuses.SoftLock, account.BillingStatus);
            Assert.Equal(now.AddHours(-1), account.SoftLockEnteredAt);
            Assert.Null(account.DormantEnteredAt);
            Assert.Single(_notifier.PilotLockEnters);
            Assert.Empty(_notifier.PilotDormantEnters);
        }

        [Fact]
        public async Task Tick_FifteenTimes24hAfterPeriodEnd_WritesDormant()
        {
            var periodEnd = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
            var now = periodEnd.AddHours(BillingAccountLifecycleService.PilotDormantHours);
            var seeded = await SeedPilotAsync(periodEnd);

            await _lifecycle.TickAsync(seeded.RestaurantId, now);

            var account = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingStatuses.Dormant, account.BillingStatus);
            Assert.Equal(periodEnd, account.SoftLockEnteredAt);
            Assert.Equal(now, account.DormantEnteredAt);
            Assert.Equal(2, _notifier.PilotLockEnters.Count + _notifier.PilotDormantEnters.Count);
            Assert.Single(_notifier.PilotLockEnters);
            Assert.Single(_notifier.PilotDormantEnters);
        }

        [Fact]
        public async Task Dunning_WalksDay0Then10Then24()
        {
            var start = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedPaidAsync();

            var started = await _lifecycle.StartDunningEpisodeAsync(
                seeded.RestaurantId,
                start
            );
            Assert.True(started.Applied);
            Assert.Equal(
                BillingStatuses.PastDue,
                (await ReloadAsync(seeded.RestaurantId)).BillingStatus
            );
            Assert.Equal([0], _notifier.PaymentFailureDays.Select(item => item.DayStep));

            await _lifecycle.TickAsync(
                seeded.RestaurantId,
                start.AddHours(BillingAccountLifecycleService.DunningSoftLockHours)
            );
            var afterTen = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingStatuses.SoftLock, afterTen.BillingStatus);
            Assert.Equal(
                [0, 3, 7, 10],
                _notifier.PaymentFailureDays.Select(item => item.DayStep).ToArray()
            );

            await _lifecycle.TickAsync(
                seeded.RestaurantId,
                start.AddHours(BillingAccountLifecycleService.DunningDormantHours)
            );
            var afterTwentyFour = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingStatuses.Dormant, afterTwentyFour.BillingStatus);
            Assert.Equal(
                [0, 3, 7, 10, 24],
                _notifier.PaymentFailureDays.Select(item => item.DayStep).ToArray()
            );
        }

        [Fact]
        public async Task Pause_DoesNotChangeBillingStatus()
        {
            var now = new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedPilotAsync(now.AddDays(10));
            var cache = new MemoryDistributedCache(
                Options.Create(new MemoryDistributedCacheOptions())
            );
            var workspace = new AccountWorkspaceService(
                _context,
                new UnusedAttachmentStorage(),
                cache
            );

            var (details, error, statusCode) = await workspace.PauseWorkspaceAsync(
                seeded.OwnerId,
                seeded.RestaurantId
            );

            Assert.Equal(200, statusCode);
            Assert.Null(error);
            Assert.NotNull(details);
            Assert.Equal("Paused", details.Status.WorkspaceStatus);
            var account = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingStatuses.Pilot, account.BillingStatus);
        }

        [Fact]
        public async Task StartDunningEpisode_RefusedOnPilot()
        {
            var seeded = await SeedPilotAsync(DateTime.UtcNow.AddDays(10));

            var result = await _lifecycle.StartDunningEpisodeAsync(
                seeded.RestaurantId,
                DateTime.UtcNow
            );

            Assert.True(result.Refused);
            Assert.Equal(BillingAccountLifecycleService.PilotRefuseReason, result.Reason);
            Assert.Equal(
                BillingStatuses.Pilot,
                (await ReloadAsync(seeded.RestaurantId)).BillingStatus
            );
            Assert.Empty(_notifier.PaymentFailureDays);
        }

        [Fact]
        public async Task StartDunningEpisode_RefusedWhenChargebackRestricted()
        {
            var seeded = await SeedPaidAsync();
            await _lifecycle.SetChargebackRestrictionAsync(seeded.RestaurantId, true);

            var result = await _lifecycle.StartDunningEpisodeAsync(
                seeded.RestaurantId,
                DateTime.UtcNow
            );

            Assert.True(result.Refused);
            Assert.Equal(
                BillingAccountLifecycleService.ChargebackRefuseReason,
                result.Reason
            );
            Assert.Equal(
                BillingStatuses.Active,
                (await ReloadAsync(seeded.RestaurantId)).BillingStatus
            );
            Assert.Null(
                (await ReloadAsync(seeded.RestaurantId)).DunningEpisodeStartedAt
            );
            Assert.Empty(_notifier.PaymentFailureDays);
        }

        [Fact]
        public async Task StartDunningEpisode_SecondOverdueWhileOpen_IsNoOp()
        {
            var seeded = await SeedPaidAsync();
            var start = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            await _lifecycle.StartDunningEpisodeAsync(seeded.RestaurantId, start);

            var second = await _lifecycle.StartDunningEpisodeAsync(
                seeded.RestaurantId,
                start.AddHours(3)
            );

            Assert.False(second.Applied);
            Assert.False(second.Refused);
            Assert.Single(_notifier.PaymentFailureDays);
            Assert.Equal(
                start,
                (await ReloadAsync(seeded.RestaurantId)).DunningEpisodeStartedAt
            );
        }

        [Fact]
        public async Task Tick_WhilePaused_StillWritesSoftLock()
        {
            var now = new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedPilotAsync(now.AddHours(-1));
            var restaurant = await _context.Restaurants.FirstAsync(
                row => row.Id == seeded.RestaurantId
            );
            restaurant.WorkspaceStatus = WorkspaceStatus.Paused;
            await _context.SaveChangesAsync();

            await _lifecycle.TickAsync(seeded.RestaurantId, now);

            Assert.Equal(
                BillingStatuses.SoftLock,
                (await ReloadAsync(seeded.RestaurantId)).BillingStatus
            );
        }

        [Fact]
        public async Task SetChargebackRestriction_DoesNotChangeBillingStatus()
        {
            var seeded = await SeedPaidAsync();

            await _lifecycle.SetChargebackRestrictionAsync(seeded.RestaurantId, true);

            var account = await ReloadAsync(seeded.RestaurantId);
            Assert.True(account.ChargebackRestricted);
            Assert.Equal(BillingStatuses.Active, account.BillingStatus);
        }

        [Fact]
        public async Task ActivatePaidPlan_ClearsPilotClocks()
        {
            var now = new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedPilotAsync(now.AddHours(-1));
            await _lifecycle.TickAsync(seeded.RestaurantId, now);

            await _lifecycle.ActivatePaidPlanAsync(seeded.RestaurantId, now);

            var account = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingStatuses.Active, account.BillingStatus);
            Assert.Null(account.PilotPeriodEnd);
            Assert.Null(account.SoftLockEnteredAt);
            Assert.False(account.PilotSoftLockNotified);
        }

        private async Task<SeededAccount> SeedPilotAsync(DateTime periodEnd)
        {
            var owner = new User
            {
                FullName = "Pilot Owner",
                Email = $"pilot-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                Role = "Owner",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                ActivatedAt = periodEnd.AddDays(-30),
                ActivationExpiresAt = periodEnd,
            };
            _context.Users.Add(owner);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Pilot Cafe",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            _context.BillingAccounts.Add(
                new BillingAccount
                {
                    RestaurantId = restaurant.Id,
                    SubscriptionPlan = BillingSubscriptionPlans.Pilot,
                    BillingStatus = BillingStatuses.Pilot,
                    ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
                    StarterKitState = StarterKitStates.Unused,
                    PilotPeriodEnd = periodEnd,
                }
            );
            await _context.SaveChangesAsync();
            return new SeededAccount(restaurant.Id, owner.Id);
        }

        private async Task<SeededAccount> SeedPaidAsync()
        {
            var owner = new User
            {
                FullName = "Paid Owner",
                Email = $"paid-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                Role = "Owner",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
            };
            _context.Users.Add(owner);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Paid Cafe",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            _context.BillingAccounts.Add(
                new BillingAccount
                {
                    RestaurantId = restaurant.Id,
                    SubscriptionPlan = BillingSubscriptionPlans.Growth,
                    BillingCycle = BillingCycles.Monthly,
                    BillingStatus = BillingStatuses.Active,
                    ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
                    StarterKitState = StarterKitStates.Unused,
                }
            );
            await _context.SaveChangesAsync();
            return new SeededAccount(restaurant.Id, owner.Id);
        }

        private async Task<BillingAccount> ReloadAsync(int restaurantId)
        {
            _context.ChangeTracker.Clear();
            var account = await _context.BillingAccounts.SingleAsync(
                row => row.RestaurantId == restaurantId
            );
            return account;
        }

        private sealed record SeededAccount(int RestaurantId, int OwnerId);

        private sealed class RecordingBillingAccountNoticeNotifier
            : IBillingAccountNoticeNotifier
        {
            public List<(int RestaurantId, int DayStep, string EpisodeId)> PaymentFailureDays
            { get; } = [];

            public List<(int RestaurantId, string EpisodeKey)> PilotLockEnters { get; } = [];

            public List<(int RestaurantId, string EpisodeKey)> PilotDormantEnters { get; } = [];

            public Task NotifyCreditThresholdCrossedAsync(
                int restaurantId,
                string channel,
                int thresholdBand,
                string periodKey,
                string billingStatus,
                bool isPilot,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;

            public Task NotifyPaymentFailureDayStepAsync(
                int restaurantId,
                int dayStep,
                string episodeId,
                CancellationToken cancellationToken = default
            )
            {
                PaymentFailureDays.Add((restaurantId, dayStep, episodeId));
                return Task.CompletedTask;
            }

            public Task NotifyUnpaidPilotLockEnterAsync(
                int restaurantId,
                string episodeKey,
                CancellationToken cancellationToken = default
            )
            {
                PilotLockEnters.Add((restaurantId, episodeKey));
                return Task.CompletedTask;
            }

            public Task NotifyUnpaidPilotDormantEnterAsync(
                int restaurantId,
                string episodeKey,
                CancellationToken cancellationToken = default
            )
            {
                PilotDormantEnters.Add((restaurantId, episodeKey));
                return Task.CompletedTask;
            }
        }

        private sealed class UnusedAttachmentStorage : IQueryAttachmentStorage
        {
            public bool IsConfigured => false;

            public Task UploadAsync(
                string storageKey,
                Stream content,
                string contentType,
                long contentLength,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;

            public Task<Stream> OpenReadAsync(
                string storageKey,
                CancellationToken cancellationToken = default
            ) => throw new InvalidOperationException();

            public Task DeleteAsync(
                string storageKey,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;
        }
    }
}
