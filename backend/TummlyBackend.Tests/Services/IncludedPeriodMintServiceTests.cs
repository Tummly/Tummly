using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class IncludedPeriodMintServiceTests
    {
        private const string PricebookId = "TUMMLY-UK-GBP-2026-08-V3";
        private readonly string _databaseName = Guid.NewGuid().ToString();
        private readonly DateTime _now = new(2026, 2, 15, 12, 0, 0, DateTimeKind.Utc);
        private readonly TimeProvider _clock;
        private readonly IPricebookCatalog _pricebook;

        public IncludedPeriodMintServiceTests()
        {
            _clock = new FixedTimeProvider(_now);
            _pricebook = PricebookCatalog.LoadFromDirectory(PackDirectory());
        }

        [Fact]
        public async Task MintOnOrderCompleted_Monthly_CompletedPayment_GrantsGrowthAllowance()
        {
            var harness = await SeedPaidAccountAsync(
                BillingSubscriptionPlans.Growth,
                BillingCycles.Monthly
            );
            var periodStart = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

            var result = await harness.Mint.MintOnOrderCompletedAsync(
                new IncludedPeriodOrderCompletedRequest
                {
                    RestaurantId = harness.RestaurantId,
                    PaymentCompleted = true,
                    CycleStartUtc = periodStart,
                    NextCycleStartUtc = periodEnd,
                }
            );

            Assert.True(result.Succeeded);
            Assert.Equal(3, result.InsertedAllocationIds.Count);

            var grants = await harness.Context.CreditLedgerEntries
                .Where(row =>
                    row.RestaurantId == harness.RestaurantId
                    && row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                )
                .ToListAsync();
            Assert.Equal(3, grants.Count);

            var ai = Assert.Single(grants, row => row.Channel == CreditChannels.Ai);
            Assert.Equal(500, ai.Quantity);
            Assert.Equal(periodStart, ai.PeriodStartUtc);
            Assert.Equal(periodEnd, ai.ExpiresAtUtc);
            Assert.Equal(_pricebook.CurrentPricebookId, ai.PricebookVersion);

            var email = Assert.Single(grants, row => row.Channel == CreditChannels.Email);
            Assert.Equal(10_000, email.Quantity);

            var sms = Assert.Single(grants, row => row.Channel == CreditChannels.Sms);
            Assert.Equal(350, sms.Quantity);
        }

        [Fact]
        public async Task MintOnOrderCompleted_FailedPayment_WritesNothing()
        {
            var harness = await SeedPaidAccountAsync(
                BillingSubscriptionPlans.Growth,
                BillingCycles.Monthly
            );

            var result = await harness.Mint.MintOnOrderCompletedAsync(
                new IncludedPeriodOrderCompletedRequest
                {
                    RestaurantId = harness.RestaurantId,
                    PaymentCompleted = false,
                    CycleStartUtc = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    NextCycleStartUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                }
            );

            Assert.True(result.Succeeded);
            Assert.Equal("payment_not_completed", result.Code);
            Assert.Empty(result.InsertedAllocationIds);

            var count = await harness.Context.CreditLedgerEntries.CountAsync();
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task MintOnOrderCompleted_Annual_CompletedPayment_MintsSliceZeroOnly()
        {
            var yearStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var yearEnd = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var harness = await SeedPaidAccountAsync(
                BillingSubscriptionPlans.Growth,
                BillingCycles.Annual,
                new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)
            );

            var result = await harness.Mint.MintOnOrderCompletedAsync(
                new IncludedPeriodOrderCompletedRequest
                {
                    RestaurantId = harness.RestaurantId,
                    PaymentCompleted = true,
                    CycleStartUtc = yearStart,
                    CycleEndUtc = yearEnd,
                }
            );

            Assert.True(result.Succeeded);
            Assert.Equal(3, result.InsertedAllocationIds.Count);

            var grants = await harness.Context.CreditLedgerEntries
                .Where(row =>
                    row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                )
                .ToListAsync();
            Assert.All(
                grants,
                row =>
                {
                    Assert.Equal(yearStart, row.PeriodStartUtc);
                    Assert.Equal(yearStart.AddMonths(1), row.ExpiresAtUtc);
                }
            );
        }

        [Fact]
        public async Task ProcessJobForRestaurant_AnnualActive_MintsCurrentOpenSliceOne()
        {
            var yearStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var yearEnd = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var harness = await SeedPaidAccountAsync(
                BillingSubscriptionPlans.Growth,
                BillingCycles.Annual,
                new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)
            );

            var sliceZero = await harness.Mint.MintOnOrderCompletedAsync(
                new IncludedPeriodOrderCompletedRequest
                {
                    RestaurantId = harness.RestaurantId,
                    PaymentCompleted = true,
                    CycleStartUtc = yearStart,
                    CycleEndUtc = yearEnd,
                }
            );
            Assert.Equal(3, sliceZero.InsertedAllocationIds.Count);

            var result = await harness.Mint.ProcessJobForRestaurantAsync(
                harness.RestaurantId,
                new DateTime(2026, 2, 15, 12, 0, 0, DateTimeKind.Utc)
            );

            Assert.True(result.Succeeded);
            Assert.Equal(3, result.InsertedAllocationIds.Count);

            var sliceOneStart = yearStart.AddMonths(1);
            var sliceOneEnd = yearStart.AddMonths(2);
            var grants = await harness.Context.CreditLedgerEntries
                .Where(row =>
                    row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                    && row.PeriodStartUtc == sliceOneStart
                )
                .ToListAsync();
            Assert.Equal(3, grants.Count);
            Assert.All(grants, row => Assert.Equal(sliceOneEnd, row.ExpiresAtUtc));
        }

        [Fact]
        public async Task ProcessJobForRestaurant_Monthly_ExpiresEndedIncludedLeftoverOnly()
        {
            var harness = await SeedPaidAccountAsync(
                BillingSubscriptionPlans.Growth,
                BillingCycles.Monthly
            );
            var oldStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var oldEnd = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            var oldGrantId = await InsertIncludedGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditChannels.Email,
                quantity: 10_000,
                oldStart,
                oldEnd
            );

            var result = await harness.Mint.ProcessJobForRestaurantAsync(
                harness.RestaurantId
            );

            Assert.True(result.Succeeded);
            Assert.Empty(result.InsertedAllocationIds);
            Assert.Equal(1, result.ExpiryRowsWritten);

            var expiry = await harness.Context.CreditLedgerEntries
                .SingleAsync(row =>
                    row.EntryType == CreditLedgerEntryTypes.Expiry
                    && row.AllocationId == oldGrantId
                );
            Assert.Equal(10_000, expiry.Quantity);

            Assert.Empty(
                await harness.Context.CreditLedgerEntries
                    .Where(row =>
                        row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                        && row.PeriodStartUtc != oldStart
                    )
                    .ToListAsync()
            );
        }

        [Fact]
        public async Task ProcessJobForRestaurant_CancelAtRenewalDate_AppliesAndSkipsMint()
        {
            var renewal = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var harness = await SeedPaidAccountAsync(
                BillingSubscriptionPlans.Growth,
                BillingCycles.Annual,
                utcNow: renewal
            );
            var account = await harness.Context.BillingAccounts.SingleAsync();
            account.ScheduledCancelPlan = true;
            account.RenewalDateUtc = renewal;
            account.HasScheduledChange = true;
            account.ScheduledTargetSubscriptionPlan = BillingSubscriptionPlans.Starter;
            await harness.Context.SaveChangesAsync();

            var yearStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await InsertIncludedGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditChannels.Email,
                10_000,
                yearStart,
                yearStart.AddMonths(1)
            );

            var result = await harness.Mint.ProcessJobForRestaurantAsync(
                harness.RestaurantId,
                nowUtc: renewal
            );

            Assert.True(result.Succeeded);
            Assert.Empty(result.InsertedAllocationIds);

            await harness.Context.Entry(account).ReloadAsync();
            Assert.False(account.ScheduledCancelPlan);
            Assert.False(account.HasScheduledChange);
            Assert.Null(account.ScheduledTargetSubscriptionPlan);

            var newGrants = await harness.Context.CreditLedgerEntries
                .Where(row =>
                    row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                    && row.PeriodStartUtc == yearStart.AddMonths(2)
                )
                .ToListAsync();
            Assert.Empty(newGrants);
        }

        [Fact]
        public async Task ProcessJobForRestaurant_CancelBeforeRenewalDate_DoesNotApply()
        {
            var renewal = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var beforeRenewal = new DateTime(2026, 2, 15, 12, 0, 0, DateTimeKind.Utc);
            var harness = await SeedPaidAccountAsync(
                BillingSubscriptionPlans.Growth,
                BillingCycles.Annual,
                utcNow: beforeRenewal
            );
            var account = await harness.Context.BillingAccounts.SingleAsync();
            account.ScheduledCancelPlan = true;
            account.RenewalDateUtc = renewal;
            await harness.Context.SaveChangesAsync();

            var yearStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await InsertIncludedGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditChannels.Email,
                10_000,
                yearStart,
                yearStart.AddMonths(1)
            );
            await InsertIncludedGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditChannels.Ai,
                500,
                yearStart,
                yearStart.AddMonths(1)
            );
            await InsertIncludedGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditChannels.Sms,
                350,
                yearStart,
                yearStart.AddMonths(1)
            );

            var result = await harness.Mint.ProcessJobForRestaurantAsync(
                harness.RestaurantId,
                nowUtc: beforeRenewal
            );

            Assert.True(result.Succeeded);
            Assert.Equal(3, result.InsertedAllocationIds.Count);

            await harness.Context.Entry(account).ReloadAsync();
            Assert.True(account.ScheduledCancelPlan);
        }

        [Fact]
        public async Task MintOnOrderCompleted_AfterPilot_UsesCurrentPricebook()
        {
            var harness = await SeedPaidAccountAsync(
                BillingSubscriptionPlans.Growth,
                BillingCycles.Monthly
            );
            harness.Context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    EntryType = CreditLedgerEntryTypes.PilotAllocation,
                    Quantity = 500,
                    PricebookVersion = PricebookId,
                    CreatedAtUtc = _now.AddDays(-20),
                }
            );
            await harness.Context.SaveChangesAsync();

            var periodStart = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var result = await harness.Mint.MintOnOrderCompletedAsync(
                new IncludedPeriodOrderCompletedRequest
                {
                    RestaurantId = harness.RestaurantId,
                    PaymentCompleted = true,
                    CycleStartUtc = periodStart,
                    NextCycleStartUtc = periodEnd,
                }
            );

            Assert.True(result.Succeeded);
            Assert.Equal(3, result.InsertedAllocationIds.Count);

            var grants = await harness.Context.CreditLedgerEntries
                .Where(row =>
                    row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                )
                .ToListAsync();
            Assert.All(
                grants,
                row => Assert.Equal(_pricebook.CurrentPricebookId, row.PricebookVersion)
            );
        }

        [Fact]
        public async Task MintOnOrderCompleted_AmbientTransaction_SharesCallerLock()
        {
            var harness = await SeedPaidAccountAsync(
                BillingSubscriptionPlans.Growth,
                BillingCycles.Monthly
            );
            var periodStart = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

            await using var ambient =
                await harness.Context.Database.BeginTransactionAsync();
            harness.Context.BillingAccounts.First().BillingEmail = "billing@example.com";
            await harness.Context.SaveChangesAsync();

            var result = await harness.Mint.MintOnOrderCompletedAsync(
                new IncludedPeriodOrderCompletedRequest
                {
                    RestaurantId = harness.RestaurantId,
                    PaymentCompleted = true,
                    CycleStartUtc = periodStart,
                    NextCycleStartUtc = periodEnd,
                }
            );

            Assert.True(result.Succeeded);
            Assert.Equal(3, result.InsertedAllocationIds.Count);

            await ambient.CommitAsync();

            var email = await harness.Context.BillingAccounts
                .AsNoTracking()
                .Where(row => row.RestaurantId == harness.RestaurantId)
                .Select(row => row.BillingEmail)
                .SingleAsync();
            Assert.Equal("billing@example.com", email);
            Assert.Equal(
                3,
                await harness.Context.CreditLedgerEntries.CountAsync(row =>
                    row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                )
            );
        }

        [Fact]
        public async Task MintOnOrderCompleted_SkipsWhenGrantExistsForExpiresAtUtc()
        {
            var harness = await SeedPaidAccountAsync(
                BillingSubscriptionPlans.Growth,
                BillingCycles.Monthly
            );
            var periodStart = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

            await InsertIncludedGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditChannels.Email,
                quantity: 10_000,
                periodStart,
                periodEnd
            );

            var result = await harness.Mint.MintOnOrderCompletedAsync(
                new IncludedPeriodOrderCompletedRequest
                {
                    RestaurantId = harness.RestaurantId,
                    PaymentCompleted = true,
                    CycleStartUtc = periodStart,
                    NextCycleStartUtc = periodEnd,
                }
            );

            Assert.True(result.Succeeded);
            Assert.Equal(2, result.InsertedAllocationIds.Count);

            var emailGrants = await harness.Context.CreditLedgerEntries
                .Where(row =>
                    row.Channel == CreditChannels.Email
                    && row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                    && row.ExpiresAtUtc == periodEnd
                )
                .ToListAsync();
            Assert.Single(emailGrants);
        }

        [Fact]
        public async Task MintOnOrderCompleted_ExpiresEndedIncludedLeftover_ThenGrantsNewPeriod()
        {
            var harness = await SeedPaidAccountAsync(
                BillingSubscriptionPlans.Growth,
                BillingCycles.Monthly
            );
            var oldStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var oldEnd = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            var oldGrantId = await InsertIncludedGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditChannels.Email,
                quantity: 10_000,
                oldStart,
                oldEnd
            );
            harness.Context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    EntryType = CreditLedgerEntryTypes.Consumption,
                    Quantity = 2_500,
                    AllocationId = oldGrantId,
                    LocationId = harness.LocationId,
                    CreatedAtUtc = oldStart.AddDays(5),
                }
            );
            await harness.Context.SaveChangesAsync();

            var newStart = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            var newEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var result = await harness.Mint.MintOnOrderCompletedAsync(
                new IncludedPeriodOrderCompletedRequest
                {
                    RestaurantId = harness.RestaurantId,
                    PaymentCompleted = true,
                    CycleStartUtc = newStart,
                    NextCycleStartUtc = newEnd,
                }
            );

            Assert.True(result.Succeeded);
            Assert.Equal(3, result.InsertedAllocationIds.Count);
            Assert.Equal(1, result.ExpiryRowsWritten);

            var expiry = await harness.Context.CreditLedgerEntries
                .SingleAsync(row =>
                    row.EntryType == CreditLedgerEntryTypes.Expiry
                    && row.AllocationId == oldGrantId
                );
            Assert.Equal(7_500, expiry.Quantity);

            var newGrant = await harness.Context.CreditLedgerEntries
                .SingleAsync(row =>
                    row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                    && row.PeriodStartUtc == newStart
                    && row.Channel == CreditChannels.Email
                );
            Assert.Equal(10_000, newGrant.Quantity);
        }

        private async Task<Harness> SeedPaidAccountAsync(
            string subscriptionPlan,
            string billingCycle,
            DateTime? utcNow = null
        )
        {
            var clock = new FixedTimeProvider(utcNow ?? _now);
            var context = CreateContext();
            var now = clock.GetUtcNow().UtcDateTime;
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Mint Owner",
                Role = "Owner",
                CreatedAt = now,
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
                CreatedAt = now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var billingAccount = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                PricebookId
            );
            billingAccount.SubscriptionPlan = subscriptionPlan;
            billingAccount.BillingCycle = billingCycle;
            billingAccount.BillingStatus = BillingStatuses.Active;
            context.BillingAccounts.Add(billingAccount);

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            return new Harness(
                context,
                new IncludedPeriodMintService(context, _pricebook, clock),
                restaurant.Id,
                location.Id
            );
        }

        private static async Task<Guid> InsertIncludedGrantAsync(
            ApplicationDbContext context,
            int restaurantId,
            string channel,
            int quantity,
            DateTime periodStartUtc,
            DateTime expiresAtUtc
        )
        {
            var id = Guid.NewGuid();
            context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = id,
                    RestaurantId = restaurantId,
                    Channel = channel,
                    EntryType = CreditLedgerEntryTypes.IncludedAllocation,
                    Quantity = quantity,
                    PricebookVersion = PricebookId,
                    PeriodStartUtc = periodStartUtc,
                    ExpiresAtUtc = expiresAtUtc,
                    CreatedAtUtc = periodStartUtc,
                }
            );
            await context.SaveChangesAsync();
            return id;
        }

        private ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(_databaseName)
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            return new ApplicationDbContext(options);
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

        private sealed record Harness(
            ApplicationDbContext Context,
            IncludedPeriodMintService Mint,
            int RestaurantId,
            int LocationId
        );

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;

            public FixedTimeProvider(DateTime utcNow)
            {
                _utcNow = new DateTimeOffset(utcNow, TimeSpan.Zero);
            }

            public override DateTimeOffset GetUtcNow()
            {
                return _utcNow;
            }
        }
    }
}
