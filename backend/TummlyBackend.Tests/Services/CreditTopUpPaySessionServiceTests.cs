using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class CreditTopUpPaySessionServiceTests
    {
        private readonly IPricebookCatalog _pricebook = TestPricebookPaths.LoadV3();
        private readonly DateTime _now = new(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task StartAsync_CreatesOrderWithFriendlyName_AndPersistsTopupIntent()
        {
            await using var context = CreateContext();
            var account = await SeedPaidStarterAsync(context);
            var merchant = new RecordingTopUpMerchant();
            var service = CreateService(context, merchant);
            var pack = RequirePack("ai", 500);

            var checkout = await service.StartAsync(
                account,
                "Multi",
                locationId: 7,
                pack,
                "key-topup-1"
            );

            Assert.Equal(RecordingTopUpMerchant.CheckoutUrl, checkout);
            Assert.Equal(1, merchant.CreateOrderCallCount);
            Assert.NotNull(merchant.LastCreateOrderRequest);
            Assert.Equal(
                account.RevolutCustomerId,
                merchant.LastCreateOrderRequest!.CustomerId
            );
            Assert.Equal(
                "AI Credits 500 Topup",
                merchant.LastCreateOrderRequest.Description
            );
            Assert.Equal(
                "AI Credits 500 Topup",
                merchant.LastCreateOrderRequest.LineItems![0].Name
            );
            Assert.True(
                string.IsNullOrWhiteSpace(
                    merchant.LastCreateOrderRequest.LineItems[0].ExternalId
                )
            );
            Assert.Contains(
                "tab=credits-usage",
                merchant.LastCreateOrderRequest.RedirectUrl
            );
            Assert.Contains(
                "topUpOutcome=success",
                merchant.LastCreateOrderRequest.RedirectUrl
            );

            var intent = await context.RevolutOrderIntents.SingleAsync();
            Assert.Equal(RevolutOrderIntentPurposes.Topup, intent.Purpose);
            Assert.Equal("ai", intent.Channel);
            Assert.Equal(500, intent.Quantity);
            Assert.Equal(pack.LookupKey, intent.PackLookupKey);
            Assert.True(intent.IsOpen);
            Assert.Equal(string.Empty, intent.TargetPlan);
            Assert.Equal(string.Empty, intent.RevolutSubscriptionId);
        }

        [Fact]
        public async Task StartAsync_ReusesOpenIntent_WhenSameKeyAndPack()
        {
            await using var context = CreateContext();
            var account = await SeedPaidStarterAsync(context);
            var merchant = new RecordingTopUpMerchant();
            var service = CreateService(context, merchant);
            var pack = RequirePack("ai", 500);

            await service.StartAsync(account, "Single", 1, pack, "same-key");
            var second = await service.StartAsync(
                account,
                "Single",
                1,
                pack,
                "same-key"
            );

            Assert.Equal(RecordingTopUpMerchant.CheckoutUrl, second);
            Assert.Equal(1, merchant.CreateOrderCallCount);
            Assert.Equal(1, await context.RevolutOrderIntents.CountAsync());
        }

        [Fact]
        public async Task StartAsync_Rejects_WhenSameKeyDifferentPack()
        {
            await using var context = CreateContext();
            var account = await SeedPaidStarterAsync(context);
            var merchant = new RecordingTopUpMerchant();
            var service = CreateService(context, merchant);

            await service.StartAsync(
                account,
                "Single",
                1,
                RequirePack("ai", 500),
                "same-key"
            );

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.StartAsync(
                    account,
                    "Single",
                    1,
                    RequirePack("ai", 100),
                    "same-key"
                )
            );
            Assert.Equal("idempotency_target_mismatch", ex.Message);
            Assert.Equal(1, merchant.CreateOrderCallCount);
        }

        [Fact]
        public async Task StartAsync_NewKey_LeavesPriorOpenWithoutAllocate()
        {
            await using var context = CreateContext();
            var account = await SeedPaidStarterAsync(context);
            var merchant = new RecordingTopUpMerchant();
            var service = CreateService(context, merchant);
            var pack = RequirePack("email", 5000);

            await service.StartAsync(account, "Single", 1, pack, "key-a");
            await service.StartAsync(account, "Single", 1, pack, "key-b");

            Assert.Equal(2, merchant.CreateOrderCallCount);
            Assert.Equal(2, await context.RevolutOrderIntents.CountAsync());
            Assert.Equal(
                2,
                await context.RevolutOrderIntents.CountAsync(row => row.IsOpen)
            );
            Assert.Equal(
                0,
                await context.CreditLedgerEntries.CountAsync(row =>
                    row.EntryType == CreditLedgerEntryTypes.TopupAllocation
                )
            );
        }

        [Fact]
        public async Task StartAsync_DoesNotAllocateCredits()
        {
            await using var context = CreateContext();
            var account = await SeedPaidStarterAsync(context);
            var merchant = new RecordingTopUpMerchant();
            var service = CreateService(context, merchant);

            await service.StartAsync(
                account,
                "Single",
                1,
                RequirePack("sms", 100),
                "key-no-mint"
            );

            Assert.Equal(
                0,
                await context.CreditLedgerEntries.CountAsync(row =>
                    row.RestaurantId == account.RestaurantId
                )
            );
        }

        [Fact]
        public async Task StartAsync_Refuses_WhenCustomerMissing()
        {
            await using var context = CreateContext();
            var account = await SeedPaidStarterAsync(context);
            account.RevolutCustomerId = null;
            await context.SaveChangesAsync();
            var service = CreateService(context, new RecordingTopUpMerchant());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.StartAsync(
                    account,
                    "Single",
                    1,
                    RequirePack("ai", 100),
                    "key"
                )
            );
            Assert.Equal("revolut_customer_required", ex.Message);
        }

        private PricebookTopUpPack RequirePack(string channel, int quantity)
        {
            var book = _pricebook.GetRequired(_pricebook.CurrentPricebookId);
            return book.TopUpPacks.Single(pack =>
                pack.Channel == channel && pack.Quantity == quantity
            );
        }

        private CreditTopUpPaySessionService CreateService(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant
        )
        {
            return new CreditTopUpPaySessionService(
                context,
                merchant,
                new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Frontend:BaseUrl"] = "https://app.test",
                        }
                    )
                    .Build(),
                new FixedTimeProvider(_now)
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
                FullName = "TopUp Owner",
                Role = "Owner",
                CreatedAt = _now,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "TopUp Venue",
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
            account.RevolutCustomerId = "cust_topup";
            account.RenewalDateUtc = _now.Date.AddMonths(1);
            context.BillingAccounts.Add(account);
            await context.SaveChangesAsync();
            return account;
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(
                        InMemoryEventId.TransactionIgnoredWarning
                    )
                )
                .Options;
            return new ApplicationDbContext(options);
        }

        private sealed class RecordingTopUpMerchant : IRevolutMerchantClient
        {
            public const string CheckoutUrl =
                "https://checkout.revolut.com/payment-link/fake-topup";

            public int CreateOrderCallCount { get; private set; }

            public RevolutCreateOrderRequest? LastCreateOrderRequest { get; private set; }

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
                LastCreateOrderRequest = request;
                return Task.FromResult(
                    new RevolutMerchantCreateResult(
                        Succeeded: true,
                        Id: $"ord_topup_{CreateOrderCallCount}",
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
                    new RevolutMerchantCreateResult(
                        Succeeded: true,
                        Id: subscriptionId
                    )
                );


public Task<RevolutMerchantCreateResult> ScheduleSubscriptionCancelAtCycleEndAsync(

                string subscriptionId,

                CancellationToken cancellationToken = default

            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
                string subscriptionId,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(
                        Succeeded: true,
                        Id: subscriptionId
                    )
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
    }
}
