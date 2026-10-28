using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
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
        public async Task ActivatePaidPlan_ClearsOwnerClock_KeepsPilotEnd()
        {
            var now = new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedPilotAsync(now.AddHours(-1));
            await _lifecycle.TickAsync(seeded.RestaurantId, now);

            await _lifecycle.ActivatePaidPlanAsync(seeded.RestaurantId, now);

            var account = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingStatuses.Active, account.BillingStatus);
            Assert.Equal(now.AddHours(-1), account.PilotPeriodEnd);
            Assert.Null(account.SoftLockEnteredAt);
            Assert.False(account.PilotSoftLockNotified);
            var owner = await _context.Users.AsNoTracking().SingleAsync(
                row => row.Id == seeded.OwnerId
            );
            Assert.Null(owner.ActivationExpiresAt);
        }

        [Fact]
        public async Task Tick_PaidAfterPilot_KeptEndDate_StaysActive()
        {
            var now = new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedPaidAsync();
            var account = await ReloadAsync(seeded.RestaurantId);
            account.PilotPeriodEnd = now.AddDays(-30);
            await _context.SaveChangesAsync();

            await _lifecycle.TickAsync(seeded.RestaurantId, now);

            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingStatuses.Active, after.BillingStatus);
            Assert.Null(after.SoftLockEnteredAt);
        }

        [Fact]
        public async Task Tick_DormantEnter_ReleasesOpenHoldsThenExpiresUnheldPilot()
        {
            var periodEnd = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
            var now = periodEnd.AddHours(BillingAccountLifecycleService.PilotDormantHours);
            var seeded = await SeedPilotAsync(periodEnd);

            var location = new RestaurantLocation
            {
                RestaurantId = seeded.RestaurantId,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var pilotGrantId = Guid.NewGuid();
            var includedGrantId = Guid.NewGuid();
            var reservationRef = Guid.NewGuid().ToString("D");
            _context.CreditLedgerEntries.AddRange(
                new CreditLedgerEntry
                {
                    Id = pilotGrantId,
                    RestaurantId = seeded.RestaurantId,
                    Channel = CreditChannels.Sms,
                    EntryType = CreditLedgerEntryTypes.PilotAllocation,
                    Quantity = 100,
                    CreatedAtUtc = periodEnd.AddDays(-20),
                    ExpiresAtUtc = periodEnd.AddDays(40),
                },
                new CreditLedgerEntry
                {
                    Id = includedGrantId,
                    RestaurantId = seeded.RestaurantId,
                    Channel = CreditChannels.Email,
                    EntryType = CreditLedgerEntryTypes.IncludedAllocation,
                    Quantity = 50,
                    CreatedAtUtc = periodEnd.AddDays(-5),
                    ExpiresAtUtc = periodEnd.AddDays(25),
                    PeriodStartUtc = periodEnd.AddDays(-5),
                    PricebookVersion = "TUMMLY-UK-GBP-2026-08-V3",
                },
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = seeded.RestaurantId,
                    Channel = CreditChannels.Sms,
                    EntryType = CreditLedgerEntryTypes.Reservation,
                    Quantity = 10,
                    AllocationId = pilotGrantId,
                    ReservationRef = reservationRef,
                    LocationId = location.Id,
                    CreatedAtUtc = periodEnd.AddDays(-1),
                }
            );

            var campaign = new Campaign
            {
                RestaurantLocationId = location.Id,
                Name = "Held Campaign",
                Status = "scheduled",
                Channel = "sms",
                AudienceKey = "all",
                OfferStance = "none",
                MessageBody = "Hello",
                BillingReservationRef = reservationRef,
                ReservedEstimate = 10,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            await _lifecycle.TickAsync(seeded.RestaurantId, now);

            Assert.Equal(
                BillingStatuses.Dormant,
                (await ReloadAsync(seeded.RestaurantId)).BillingStatus
            );

            var entries = await _context.CreditLedgerEntries
                .Where(row => row.RestaurantId == seeded.RestaurantId)
                .ToListAsync();
            Assert.Contains(
                entries,
                row =>
                    row.EntryType == CreditLedgerEntryTypes.Release
                    && row.ReservationRef == reservationRef
                    && row.Quantity == 10
            );
            Assert.Contains(
                entries,
                row =>
                    row.EntryType == CreditLedgerEntryTypes.Expiry
                    && row.AllocationId == pilotGrantId
                    && row.Quantity == 100
            );
            Assert.DoesNotContain(
                entries,
                row =>
                    row.EntryType == CreditLedgerEntryTypes.Expiry
                    && row.AllocationId == includedGrantId
            );

            _context.ChangeTracker.Clear();
            var reloadedCampaign = await _context.Campaigns.SingleAsync(
                row => row.Id == campaign.Id
            );
            Assert.Null(reloadedCampaign.BillingReservationRef);
        }

        [Fact]
        public async Task Tick_SoftLockEnter_DoesNotExpirePilotLeftover()
        {
            var now = new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc);
            var periodEnd = now.AddHours(-1);
            var seeded = await SeedPilotAsync(periodEnd);

            var pilotGrantId = Guid.NewGuid();
            _context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = pilotGrantId,
                    RestaurantId = seeded.RestaurantId,
                    Channel = CreditChannels.Ai,
                    EntryType = CreditLedgerEntryTypes.PilotAllocation,
                    Quantity = 40,
                    CreatedAtUtc = periodEnd.AddDays(-10),
                    ExpiresAtUtc = periodEnd.AddDays(20),
                }
            );
            await _context.SaveChangesAsync();

            await _lifecycle.TickAsync(seeded.RestaurantId, now);

            Assert.Equal(
                BillingStatuses.SoftLock,
                (await ReloadAsync(seeded.RestaurantId)).BillingStatus
            );
            Assert.DoesNotContain(
                await _context.CreditLedgerEntries
                    .Where(row => row.RestaurantId == seeded.RestaurantId)
                    .ToListAsync(),
                row => row.EntryType == CreditLedgerEntryTypes.Expiry
            );
        }

        [Fact]
        public void ApplyPostCancelSoftLock_DemotesPaidToPilotSoftLock()
        {
            var renewalEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var account = new BillingAccount
            {
                RestaurantId = 1,
                SubscriptionPlan = BillingSubscriptionPlans.Growth,
                BillingCycle = BillingCycles.Monthly,
                BillingStatus = BillingStatuses.Active,
                ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
                StarterKitState = StarterKitStates.Unused,
                PaidExtraLocationCount = 2,
                DunningEpisodeStartedAt = renewalEnd.AddDays(-5),
                DunningFiredSteps = "0,3",
                DunningOutstandingOrderId = "ord_x",
                PilotSoftLockNotified = true,
                PilotDormantNotified = true,
            };

            var result = _lifecycle.ApplyPostCancelSoftLock(account, renewalEnd);

            Assert.True(result.Applied);
            Assert.Equal(BillingSubscriptionPlans.Pilot, account.SubscriptionPlan);
            Assert.Null(account.BillingCycle);
            Assert.Equal(BillingStatuses.SoftLock, account.BillingStatus);
            Assert.Equal(renewalEnd, account.PilotPeriodEnd);
            Assert.Equal(renewalEnd, account.SoftLockEnteredAt);
            Assert.Null(account.DormantEnteredAt);
            Assert.Null(account.DunningEpisodeStartedAt);
            Assert.Null(account.DunningFiredSteps);
            Assert.Null(account.DunningOutstandingOrderId);
            Assert.Equal(2, account.PaidExtraLocationCount);
            Assert.False(account.PilotSoftLockNotified);
            Assert.False(account.PilotDormantNotified);
        }

        [Fact]
        public void ApplyPostCancelSoftLock_ClearsGuestRetentionPurgedAt()
        {
            var renewalEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var account = new BillingAccount
            {
                RestaurantId = 1,
                SubscriptionPlan = BillingSubscriptionPlans.Growth,
                BillingCycle = BillingCycles.Monthly,
                BillingStatus = BillingStatuses.Dormant,
                ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
                StarterKitState = StarterKitStates.Unused,
                DormantEnteredAt = renewalEnd.AddDays(-100),
                GuestRetentionPurgedAtUtc = renewalEnd.AddDays(-10),
            };

            var result = _lifecycle.ApplyPostCancelSoftLock(account, renewalEnd);

            Assert.True(result.Applied);
            Assert.Equal(BillingStatuses.SoftLock, account.BillingStatus);
            Assert.Null(account.DormantEnteredAt);
            Assert.Null(account.GuestRetentionPurgedAtUtc);
        }

        [Fact]
        public void ApplyPostCancelSoftLock_IdempotentWhenAlreadyPilotSoftLock()
        {
            var renewalEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var account = new BillingAccount
            {
                RestaurantId = 1,
                SubscriptionPlan = BillingSubscriptionPlans.Pilot,
                BillingStatus = BillingStatuses.SoftLock,
                ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
                StarterKitState = StarterKitStates.Unused,
                PilotPeriodEnd = renewalEnd,
                SoftLockEnteredAt = renewalEnd,
            };

            var result = _lifecycle.ApplyPostCancelSoftLock(account, renewalEnd.AddDays(1));

            Assert.False(result.Applied);
            Assert.False(result.Refused);
            Assert.Equal(renewalEnd, account.SoftLockEnteredAt);
            Assert.Equal(renewalEnd, account.PilotPeriodEnd);
        }

        [Fact]
        public void ApplyPostCancelSoftLock_PaidWriteDeny_IsSoftLock()
        {
            var renewalEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var account = new BillingAccount
            {
                RestaurantId = 1,
                SubscriptionPlan = BillingSubscriptionPlans.Growth,
                BillingCycle = BillingCycles.Monthly,
                BillingStatus = BillingStatuses.Active,
                ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
                StarterKitState = StarterKitStates.Unused,
            };
            _lifecycle.ApplyPostCancelSoftLock(account, renewalEnd);

            var deny = OperatorBillingLockEvaluator.EvaluatePaidWriteDeny(
                OperatorBillingLockEvaluator.FromBillingAccount(account)
            );
            Assert.Equal(OperatorBillingLockEvaluator.SoftLock, deny);
        }

        [Fact]
        public async Task ActivatePaidPlan_AfterPostCancelSoftLock_ClearsLocks_KeepsPilotEnd()
        {
            var renewalEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedPaidAsync();
            var account = await ReloadAsync(seeded.RestaurantId);
            _lifecycle.ApplyPostCancelSoftLock(account, renewalEnd);
            await _context.SaveChangesAsync();

            var now = renewalEnd.AddDays(1);
            await _lifecycle.ActivatePaidPlanAsync(seeded.RestaurantId, now);

            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingStatuses.Active, after.BillingStatus);
            Assert.Null(after.SoftLockEnteredAt);
            Assert.Null(after.DormantEnteredAt);
            Assert.Equal(renewalEnd, after.PilotPeriodEnd);
        }

        [Fact]
        public async Task ActivatePaidPlan_ClearsGuestRetentionPurgedAt()
        {
            var periodEnd = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
            var dormantAt = periodEnd.AddHours(
                BillingAccountLifecycleService.PilotDormantHours
            );
            var seeded = await SeedPilotAsync(periodEnd);
            await _lifecycle.TickAsync(seeded.RestaurantId, dormantAt);

            var account = await ReloadAsync(seeded.RestaurantId);
            account.GuestRetentionPurgedAtUtc = dormantAt.AddDays(90);
            await _context.SaveChangesAsync();

            var now = dormantAt.AddDays(91);
            await _lifecycle.ActivatePaidPlanAsync(seeded.RestaurantId, now);

            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingStatuses.Active, after.BillingStatus);
            Assert.Null(after.GuestRetentionPurgedAtUtc);
            Assert.Null(after.DormantEnteredAt);
        }

        [Fact]
        public async Task ExtendPilot_Active14_MovesEndFromCurrentEnd()
        {
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var periodEnd = now.AddDays(5);
            var seeded = await SeedPilotAsync(periodEnd);

            var result = await _lifecycle.ExtendPilotAsync(
                seeded.RestaurantId,
                14,
                "key-active-14",
                now, "Support asked for a delay", 0
            );

            Assert.Equal("extended", result.Outcome);
            Assert.Null(result.Code);
            Assert.Equal(periodEnd, result.OldEndUtc);
            Assert.Equal(periodEnd.AddDays(14), result.NewEndUtc);
            Assert.Equal(1, result.CountAfter);
            Assert.Equal(14, result.DaysTotalAfter);

            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingStatuses.Pilot, after.BillingStatus);
            Assert.Equal(periodEnd.AddDays(14), after.PilotPeriodEnd);
            Assert.Equal(1, after.PilotExtensionCount);
            Assert.Equal(14, after.PilotExtensionDaysTotal);
            Assert.Equal("key-active-14", after.LastPilotExtensionKey);
        }

        [Fact]
        public async Task ExtendPilot_SoftLocked30_MovesEndFromNow_LiftsLock()
        {
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var periodEnd = now.AddDays(-3);
            var seeded = await SeedPilotAsync(periodEnd);
            var locked = await ReloadAsync(seeded.RestaurantId);
            locked.BillingStatus = BillingStatuses.SoftLock;
            locked.SoftLockEnteredAt = periodEnd;
            locked.PilotSoftLockNotified = true;
            locked.PilotDormantNotified = true;
            await _context.SaveChangesAsync();

            var result = await _lifecycle.ExtendPilotAsync(
                seeded.RestaurantId,
                30,
                "key-lock-30",
                now, "Support asked for a delay", 0
            );

            Assert.Equal("extended", result.Outcome);
            Assert.Equal(now.AddDays(30), result.NewEndUtc);
            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingStatuses.Pilot, after.BillingStatus);
            Assert.Equal(now.AddDays(30), after.PilotPeriodEnd);
            Assert.Null(after.SoftLockEnteredAt);
            Assert.False(after.PilotSoftLockNotified);
            Assert.False(after.PilotDormantNotified);
        }

        [Fact]
        public async Task ExtendPilot_InvalidDays7_Refused()
        {
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var periodEnd = now.AddDays(5);
            var seeded = await SeedPilotAsync(periodEnd);

            var result = await _lifecycle.ExtendPilotAsync(
                seeded.RestaurantId,
                7,
                "key-days-7",
                now, "Support asked for a delay", 0
            );

            Assert.Equal("refused", result.Outcome);
            Assert.Equal("pilot_extension_invalid_days", result.Code);
            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(periodEnd, after.PilotPeriodEnd);
            Assert.Equal(0, after.PilotExtensionCount);
        }

        [Fact]
        public async Task ExtendPilot_Third_RefusedLimit()
        {
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var periodEnd = now.AddDays(5);
            var seeded = await SeedPilotAsync(
                periodEnd,
                extensionCount: 2,
                extensionDaysTotal: 28
            );

            var result = await _lifecycle.ExtendPilotAsync(
                seeded.RestaurantId,
                14,
                "key-third",
                now, "Support asked for a delay", 2
            );

            Assert.Equal("refused", result.Outcome);
            Assert.Equal("pilot_extension_limit", result.Code);
            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(periodEnd, after.PilotPeriodEnd);
            Assert.Equal(2, after.PilotExtensionCount);
            Assert.Equal(28, after.PilotExtensionDaysTotal);
        }

        [Fact]
        public async Task ExtendPilot_TotalOver90_RefusedLimit()
        {
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var periodEnd = now.AddDays(5);
            var seeded = await SeedPilotAsync(
                periodEnd,
                extensionCount: 1,
                extensionDaysTotal: 75
            );

            var result = await _lifecycle.ExtendPilotAsync(
                seeded.RestaurantId,
                30,
                "key-total-105",
                now, "Support asked for a delay", 1
            );

            Assert.Equal("refused", result.Outcome);
            Assert.Equal("pilot_extension_limit", result.Code);
            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(1, after.PilotExtensionCount);
            Assert.Equal(75, after.PilotExtensionDaysTotal);
        }

        [Fact]
        public async Task ExtendPilot_Dormant_RefusedNotAllowed()
        {
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var periodEnd = now.AddDays(-20);
            var seeded = await SeedPilotAsync(
                periodEnd,
                status: BillingStatuses.Dormant
            );

            var result = await _lifecycle.ExtendPilotAsync(
                seeded.RestaurantId,
                14,
                "key-dormant",
                now, "Support asked for a delay", 0
            );

            Assert.Equal("refused", result.Outcome);
            Assert.Equal("pilot_extension_not_allowed", result.Code);
            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingStatuses.Dormant, after.BillingStatus);
            Assert.Equal(0, after.PilotExtensionCount);
        }

        [Fact]
        public async Task ExtendPilot_PaidPlan_RefusedNotAllowed()
        {
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedPaidAsync();

            var result = await _lifecycle.ExtendPilotAsync(
                seeded.RestaurantId,
                14,
                "key-paid",
                now, "Support asked for a delay", 0
            );

            Assert.Equal("refused", result.Outcome);
            Assert.Equal("pilot_extension_not_allowed", result.Code);
            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingSubscriptionPlans.Growth, after.SubscriptionPlan);
            Assert.Equal(0, after.PilotExtensionCount);
        }

        [Fact]
        public async Task ExtendPilot_PostCancelSoftLock_RefusedNotAllowed()
        {
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedPaidAsync();
            var renewalEnd = now.AddDays(-3);
            var account = await ReloadAsync(seeded.RestaurantId);
            account.RenewalDateUtc = renewalEnd;
            _lifecycle.ApplyPostCancelSoftLock(account, renewalEnd);
            await _context.SaveChangesAsync();
            Assert.Equal(BillingSubscriptionPlans.Pilot, account.SubscriptionPlan);
            Assert.Equal(BillingStatuses.SoftLock, account.BillingStatus);

            var result = await _lifecycle.ExtendPilotAsync(
                seeded.RestaurantId,
                14,
                "key-post-cancel",
                now, "Support asked for a delay", 0
            );

            Assert.Equal("refused", result.Outcome);
            Assert.Equal("pilot_extension_not_allowed", result.Code);
            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingStatuses.SoftLock, after.BillingStatus);
            Assert.Equal(renewalEnd, after.PilotPeriodEnd);
            Assert.Equal(0, after.PilotExtensionCount);
        }

        [Fact]
        public async Task ExtendPilot_SameKeyTwice_ExtendsOnce()
        {
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var periodEnd = now.AddDays(5);
            var seeded = await SeedPilotAsync(periodEnd);

            var first = await _lifecycle.ExtendPilotAsync(
                seeded.RestaurantId,
                14,
                "same-key",
                now, "Support asked for a delay", 0
            );
            var second = await _lifecycle.ExtendPilotAsync(
                seeded.RestaurantId,
                14,
                "same-key",
                now, "Support asked for a delay", 0
            );

            Assert.Equal("extended", first.Outcome);
            Assert.Equal("replayed", second.Outcome);
            Assert.Equal(1, second.CountAfter);
            Assert.Equal(14, second.DaysTotalAfter);
            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(periodEnd.AddDays(14), after.PilotPeriodEnd);
            Assert.Equal(1, after.PilotExtensionCount);
            Assert.Equal(14, after.PilotExtensionDaysTotal);
        }

        [Fact]
        public async Task ExtendPilot_ReplayAfterLimit_ReturnsReplayed_NewKeyLimit()
        {
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var periodEnd = now.AddDays(5);
            var seeded = await SeedPilotAsync(periodEnd);

            var a = await _lifecycle.ExtendPilotAsync(seeded.RestaurantId, 14, "key-a", now, "Support asked for a delay", 0);
            var b = await _lifecycle.ExtendPilotAsync(seeded.RestaurantId, 14, "key-b", now, "Support asked for a delay", 1);
            var replayB = await _lifecycle.ExtendPilotAsync(seeded.RestaurantId, 14, "key-b", now, "Support asked for a delay", 1);
            var c = await _lifecycle.ExtendPilotAsync(seeded.RestaurantId, 14, "key-c", now, "Support asked for a delay", 2);

            Assert.Equal("extended", a.Outcome);
            Assert.Equal("extended", b.Outcome);
            Assert.Equal("replayed", replayB.Outcome);
            Assert.Equal(2, replayB.CountAfter);
            Assert.Equal(28, replayB.DaysTotalAfter);
            Assert.Equal("refused", c.Outcome);
            Assert.Equal("pilot_extension_limit", c.Code);

            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(periodEnd.AddDays(28), after.PilotPeriodEnd);
            Assert.Equal(2, after.PilotExtensionCount);
            Assert.Equal(28, after.PilotExtensionDaysTotal);
            Assert.Equal("key-b", after.LastPilotExtensionKey);
        }

        [Fact]
        public async Task ExtendPilot_KeepsStart()
        {
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var startedAt = now.AddDays(-20);
            var seeded = await SeedPilotAsync(
                now.AddDays(-6),
                startedAt: startedAt,
                status: BillingStatuses.SoftLock
            );

            await _lifecycle.ExtendPilotAsync(seeded.RestaurantId, 14, "key-start", now, "Support asked for a delay", 0);

            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(startedAt, after.PilotStartedAtUtc);
        }

        [Fact]
        public async Task ExtendPilot_SyncsMembersClock()
        {
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var periodEnd = now.AddDays(5);
            var seeded = await SeedPilotAsync(periodEnd);
            var memberId = await AddActiveMemberAsync(seeded.RestaurantId, periodEnd);

            await _lifecycle.ExtendPilotAsync(seeded.RestaurantId, 14, "key-members", now, "Support asked for a delay", 0);

            _context.ChangeTracker.Clear();
            var owner = await _context.Users.SingleAsync(row => row.Id == seeded.OwnerId);
            var member = await _context.Users.SingleAsync(row => row.Id == memberId);
            Assert.Equal(periodEnd.AddDays(14), owner.ActivationExpiresAt);
            Assert.Equal(periodEnd.AddDays(14), member.ActivationExpiresAt);
        }

        [Fact]
        public async Task ExtendPilot_MovesEffectiveEnd_KeepsOriginalEnd()
        {
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var periodEnd = now.AddDays(5);
            var originalEnd = new DateTime(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedPilotAsync(periodEnd, originalEndUtc: originalEnd);

            var result = await _lifecycle.ExtendPilotAsync(
                seeded.RestaurantId,
                14,
                "key-original",
                now,
                "Support asked for a delay",
                0
            );

            Assert.Equal("extended", result.Outcome);
            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(originalEnd, after.PilotOriginalEndUtc);
            Assert.Equal(periodEnd.AddDays(14), after.PilotPeriodEnd);
        }

        [Fact]
        public async Task ExtendPilot_StaleExpectedCount_RefusedWithoutChange()
        {
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var periodEnd = now.AddDays(5);
            var seeded = await SeedPilotAsync(
                periodEnd,
                extensionCount: 1,
                extensionDaysTotal: 14
            );

            var result = await _lifecycle.ExtendPilotAsync(
                seeded.RestaurantId,
                14,
                "key-stale",
                now,
                "Support asked for a delay",
                0
            );

            Assert.Equal("refused", result.Outcome);
            Assert.Equal("stale_pilot_version", result.Code);
            Assert.Equal(1, result.CountAfter);
            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(periodEnd, after.PilotPeriodEnd);
            Assert.Equal(1, after.PilotExtensionCount);
            Assert.Equal(14, after.PilotExtensionDaysTotal);
            Assert.Null(after.LastPilotExtensionKey);
        }

        [Fact]
        public async Task ExtendPilot_MissingExpectedCount_RefusedAsStale()
        {
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var periodEnd = now.AddDays(5);
            var seeded = await SeedPilotAsync(periodEnd);

            var result = await _lifecycle.ExtendPilotAsync(
                seeded.RestaurantId,
                14,
                "key-no-version",
                now,
                "Support asked for a delay",
                null
            );

            Assert.Equal("refused", result.Outcome);
            Assert.Equal("stale_pilot_version", result.Code);
            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(periodEnd, after.PilotPeriodEnd);
            Assert.Equal(0, after.PilotExtensionCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("abcd")]
        [InlineData("  abc  ")]
        public async Task ExtendPilot_ReasonOutsideBounds_RefusedWithoutChange(string? reason)
        {
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var periodEnd = now.AddDays(5);
            var seeded = await SeedPilotAsync(periodEnd);

            var result = await _lifecycle.ExtendPilotAsync(
                seeded.RestaurantId,
                14,
                "key-reason",
                now,
                reason,
                0
            );

            Assert.Equal("refused", result.Outcome);
            Assert.Equal("pilot_extension_invalid_reason", result.Code);
            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(periodEnd, after.PilotPeriodEnd);
            Assert.Equal(0, after.PilotExtensionCount);
        }

        [Fact]
        public async Task ExtendPilot_ReasonOver500Characters_Refused()
        {
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedPilotAsync(now.AddDays(5));

            var result = await _lifecycle.ExtendPilotAsync(
                seeded.RestaurantId,
                14,
                "key-long-reason",
                now,
                new string('r', 501),
                0
            );

            Assert.Equal("pilot_extension_invalid_reason", result.Code);
        }

        [Theory]
        [InlineData("  Support asked for a delay  ", "Support asked for a delay")]
        [InlineData("abcde", "abcde")]
        [InlineData("abcd", null)]
        [InlineData("   ", null)]
        [InlineData(null, null)]
        public void NormalizePilotExtensionReason_TrimsAndBoundsLength(
            string? raw,
            string? expected
        )
        {
            Assert.Equal(
                expected,
                BillingAccountLifecycleService.NormalizePilotExtensionReason(raw)
            );
        }

        [Fact]
        public void NormalizePilotExtensionReason_AcceptsExactly500Characters()
        {
            var raw = new string('r', 500);

            Assert.Equal(raw, BillingAccountLifecycleService.NormalizePilotExtensionReason(raw));
            Assert.Null(BillingAccountLifecycleService.NormalizePilotExtensionReason(raw + "r"));
        }

        private async Task<int> AddActiveMemberAsync(int restaurantId, DateTime clock)
        {
            var member = new User
            {
                FullName = "Pilot Member",
                Email = $"member-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                ActivationExpiresAt = clock,
            };
            _context.Users.Add(member);
            await _context.SaveChangesAsync();

            _context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = member.Id,
                    RestaurantId = restaurantId,
                    PermissionRole = PermissionRoles.Staff,
                    Status = MembershipStatus.Active,
                }
            );
            await _context.SaveChangesAsync();
            return member.Id;
        }

        [Fact]
        public async Task RecoverDunning_ClearsGuestRetentionPurgedAt()
        {
            var start = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var dormantAt = start.AddHours(
                BillingAccountLifecycleService.DunningDormantHours
            );
            var seeded = await SeedPaidAsync();
            await _lifecycle.StartDunningEpisodeAsync(seeded.RestaurantId, start);
            await _lifecycle.TickAsync(seeded.RestaurantId, dormantAt);

            var account = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingStatuses.Dormant, account.BillingStatus);
            account.GuestRetentionPurgedAtUtc = dormantAt.AddDays(90);
            await _context.SaveChangesAsync();

            var now = dormantAt.AddDays(91);
            await _lifecycle.RecoverDunningAsync(seeded.RestaurantId, now);

            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingStatuses.Active, after.BillingStatus);
            Assert.Null(after.GuestRetentionPurgedAtUtc);
            Assert.Null(after.DormantEnteredAt);
        }

        [Fact]
        public async Task Tick_AfterPostCancelSoftLock_ReachesDormantAtPilotClock()
        {
            var renewalEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedPaidAsync();
            var account = await ReloadAsync(seeded.RestaurantId);
            account.SubscriptionPlan = BillingSubscriptionPlans.Growth;
            account.BillingCycle = BillingCycles.Monthly;
            account.BillingStatus = BillingStatuses.Active;
            await _context.SaveChangesAsync();

            _lifecycle.ApplyPostCancelSoftLock(account, renewalEnd);
            await _context.SaveChangesAsync();

            var dormantAt = renewalEnd.AddHours(
                BillingAccountLifecycleService.PilotDormantHours
            );
            await _lifecycle.TickAsync(seeded.RestaurantId, dormantAt);

            var after = await ReloadAsync(seeded.RestaurantId);
            Assert.Equal(BillingStatuses.Dormant, after.BillingStatus);
            Assert.Equal(renewalEnd, after.SoftLockEnteredAt);
            Assert.Equal(dormantAt, after.DormantEnteredAt);
            Assert.Single(_notifier.PilotLockEnters);
            Assert.Single(_notifier.PilotDormantEnters);
        }

        private async Task<SeededAccount> SeedPilotAsync(
            DateTime periodEnd,
            string status = BillingStatuses.Pilot,
            DateTime? startedAt = null,
            int extensionCount = 0,
            int extensionDaysTotal = 0,
            DateTime? originalEndUtc = null
        )
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
                    BillingStatus = status,
                    ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
                    StarterKitState = StarterKitStates.Unused,
                    PilotPeriodEnd = periodEnd,
                    PilotOriginalEndUtc = originalEndUtc,
                    PilotStartedAtUtc = startedAt,
                    PilotExtensionCount = extensionCount,
                    PilotExtensionDaysTotal = extensionDaysTotal,
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
                int used = 0,
            int remaining = 0,
            decimal usedShare = 0m,
            string? resetDateLabel = null,
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
