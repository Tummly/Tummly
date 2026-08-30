using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class SameCadenceUpgradePaySessionServiceTests
    {
        private readonly IPricebookCatalog _pricebook = TestPricebookPaths.LoadV3();
        private readonly DateTime _now = new(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task StartAsync_CreatesOrderWithVatLineItems_AndPersistsIntent()
        {
            await using var context = CreateContext();
            var account = await SeedPaidStarterAsync(context);
            var merchant = new RecordingUpgradeMerchant();
            var service = CreateService(context, merchant);

            var result = await service.StartAsync(
                account,
                "Single",
                locationId: 1,
                targetPlan: "Growth",
                targetCadenceApi: "monthly",
                idempotencyKey: "key-upgrade-1"
            );

            Assert.Equal("pay", result.Outcome);
            Assert.Equal(RecordingUpgradeMerchant.CheckoutUrl, result.RedirectUrl);
            Assert.Equal(1, merchant.CreateOrderCallCount);
            Assert.NotNull(merchant.LastCreateOrderRequest);
            Assert.Equal(
                "plan_upgrade_proration",
                (
                    await context.RevolutOrderIntents.SingleAsync()
                ).Purpose
            );
            Assert.True(merchant.LastCreateOrderRequest!.LineItems!.Count > 0);
            Assert.Equal(
                "VAT",
                merchant.LastCreateOrderRequest.LineItems[0].Taxes[0].Name
            );
            Assert.True(
                merchant.LastCreateOrderRequest.AmountMinor
                    == merchant.LastCreateOrderRequest.LineItems[0].TotalAmount
            );
        }

        [Fact]
        public async Task StartAsync_ReusesOpenIntent_WhenSameKeyAndTarget()
        {
            await using var context = CreateContext();
            var account = await SeedPaidStarterAsync(context);
            var merchant = new RecordingUpgradeMerchant();
            var service = CreateService(context, merchant);

            await service.StartAsync(
                account,
                "Single",
                1,
                "Growth",
                "monthly",
                "same-key"
            );
            var second = await service.StartAsync(
                account,
                "Single",
                1,
                "Growth",
                "monthly",
                "same-key"
            );

            Assert.Equal("pay", second.Outcome);
            Assert.Equal(1, merchant.CreateOrderCallCount);
            Assert.Equal(1, await context.RevolutOrderIntents.CountAsync());
        }

        [Fact]
        public async Task StartAsync_Rejects_WhenSameKeyDifferentTarget()
        {
            await using var context = CreateContext();
            var account = await SeedPaidStarterAsync(context);
            var merchant = new RecordingUpgradeMerchant();
            var service = CreateService(context, merchant);

            await service.StartAsync(
                account,
                "Single",
                1,
                "Growth",
                "monthly",
                "same-key"
            );

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.StartAsync(
                    account,
                    "Single",
                    1,
                    "Group",
                    "monthly",
                    "same-key"
                )
            );
            Assert.Equal("idempotency_target_mismatch", ex.Message);
        }

        [Fact]
        public async Task StartAsync_ClosesOpenDifferentTarget_ThenCreates()
        {
            await using var context = CreateContext();
            var account = await SeedPaidStarterAsync(context);
            var merchant = new RecordingUpgradeMerchant();
            var service = CreateService(context, merchant);

            await service.StartAsync(
                account,
                "Single",
                1,
                "Growth",
                "monthly",
                "key-a"
            );
            await service.StartAsync(
                account,
                "Single",
                1,
                "Group",
                "monthly",
                "key-b"
            );

            Assert.Equal(2, merchant.CreateOrderCallCount);
            Assert.Equal(2, await context.RevolutOrderIntents.CountAsync());
            Assert.Equal(
                1,
                await context.RevolutOrderIntents.CountAsync(row => row.IsOpen)
            );
        }

        private SameCadenceUpgradePaySessionService CreateService(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant
        )
        {
            return new SameCadenceUpgradePaySessionService(
                context,
                merchant,
                _pricebook,
                new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Frontend:BaseUrl"] = "https://app.test",
                        }
                    )
                    .Build(),
                new FixedClock(_now)
            );
        }

        private async Task<BillingAccount> SeedPaidStarterAsync(
            ApplicationDbContext context
        )
        {
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Upgrade Owner",
                Role = "Owner",
                CreatedAt = _now,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Upgrade Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = _now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var account = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                _pricebook.CurrentPricebookId
            );
            account.SubscriptionPlan = BillingSubscriptionPlans.Starter;
            account.BillingStatus = BillingStatuses.Active;
            account.BillingCycle = BillingCycles.Monthly;
            account.RevolutCustomerId = "cust_upgrade";
            account.RenewalDateUtc = new DateTime(
                2026,
                9,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc
            );
            context.BillingAccounts.Add(account);
            context.RevolutPendingPaySessions.Add(
                new RevolutPendingPaySession
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurant.Id,
                    TargetPlan = BillingSubscriptionPlans.Starter,
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = "sub_live",
                    SetupOrderId = "ord_setup_old",
                    CheckoutUrl = "https://checkout.revolut.test/old",
                    IdempotencyKey = "old",
                    IsOpen = false,
                    CreatedAtUtc = _now.AddDays(-10),
                }
            );
            await context.SaveChangesAsync();
            return account;
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

        private sealed class FixedClock : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;

            public FixedClock(DateTime utcNow)
            {
                _utcNow = new DateTimeOffset(utcNow, TimeSpan.Zero);
            }

            public override DateTimeOffset GetUtcNow() => _utcNow;
        }

        private sealed class RecordingUpgradeMerchant : IRevolutMerchantClient
        {
            public const string CheckoutUrl =
                "https://checkout.revolut.com/payment-link/upgrade";

            public int CreateOrderCallCount { get; private set; }

            public RevolutCreateOrderRequest? LastCreateOrderRequest { get; private set; }

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
            )
            {
                CreateOrderCallCount++;
                LastCreateOrderRequest = request;
                return Task.FromResult(
                    new RevolutMerchantCreateResult(
                        Succeeded: true,
                        Id: $"ord_up_{CreateOrderCallCount}",
                        CheckoutUrl: CheckoutUrl
                    )
                );
            }

            public Task<RevolutMerchantCreateResult> ChangeSubscriptionPlanAsync(
                string subscriptionId,
                string planVariationLookupKey,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
                string subscriptionId,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutOrderRetrieveResult> GetOrderAsync(
                string orderId,
                CancellationToken cancellationToken = default
            )
            {
                return Task.FromResult(
                    new RevolutOrderRetrieveResult(
                        Succeeded: true,
                        Id: orderId,
                        State: "pending",
                        CheckoutUrl: CheckoutUrl
                    )
                );
            }

            public Task<RevolutMerchantCreateResult> UpdateOrderMerchantReferenceAsync(
                string orderId,
                string merchantReference,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(Succeeded: true, Id: orderId)
                );
        }
    }
}
