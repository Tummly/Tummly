using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class CustomPlanCycleBillingServiceTests
    {
        private readonly IPricebookCatalog _pricebook = TestPricebookPaths.LoadV3();
        private readonly DateTime _now = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task StartCyclePay_WhenVatModeOff_ChargesNetOnly_AndOmitsLineTax()
        {
            await using var context = CreateContext();
            var account = await SeedCustomAccountAsync(context, periodNetPence: 10_000);
            var merchant = new RecordingCustomCycleMerchant();
            var service = CreateService(
                context,
                merchant,
                new TummlySellerVatSettings { IsActive = false }
            );

            var (checkout, error) = await service.StartCyclePayAsync(
                account.RestaurantId,
                "https://app.test/redirect"
            );

            Assert.Null(error);
            Assert.Equal(RecordingCustomCycleMerchant.CheckoutUrl, checkout);
            Assert.NotNull(merchant.LastCreateOrderRequest);
            Assert.Equal(10_000, merchant.LastCreateOrderRequest!.AmountMinor);
            Assert.Empty(merchant.LastCreateOrderRequest.LineItems![0].Taxes);

            var intent = await context.RevolutOrderIntents.SingleAsync();
            Assert.Equal(10_000, intent.NetAmountMinor);
            Assert.Equal(0, intent.VatAmountMinor);
            Assert.Equal(10_000, intent.GrossAmountMinor);
            Assert.Equal(RevolutOrderIntentPurposes.CustomPlanCycle, intent.Purpose);
        }

        [Fact]
        public async Task StartCyclePay_WhenVatModeActive_ChargesGrossWith20PercentVat()
        {
            await using var context = CreateContext();
            var account = await SeedCustomAccountAsync(context, periodNetPence: 10_000);
            var merchant = new RecordingCustomCycleMerchant();
            var service = CreateService(
                context,
                merchant,
                new TummlySellerVatSettings { IsActive = true }
            );
            var expectedVat = TummlyVatMath.VatPenceFromNetPence(
                10_000,
                TummlyVatMath.DefaultVatRateBps
            );
            var expectedGross = 10_000 + expectedVat;

            var (checkout, error) = await service.StartCyclePayAsync(
                account.RestaurantId,
                "https://app.test/redirect",
                "custom-cycle-key-1"
            );

            Assert.Null(error);
            Assert.Equal(RecordingCustomCycleMerchant.CheckoutUrl, checkout);
            Assert.Equal(expectedGross, merchant.LastCreateOrderRequest!.AmountMinor);
            Assert.Single(merchant.LastCreateOrderRequest.LineItems![0].Taxes);
            Assert.Equal(
                expectedVat,
                merchant.LastCreateOrderRequest.LineItems[0].Taxes[0].Amount
            );

            var intent = await context.RevolutOrderIntents.SingleAsync();
            Assert.Equal(10_000, intent.NetAmountMinor);
            Assert.Equal(expectedVat, intent.VatAmountMinor);
            Assert.Equal(expectedGross, intent.GrossAmountMinor);
            Assert.Equal("custom-cycle-key-1", intent.IdempotencyKey);
        }

        [Fact]
        public async Task StartCyclePay_ReusesOpenIntent_WhenSameIdempotencyKey()
        {
            await using var context = CreateContext();
            var account = await SeedCustomAccountAsync(context, periodNetPence: 5_000);
            var merchant = new RecordingCustomCycleMerchant();
            var service = CreateService(context, merchant);

            await service.StartCyclePayAsync(
                account.RestaurantId,
                "https://app.test/redirect",
                "same-key"
            );
            var (second, error) = await service.StartCyclePayAsync(
                account.RestaurantId,
                "https://app.test/redirect",
                "same-key"
            );

            Assert.Null(error);
            Assert.Equal(RecordingCustomCycleMerchant.CheckoutUrl, second);
            Assert.Equal(1, merchant.CreateOrderCallCount);
            Assert.Equal(1, await context.RevolutOrderIntents.CountAsync());
        }

        private CustomPlanCycleBillingService CreateService(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant,
            TummlySellerVatSettings? sellerVat = null
        )
        {
            return new CustomPlanCycleBillingService(
                context,
                merchant,
                new FixedTimeProvider(_now),
                Options.Create(sellerVat ?? new TummlySellerVatSettings { IsActive = false })
            );
        }

        private async Task<BillingAccount> SeedCustomAccountAsync(
            ApplicationDbContext context,
            int periodNetPence
        )
        {
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Custom Owner",
                Role = "Owner",
                CreatedAt = _now,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Custom Venue",
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
                CatalogPublicId = "cat_test",
                Cadence = "monthly",
                PeriodNetPence = periodNetPence,
                MonthlyNetPence = periodNetPence,
                Quantities = new CustomPlanQuantities
                {
                    Locations = 2,
                    TeamUsers = 4,
                    QrPlacements = 10,
                    Offers = 2,
                    EmailCredits = 1000,
                    SmsCredits = 50,
                    AiCredits = 50,
                },
            };

            var account = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                _pricebook.CurrentPricebookId
            );
            account.SubscriptionPlan = BillingSubscriptionPlans.Custom;
            account.BillingStatus = BillingStatuses.Active;
            account.BillingCycle = BillingCycles.Monthly;
            account.RevolutCustomerId = "cust_custom";
            account.CustomPlanConfigJson = config.ToJson();
            account.RenewalDateUtc = _now.Date.AddDays(30);
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

        private sealed class RecordingCustomCycleMerchant : IRevolutMerchantClient
        {
            public const string CheckoutUrl =
                "https://checkout.revolut.com/payment-link/fake-custom";

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
                        Id: $"ord_custom_{CreateOrderCallCount}",
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
    }
}
