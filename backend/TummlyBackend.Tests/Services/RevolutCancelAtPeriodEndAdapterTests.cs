using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Ticket 23: Revolut cancel only at period end, not on confirm day.
    /// </summary>
    public sealed class RevolutCancelAtPeriodEndAdapterTests
    {
        private readonly string _databaseName = Guid.NewGuid().ToString("N");
        private readonly IPricebookCatalog _pricebook;
        private readonly DateTime _now = new(2026, 2, 15, 12, 0, 0, DateTimeKind.Utc);

        public RevolutCancelAtPeriodEndAdapterTests()
        {
            _pricebook = PricebookCatalog.LoadFromDirectory(PackDirectory());
        }

        [Fact]
        public async Task ProcessJob_AtRenewal_CallsRevolutCancel_WithSubscriptionId()
        {
            var renewal = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var merchant = new RecordingCancelMerchant();
            var harness = await SeedPaidWithSubscriptionAsync(
                merchant,
                utcNow: renewal,
                subscriptionId: "sub_live_period_end"
            );
            var account = await harness.Context.BillingAccounts.SingleAsync();
            account.ScheduledCancelPlan = true;
            account.HasScheduledChange = true;
            account.RenewalDateUtc = renewal;
            await harness.Context.SaveChangesAsync();

            var yearStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await InsertIncludedGrantAsync(
                harness.Context,
                harness.RestaurantId,
                yearStart,
                yearStart.AddMonths(1)
            );

            var result = await harness.Mint.ProcessJobForRestaurantAsync(
                harness.RestaurantId,
                nowUtc: renewal
            );

            Assert.True(result.Succeeded);
            Assert.Equal(1, merchant.CancelCallCount);
            Assert.Equal("sub_live_period_end", merchant.LastCancelledSubscriptionId);

            await harness.Context.Entry(account).ReloadAsync();
            Assert.False(account.ScheduledCancelPlan);
        }

        [Fact]
        public async Task ProcessJob_BeforeRenewal_DoesNotCallRevolutCancel()
        {
            var renewal = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var beforeRenewal = new DateTime(2026, 2, 15, 12, 0, 0, DateTimeKind.Utc);
            var merchant = new RecordingCancelMerchant();
            var harness = await SeedPaidWithSubscriptionAsync(
                merchant,
                utcNow: beforeRenewal,
                subscriptionId: "sub_still_active"
            );
            var account = await harness.Context.BillingAccounts.SingleAsync();
            account.ScheduledCancelPlan = true;
            account.HasScheduledChange = true;
            account.RenewalDateUtc = renewal;
            await harness.Context.SaveChangesAsync();

            var yearStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await InsertIncludedGrantAsync(
                harness.Context,
                harness.RestaurantId,
                yearStart,
                yearStart.AddMonths(1)
            );

            var result = await harness.Mint.ProcessJobForRestaurantAsync(
                harness.RestaurantId,
                nowUtc: beforeRenewal
            );

            Assert.True(result.Succeeded);
            Assert.Equal(0, merchant.CancelCallCount);

            await harness.Context.Entry(account).ReloadAsync();
            Assert.True(account.ScheduledCancelPlan);
        }

        [Fact]
        public async Task Adapter_CancelNative_CallsMerchant_WhenSubscriptionCorrelated()
        {
            var merchant = new RecordingCancelMerchant();
            var context = CreateContext();
            var restaurant = new Restaurant
            {
                Name = "Adapter Sync Cafe",
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = _now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    "TUMMLY-UK-GBP-2026-08-V3"
                )
            );
            context.RevolutPendingPaySessions.Add(
                new RevolutPendingPaySession
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurant.Id,
                    TargetPlan = BillingSubscriptionPlans.Growth,
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = "sub_confirm_never",
                    SetupOrderId = "ord_x",
                    CheckoutUrl = "https://checkout.revolut.com/x",
                    IdempotencyKey = "k",
                    IsOpen = false,
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();

            var adapter = new RevolutCancelAtPeriodEndAdapter(context, merchant);
            await adapter.CancelNativeSubscriptionAsync(restaurant.Id);

            Assert.Equal(1, merchant.CancelCallCount);
            Assert.Equal("sub_confirm_never", merchant.LastCancelledSubscriptionId);
        }

        [Fact]
        public async Task Adapter_CancelNative_ClosesOpenPaySessions_ForSubscription()
        {
            var merchant = new RecordingCancelMerchant();
            var context = CreateContext();
            var restaurant = new Restaurant
            {
                Name = "Sync Cancel Cafe",
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = _now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    "TUMMLY-UK-GBP-2026-08-V3"
                )
            );
            var sessionId = Guid.NewGuid();
            context.RevolutPendingPaySessions.Add(
                new RevolutPendingPaySession
                {
                    Id = sessionId,
                    RestaurantId = restaurant.Id,
                    TargetPlan = BillingSubscriptionPlans.Growth,
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = "sub_open_sync",
                    SetupOrderId = "ord_open",
                    CheckoutUrl = "https://checkout.revolut.com/open",
                    IdempotencyKey = "open-key",
                    IsOpen = true,
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();

            var adapter = new RevolutCancelAtPeriodEndAdapter(context, merchant);
            await adapter.CancelNativeSubscriptionAsync(restaurant.Id);

            Assert.Equal(1, merchant.CancelCallCount);
            var session = await context.RevolutPendingPaySessions
                .SingleAsync(row => row.Id == sessionId);
            Assert.False(session.IsOpen);
        }

        private async Task<Harness> SeedPaidWithSubscriptionAsync(
            RecordingCancelMerchant merchant,
            DateTime utcNow,
            string subscriptionId
        )
        {
            var context = CreateContext();
            var clock = new FixedTimeProvider(utcNow);
            var restaurant = new Restaurant
            {
                Name = "Cancel Revolut Cafe",
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = utcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var billingAccount = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                "TUMMLY-UK-GBP-2026-08-V3"
            );
            billingAccount.SubscriptionPlan = BillingSubscriptionPlans.Growth;
            billingAccount.BillingCycle = BillingCycles.Annual;
            billingAccount.BillingStatus = BillingStatuses.Active;
            context.BillingAccounts.Add(billingAccount);

            context.RevolutPendingPaySessions.Add(
                new RevolutPendingPaySession
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurant.Id,
                    TargetPlan = BillingSubscriptionPlans.Growth,
                    TargetCadence = "annual",
                    RevolutSubscriptionId = subscriptionId,
                    SetupOrderId = "ord_setup_cancel",
                    CheckoutUrl = "https://checkout.revolut.com/done",
                    IdempotencyKey = "cancel-period-end-key",
                    IsOpen = false,
                    CreatedAtUtc = utcNow.AddDays(-30),
                }
            );
            await context.SaveChangesAsync();

            var adapter = new RevolutCancelAtPeriodEndAdapter(context, merchant);
            var mint = new IncludedPeriodMintService(
                context,
                _pricebook,
                clock,
                revolutCancel: adapter
            );

            return new Harness(context, mint, restaurant.Id);
        }

        private static async Task InsertIncludedGrantAsync(
            ApplicationDbContext context,
            int restaurantId,
            DateTime periodStartUtc,
            DateTime expiresAtUtc
        )
        {
            context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurantId,
                    Channel = CreditChannels.Email,
                    EntryType = CreditLedgerEntryTypes.IncludedAllocation,
                    Quantity = 10_000,
                    PricebookVersion = "TUMMLY-UK-GBP-2026-08-V3",
                    PeriodStartUtc = periodStartUtc,
                    ExpiresAtUtc = expiresAtUtc,
                    CreatedAtUtc = periodStartUtc,
                }
            );
            await context.SaveChangesAsync();
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
            int RestaurantId
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

        private sealed class RecordingCancelMerchant : IRevolutMerchantClient
        {
            public int CancelCallCount { get; private set; }

            public string? LastCancelledSubscriptionId { get; private set; }

            public void EnsureReadyForCreate(string? planVariationLookupKey = null)
            {
            }

            public Task<RevolutListCustomersResult> ListCustomersByEmailAsync(
                string email,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> CreateCustomerAsync(
                RevolutCreateCustomerRequest request,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> CreateSubscriptionAsync(
                RevolutCreateSubscriptionRequest request,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> CreateOrderAsync(
                RevolutCreateOrderRequest request,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> ChangeSubscriptionPlanAsync(
                string subscriptionId,
                string planVariationLookupKey,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
                string subscriptionId,
                CancellationToken cancellationToken = default
            )
            {
                CancelCallCount++;
                LastCancelledSubscriptionId = subscriptionId;
                return Task.FromResult(
                    new RevolutMerchantCreateResult(
                        Succeeded: true,
                        Id: subscriptionId
                    )
                );
            }

            public Task<RevolutOrderRetrieveResult> GetOrderAsync(
                string orderId,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> UpdateOrderMerchantReferenceAsync(
                string orderId,
                string merchantReference,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();
        }
    }
}
