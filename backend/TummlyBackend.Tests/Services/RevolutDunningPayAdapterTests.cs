using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class RevolutDunningPayAdapterTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly RecordingDunningMerchant _merchant = new();
        private readonly RevolutDunningPayAdapter _adapter;

        public RevolutDunningPayAdapterTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            _context = new ApplicationDbContext(options);
            _adapter = new RevolutDunningPayAdapter(
                _context,
                _merchant,
                NullLogger<RevolutDunningPayAdapter>.Instance
            );
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public async Task Day0_PaysStoredOutstandingOrderId()
        {
            var seeded = await SeedOpenEpisodeAsync("ord_stored");
            _merchant.Orders["ord_stored"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_stored",
                State: "pending",
                SubscriptionId: seeded.SubscriptionId
            );
            _merchant.Subscriptions[seeded.SubscriptionId] =
                new RevolutSubscriptionRetrieveResult(
                    Succeeded: true,
                    Id: seeded.SubscriptionId,
                    State: "overdue",
                    CurrentCycleId: "cyc_1",
                    PaymentMethodId: "pm_1"
                );

            await _adapter.HandleDayStepAsync(seeded.RestaurantId, dayStep: 0);

            Assert.Equal(1, _merchant.PayOrderCallCount);
            Assert.Equal(["ord_stored"], _merchant.PayOrderIds);
            var account = await _context.BillingAccounts.SingleAsync();
            Assert.Equal(BillingStatuses.PastDue, account.BillingStatus);
            Assert.Equal("0", account.DunningFiredSteps);
        }

        [Fact]
        public async Task Day3_PaysStoredOutstandingOrderId()
        {
            var seeded = await SeedOpenEpisodeAsync("ord_day3");
            _merchant.Orders["ord_day3"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_day3",
                State: "pending",
                SubscriptionId: seeded.SubscriptionId
            );
            _merchant.Subscriptions[seeded.SubscriptionId] =
                new RevolutSubscriptionRetrieveResult(
                    Succeeded: true,
                    Id: seeded.SubscriptionId,
                    State: "overdue",
                    PaymentMethodId: "pm_1"
                );

            await _adapter.HandleDayStepAsync(seeded.RestaurantId, dayStep: 3);

            Assert.Equal(1, _merchant.PayOrderCallCount);
            Assert.Equal("ord_day3", _merchant.PayOrderIds.Single());
        }

        [Fact]
        public async Task Day7_DoesNotPay()
        {
            var seeded = await SeedOpenEpisodeAsync("ord_day7");
            _merchant.Orders["ord_day7"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_day7",
                State: "pending",
                SubscriptionId: seeded.SubscriptionId
            );

            await _adapter.HandleDayStepAsync(seeded.RestaurantId, dayStep: 7);

            Assert.Equal(0, _merchant.PayOrderCallCount);
            Assert.Equal(0, _merchant.GetOrderCallCount);
        }

        [Fact]
        public async Task FailedPay_DoesNotBumpDunningDay()
        {
            var seeded = await SeedOpenEpisodeAsync("ord_fail_pay");
            seeded.Account.DunningFiredSteps = "0";
            await _context.SaveChangesAsync();
            _merchant.Orders["ord_fail_pay"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_fail_pay",
                State: "pending",
                SubscriptionId: seeded.SubscriptionId
            );
            _merchant.Subscriptions[seeded.SubscriptionId] =
                new RevolutSubscriptionRetrieveResult(
                    Succeeded: true,
                    Id: seeded.SubscriptionId,
                    State: "overdue",
                    PaymentMethodId: "pm_1"
                );
            _merchant.PaySucceeds = false;

            await _adapter.HandleDayStepAsync(seeded.RestaurantId, dayStep: 0);

            Assert.Equal(1, _merchant.PayOrderCallCount);
            var account = await _context.BillingAccounts.AsNoTracking().SingleAsync();
            Assert.Equal("0", account.DunningFiredSteps);
            Assert.Equal(BillingStatuses.PastDue, account.BillingStatus);
            Assert.NotNull(account.DunningEpisodeStartedAt);
        }

        [Fact]
        public async Task SoftLockEnter_DoesNotCancelRevolutSubscription()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            await using var context = new ApplicationDbContext(options);
            var merchant = new RecordingDunningMerchant();
            var notifier = new NoOpNotifier();
            var lifecycle = new BillingAccountLifecycleService(
                context,
                notifier,
                new RevolutDunningPayAdapter(context, merchant)
            );

            var restaurant = await SeedRestaurantAsync(context);
            context.BillingAccounts.Add(
                new BillingAccount
                {
                    RestaurantId = restaurant.Id,
                    SubscriptionPlan = BillingSubscriptionPlans.Starter,
                    BillingCycle = BillingCycles.Monthly,
                    BillingStatus = BillingStatuses.PastDue,
                    ContractedPricebookId = "pb",
                    StarterKitState = StarterKitStates.Unused,
                    DunningEpisodeStartedAt = new DateTime(
                        2026,
                        8,
                        1,
                        0,
                        0,
                        0,
                        DateTimeKind.Utc
                    ),
                    DunningFiredSteps = "0,3,7",
                    DunningOutstandingOrderId = "ord_x",
                }
            );
            context.RevolutPendingPaySessions.Add(
                new RevolutPendingPaySession
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurant.Id,
                    TargetPlan = BillingSubscriptionPlans.Starter,
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = "sub_soft",
                    SetupOrderId = "ord_setup",
                    CheckoutUrl = "https://checkout.test",
                    IdempotencyKey = Guid.NewGuid().ToString("D"),
                    IsOpen = false,
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();

            var softLockAt = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(
                BillingAccountLifecycleService.DunningSoftLockHours
            );
            await lifecycle.TickAsync(restaurant.Id, softLockAt);

            var account = await context.BillingAccounts.AsNoTracking().SingleAsync();
            Assert.Equal(BillingStatuses.SoftLock, account.BillingStatus);
            Assert.Equal(0, merchant.CancelSubscriptionCallCount);
        }

        private async Task<Seeded> SeedOpenEpisodeAsync(string orderId)
        {
            var restaurant = await SeedRestaurantAsync(_context);
            var subscriptionId = $"sub_{orderId}";
            var account = new BillingAccount
            {
                RestaurantId = restaurant.Id,
                SubscriptionPlan = BillingSubscriptionPlans.Starter,
                BillingCycle = BillingCycles.Monthly,
                BillingStatus = BillingStatuses.PastDue,
                ContractedPricebookId = "pb",
                StarterKitState = StarterKitStates.Unused,
                RevolutCustomerId = $"cust_{orderId}",
                DunningEpisodeStartedAt = DateTime.UtcNow.AddDays(-1),
                DunningFiredSteps = "0",
                DunningOutstandingOrderId = orderId,
            };
            _context.BillingAccounts.Add(account);
            _context.RevolutPendingPaySessions.Add(
                new RevolutPendingPaySession
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurant.Id,
                    TargetPlan = BillingSubscriptionPlans.Starter,
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = subscriptionId,
                    SetupOrderId = $"setup_{orderId}",
                    CheckoutUrl = "https://checkout.test",
                    IdempotencyKey = Guid.NewGuid().ToString("D"),
                    IsOpen = false,
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync();
            return new Seeded(restaurant.Id, subscriptionId, account);
        }

        private static async Task<Restaurant> SeedRestaurantAsync(ApplicationDbContext context)
        {
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Dunning Owner",
                Role = "Owner",
                CreatedAt = DateTime.UtcNow,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();
            var restaurant = new Restaurant
            {
                Name = "Dunning Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();
            return restaurant;
        }

        private sealed record Seeded(
            int RestaurantId,
            string SubscriptionId,
            BillingAccount Account
        );

        private sealed class RecordingDunningMerchant : IRevolutMerchantClient
        {
            public Dictionary<string, RevolutOrderRetrieveResult> Orders { get; } =
                new(StringComparer.Ordinal);

            public Dictionary<string, RevolutSubscriptionRetrieveResult> Subscriptions { get; } =
                new(StringComparer.Ordinal);

            public bool PaySucceeds { get; set; } = true;

            public int GetOrderCallCount { get; private set; }

            public int PayOrderCallCount { get; private set; }

            public int CancelSubscriptionCallCount { get; private set; }

            public List<string> PayOrderIds { get; } = [];

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

            public Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
                string subscriptionId,
                CancellationToken cancellationToken = default
            )
            {
                CancelSubscriptionCallCount++;
                return Task.FromResult(
                    new RevolutMerchantCreateResult(Succeeded: true, Id: subscriptionId)
                );
            }

            public Task<RevolutOrderRetrieveResult> GetOrderAsync(
                string orderId,
                CancellationToken cancellationToken = default
            )
            {
                GetOrderCallCount++;
                return Task.FromResult(
                    Orders.TryGetValue(orderId, out var order)
                        ? order
                        : new RevolutOrderRetrieveResult(Succeeded: false, ErrorCode: "missing")
                );
            }

            public Task<RevolutSubscriptionRetrieveResult> GetSubscriptionAsync(
                string subscriptionId,
                CancellationToken cancellationToken = default
            )
            {
                return Task.FromResult(
                    Subscriptions.TryGetValue(subscriptionId, out var subscription)
                        ? subscription
                        : new RevolutSubscriptionRetrieveResult(
                            Succeeded: false,
                            ErrorCode: "missing"
                        )
                );
            }

            public Task<RevolutMerchantCreateResult> PayOrderAsync(
                RevolutPayOrderRequest request,
                CancellationToken cancellationToken = default
            )
            {
                PayOrderCallCount++;
                PayOrderIds.Add(request.OrderId);
                return Task.FromResult(
                    new RevolutMerchantCreateResult(
                        Succeeded: PaySucceeds,
                        Id: request.OrderId,
                        ErrorCode: PaySucceeds ? null : "pay_declined"
                    )
                );
            }
        }

        private sealed class NoOpNotifier : IBillingAccountNoticeNotifier
        {
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
