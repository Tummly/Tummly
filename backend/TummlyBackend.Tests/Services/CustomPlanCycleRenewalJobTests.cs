using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class CustomPlanCycleRenewalJobTests
    {
        private readonly IPricebookCatalog _pricebook = TestPricebookPaths.LoadV3();
        private readonly DateTime _now = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Process_CreatesOneOrder_StartsDunning_SecondRunIdempotent()
        {
            await using var context = CreateContext();
            var account = await SeedDueCustomAsync(context);
            var merchant = new RecordingRenewalMerchant();
            var job = CreateJob(context, merchant);

            var first = await job.ProcessAsync(_now);
            Assert.Equal(1, first.Processed);
            Assert.Equal(1, first.OrdersOpened);
            Assert.Equal(0, first.Failed);
            Assert.Equal(1, merchant.CreateOrderCallCount);

            await context.Entry(account).ReloadAsync();
            Assert.NotNull(account.DunningEpisodeStartedAt);
            Assert.False(string.IsNullOrWhiteSpace(account.DunningOutstandingOrderId));
            Assert.Equal(BillingStatuses.PastDue, account.BillingStatus);

            var second = await job.ProcessAsync(_now);
            Assert.Equal(1, second.Processed);
            Assert.Equal(0, second.OrdersOpened);
            Assert.Equal(1, merchant.CreateOrderCallCount);
            Assert.Equal(1, await context.RevolutOrderIntents.CountAsync());
        }

        [Fact]
        public async Task Process_PastSoftLockHours_SetsSoftLock_KeepsCustomPlan()
        {
            await using var context = CreateContext();
            var account = await SeedDueCustomAsync(context);
            var merchant = new RecordingRenewalMerchant();
            var job = CreateJob(context, merchant);

            await job.ProcessAsync(_now);
            await context.Entry(account).ReloadAsync();
            Assert.Equal(BillingStatuses.PastDue, account.BillingStatus);

            var softLockAt = account.DunningEpisodeStartedAt!.Value.AddHours(
                BillingAccountLifecycleService.DunningSoftLockHours
            );
            await job.ProcessAsync(softLockAt);

            await context.Entry(account).ReloadAsync();
            Assert.Equal(BillingStatuses.SoftLock, account.BillingStatus);
            Assert.Equal(BillingSubscriptionPlans.Custom, account.SubscriptionPlan);
        }

        [Fact]
        public async Task Process_SkipsWhenScheduledCancel()
        {
            await using var context = CreateContext();
            var account = await SeedDueCustomAsync(context);
            account.ScheduledCancelPlan = true;
            account.HasScheduledChange = true;
            await context.SaveChangesAsync();
            var merchant = new RecordingRenewalMerchant();
            var job = CreateJob(context, merchant);

            var result = await job.ProcessAsync(_now);

            Assert.Equal(0, result.Processed);
            Assert.Equal(0, merchant.CreateOrderCallCount);
        }

        private CustomPlanCycleRenewalJob CreateJob(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant
        )
        {
            var cycleBilling = new CustomPlanCycleBillingService(
                context,
                merchant,
                new FixedTimeProvider(_now),
                Options.Create(new TummlySellerVatSettings { IsActive = false })
            );
            var lifecycle = new BillingAccountLifecycleService(
                context,
                new NoOpBillingAccountNoticeNotifier()
            );
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://app.test",
                    }
                )
                .Build();
            return new CustomPlanCycleRenewalJob(
                context,
                cycleBilling,
                lifecycle,
                configuration,
                NullLogger<CustomPlanCycleRenewalJob>.Instance
            );
        }

        private async Task<BillingAccount> SeedDueCustomAsync(ApplicationDbContext context)
        {
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Renewal Owner",
                Role = "Owner",
                CreatedAt = _now,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Renewal Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = _now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var config = new CustomPlanConfig
            {
                CatalogPublicId = "cat",
                Cadence = "monthly",
                PeriodNetPence = 9_000,
                MonthlyNetPence = 9_000,
                Quantities = new CustomPlanQuantities
                {
                    Locations = 2,
                    TeamUsers = 3,
                    QrPlacements = 8,
                    Offers = 2,
                    EmailCredits = 1000,
                    SmsCredits = 40,
                    AiCredits = 40,
                },
            };

            var account = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                _pricebook.CurrentPricebookId
            );
            account.SubscriptionPlan = BillingSubscriptionPlans.Custom;
            account.BillingStatus = BillingStatuses.Active;
            account.BillingCycle = BillingCycles.Monthly;
            account.RevolutCustomerId = "cust_renew";
            account.CustomPlanConfigJson = config.ToJson();
            account.RenewalDateUtc = _now.AddDays(-1);
            context.BillingAccounts.Add(account);
            await context.SaveChangesAsync();
            return account;
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            return new ApplicationDbContext(options);
        }

        private sealed class RecordingRenewalMerchant : IRevolutMerchantClient
        {
            public const string CheckoutUrl =
                "https://checkout.revolut.com/payment-link/fake-renew";

            public int CreateOrderCallCount { get; private set; }

            public void EnsureReadyForCreate(string? planVariationLookupKey = null)
            {
            }

            public Task<RevolutListCustomersResult> ListCustomersByEmailAsync(
                string email,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(new RevolutListCustomersResult(Succeeded: true));

            public Task<RevolutMerchantCreateResult> CreateCustomerAsync(
                RevolutCreateCustomerRequest request,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(Succeeded: true, Id: "cust")
                );

            public Task<RevolutMerchantCreateResult> CreateSubscriptionAsync(
                RevolutCreateSubscriptionRequest request,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(Succeeded: true, Id: "sub")
                );

            public Task<RevolutMerchantCreateResult> CreateOrderAsync(
                RevolutCreateOrderRequest request,
                CancellationToken cancellationToken = default
            )
            {
                CreateOrderCallCount++;
                return Task.FromResult(
                    new RevolutMerchantCreateResult(
                        Succeeded: true,
                        Id: $"ord_renew_{CreateOrderCallCount}",
                        CheckoutUrl: CheckoutUrl
                    )
                );
            }

            public Task<RevolutMerchantCreateResult> ChangeSubscriptionPlanAsync(
                string subscriptionId,
                string planVariationLookupKey,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(Succeeded: true, Id: subscriptionId)
                );

            public Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
                string subscriptionId,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(Succeeded: true, Id: subscriptionId)
                );

            public Task<RevolutOrderRetrieveResult> GetOrderAsync(
                string orderId,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutOrderRetrieveResult(
                        Succeeded: true,
                        Id: orderId,
                        State: "pending",
                        CheckoutUrl: CheckoutUrl
                    )
                );

            public Task<RevolutMerchantCreateResult> UpdateOrderMerchantReferenceAsync(
                string orderId,
                string merchantReference,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(Succeeded: true, Id: orderId)
                );

            public Task<RevolutMerchantCreateResult> AcceptDisputeAsync(
                string disputeId,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(Succeeded: true, Id: disputeId)
                );

            public Task<RevolutMerchantCreateResult> ChallengeDisputeAsync(
                string disputeId,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(Succeeded: true, Id: disputeId)
                );
        }

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;

            public FixedTimeProvider(DateTime utcNow)
            {
                _utcNow = new DateTimeOffset(utcNow);
            }

            public override DateTimeOffset GetUtcNow() => _utcNow;
        }

        private sealed class NoOpBillingAccountNoticeNotifier
            : IBillingAccountNoticeNotifier
        {
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
            ) => Task.CompletedTask;

            public Task NotifyUnpaidPilotLockEnterAsync(
                int restaurantId,
                string episodeKey,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;

            public Task NotifyUnpaidPilotDormantEnterAsync(
                int restaurantId,
                string episodeKey,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;
        }
    }
}
