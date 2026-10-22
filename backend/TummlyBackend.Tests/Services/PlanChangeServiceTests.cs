using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class PlanChangeServiceTests
    {
        private const string PricebookId = "TUMMLY-UK-GBP-2026-08-V3";

        [Fact]
        public async Task ApplyImmediateSameCadenceUpgrade_StarterToGrowth_Aug16_GrantsFloorMigration()
        {
            var now = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);
            var periodStart = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            var harness = await SeedStarterAsync(now, periodStart, periodEnd);

            await harness.PlanChange.ApplyImmediateSameCadenceUpgradeAsync(
                harness.RestaurantId,
                BillingSubscriptionPlans.Growth
            );

            var account = await harness.Context.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == harness.RestaurantId);
            Assert.Equal(BillingSubscriptionPlans.Growth, account.SubscriptionPlan);
            Assert.Equal(PricebookId, account.ContractedPricebookId);
            Assert.Null(account.ScheduledTargetSubscriptionPlan);
            Assert.False(account.ScheduledCancelPlan);

            var migrations = await harness.Context.CreditLedgerEntries
                .Where(row =>
                    row.RestaurantId == harness.RestaurantId
                    && row.EntryType == CreditLedgerEntryTypes.PlanMigration
                )
                .ToListAsync();
            Assert.Equal(3, migrations.Count);

            var ai = Assert.Single(migrations, row => row.Channel == CreditChannels.Ai);
            Assert.Equal(206, ai.Quantity);
            Assert.Equal(periodStart, ai.PeriodStartUtc);
            Assert.Equal(periodEnd, ai.ExpiresAtUtc);
            Assert.Equal(PricebookId, ai.PricebookVersion);

            var email = Assert.Single(
                migrations,
                row => row.Channel == CreditChannels.Email
            );
            Assert.Equal(3_870, email.Quantity);

            var sms = Assert.Single(migrations, row => row.Channel == CreditChannels.Sms);
            Assert.Equal(129, sms.Quantity);

            var included = await harness.Context.CreditLedgerEntries
                .Where(row =>
                    row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                )
                .ToListAsync();
            Assert.All(
                included,
                row =>
                {
                    Assert.Equal(periodStart, row.PeriodStartUtc);
                    Assert.Equal(periodEnd, row.ExpiresAtUtc);
                }
            );
        }

        [Fact]
        public async Task ApplyImmediateSameCadenceUpgrade_SecondUpgrade_AppendsAnotherMigration()
        {
            var now = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);
            var periodStart = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            var harness = await SeedStarterAsync(now, periodStart, periodEnd);

            await harness.PlanChange.ApplyImmediateSameCadenceUpgradeAsync(
                harness.RestaurantId,
                BillingSubscriptionPlans.Growth
            );
            await harness.PlanChange.ApplyImmediateSameCadenceUpgradeAsync(
                harness.RestaurantId,
                BillingSubscriptionPlans.Group
            );

            var migrations = await harness.Context.CreditLedgerEntries
                .Where(row =>
                    row.EntryType == CreditLedgerEntryTypes.PlanMigration
                )
                .ToListAsync();
            Assert.Equal(6, migrations.Count);

            var account = await harness.Context.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == harness.RestaurantId);
            Assert.Equal(BillingSubscriptionPlans.Group, account.SubscriptionPlan);
        }

        [Fact]
        public async Task ApplyImmediateSameCadenceUpgrade_RatioZero_AppliesPlanWithoutMigration()
        {
            var now = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodStart = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            var harness = await SeedStarterAsync(now, periodStart, periodEnd);

            await harness.PlanChange.ApplyImmediateSameCadenceUpgradeAsync(
                harness.RestaurantId,
                BillingSubscriptionPlans.Growth
            );

            var account = await harness.Context.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == harness.RestaurantId);
            Assert.Equal(BillingSubscriptionPlans.Growth, account.SubscriptionPlan);
            Assert.Equal(PricebookId, account.ContractedPricebookId);

            var migrations = await harness.Context.CreditLedgerEntries
                .CountAsync(row =>
                    row.EntryType == CreditLedgerEntryTypes.PlanMigration
                );
            Assert.Equal(0, migrations);
        }

        [Fact]
        public async Task MintOnOrderCompleted_AppliesNonCancelScheduledChange_BeforeMint()
        {
            var now = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
            var harness = await SeedGrowthWithScheduleAsync(
                now,
                BillingSubscriptionPlans.Starter,
                BillingCycles.Monthly
            );

            var result = await harness.Mint.MintOnOrderCompletedAsync(
                new IncludedPeriodOrderCompletedRequest
                {
                    RestaurantId = harness.RestaurantId,
                    PaymentCompleted = true,
                    CycleStartUtc = now.Date,
                    NextCycleStartUtc = now.Date.AddMonths(1),
                }
            );

            Assert.True(result.Succeeded);
            Assert.Equal(3, result.InsertedAllocationIds.Count);

            var account = await harness.Context.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == harness.RestaurantId);
            Assert.Equal(BillingSubscriptionPlans.Starter, account.SubscriptionPlan);
            Assert.Null(account.ScheduledTargetSubscriptionPlan);

            var email = await harness.Context.CreditLedgerEntries
                .SingleAsync(row =>
                    row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                    && row.Channel == CreditChannels.Email
                );
            Assert.Equal(2_500, email.Quantity);
        }

        [Fact]
        public async Task MintOnOrderCompleted_GateFailed_DoesNotApplyOrMint()
        {
            var now = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
            var harness = await SeedGrowthWithScheduleAsync(
                now,
                BillingSubscriptionPlans.Starter,
                BillingCycles.Monthly,
                extraLocations: 3
            );

            var result = await harness.Mint.MintOnOrderCompletedAsync(
                new IncludedPeriodOrderCompletedRequest
                {
                    RestaurantId = harness.RestaurantId,
                    PaymentCompleted = true,
                    CycleStartUtc = now.Date,
                    NextCycleStartUtc = now.Date.AddMonths(1),
                }
            );

            Assert.Equal("scheduled_change_gate_failed", result.Code);

            var account = await harness.Context.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == harness.RestaurantId);
            Assert.Equal(BillingSubscriptionPlans.Growth, account.SubscriptionPlan);
            Assert.Equal(
                BillingSubscriptionPlans.Starter,
                account.ScheduledTargetSubscriptionPlan
            );
            Assert.Equal(
                0,
                await harness.Context.CreditLedgerEntries.CountAsync()
            );
        }

        private async Task<Harness> SeedStarterAsync(
            DateTime nowUtc,
            DateTime periodStart,
            DateTime periodEnd
        )
        {
            var context = CreateContext();
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Plan Change Owner",
                Role = "Owner",
                CreatedAt = nowUtc,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Paid Starter Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = nowUtc,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var billingAccount = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                PricebookId
            );
            billingAccount.SubscriptionPlan = BillingSubscriptionPlans.Starter;
            billingAccount.BillingCycle = BillingCycles.Monthly;
            billingAccount.BillingStatus = BillingStatuses.Active;
            context.BillingAccounts.Add(billingAccount);

            foreach (var channel in CreditChannels.All)
            {
                var qty = channel switch
                {
                    CreditChannels.Ai => 100,
                    CreditChannels.Email => 2_500,
                    CreditChannels.Sms => 100,
                    _ => 0,
                };
                context.CreditLedgerEntries.Add(
                    new CreditLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = restaurant.Id,
                        Channel = channel,
                        EntryType = CreditLedgerEntryTypes.IncludedAllocation,
                        Quantity = qty,
                        PricebookVersion = PricebookId,
                        PeriodStartUtc = periodStart,
                        ExpiresAtUtc = periodEnd,
                        CreatedAtUtc = periodStart,
                    }
                );
            }

            await context.SaveChangesAsync();

            var clock = new FixedTimeProvider(nowUtc);
            var pricebook = TestPricebookPaths.LoadV3();
            var planChange = new PlanChangeService(context, pricebook, clock);
            return new Harness(context, restaurant.Id, planChange, null!);
        }

        private async Task<Harness> SeedGrowthWithScheduleAsync(
            DateTime nowUtc,
            string scheduledPlan,
            string scheduledCycle,
            int extraLocations = 0
        )
        {
            var context = CreateContext();
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Schedule Owner",
                Role = "Owner",
                CreatedAt = nowUtc,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Paid Growth Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = nowUtc,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var billingAccount = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                PricebookId
            );
            billingAccount.SubscriptionPlan = BillingSubscriptionPlans.Growth;
            billingAccount.BillingCycle = BillingCycles.Monthly;
            billingAccount.BillingStatus = BillingStatuses.Active;
            billingAccount.HasScheduledChange = true;
            billingAccount.ScheduledTargetSubscriptionPlan = scheduledPlan;
            billingAccount.ScheduledTargetBillingCycle = scheduledCycle;
            billingAccount.ScheduledTargetExtraLocationCount = 0;
            context.BillingAccounts.Add(billingAccount);

            context.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = "Main",
                    Address = "1 High Street",
                    CreatedAt = nowUtc,
                }
            );
            for (var i = 0; i < extraLocations; i++)
            {
                context.RestaurantLocations.Add(
                    new RestaurantLocation
                    {
                        RestaurantId = restaurant.Id,
                        LocationName = $"Extra {i + 1}",
                        Address = $"{i + 2} High Street",
                        CreatedAt = nowUtc,
                    }
                );
            }

            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = owner.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.Owner,
                    Status = MembershipStatus.Active,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                }
            );
            await context.SaveChangesAsync();

            var clock = new FixedTimeProvider(nowUtc);
            var pricebook = TestPricebookPaths.LoadV3();
            var planChange = new PlanChangeService(context, pricebook, clock);
            var mint = new IncludedPeriodMintService(
                context,
                pricebook,
                clock,
                planChange: planChange
            );
            return new Harness(context, restaurant.Id, planChange, mint);
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            return new ApplicationDbContext(options);
        }

        private sealed record Harness(
            ApplicationDbContext Context,
            int RestaurantId,
            PlanChangeService PlanChange,
            IncludedPeriodMintService Mint
        );

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;

            public FixedTimeProvider(DateTime utcNow)
            {
                _utcNow = new DateTimeOffset(utcNow, TimeSpan.Zero);
            }

            public override DateTimeOffset GetUtcNow() => _utcNow;
        }
    }
}
