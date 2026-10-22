using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class ShopMaterialsOrderPaySessionServiceTests
    {
        private readonly DateTime _now = new(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task StartAsync_WhenVatModeOff_OmitsLineTaxes_AndChargesOrderGross()
        {
            await using var context = CreateContext();
            var seeded = await SeedAsync(context, vatPence: 0, grossPence: 1000);
            var merchant = new RecordingMerchant();
            var service = CreateService(
                context,
                merchant,
                new TummlySellerVatSettings { IsActive = false }
            );

            await service.StartAsync(
                seeded.Account,
                "Single",
                seeded.Order,
                "key-shop-off"
            );

            Assert.NotNull(merchant.LastCreateOrderRequest);
            Assert.Equal(1000, merchant.LastCreateOrderRequest!.AmountMinor);
            Assert.Empty(merchant.LastCreateOrderRequest.LineItems![0].Taxes);
            Assert.Equal(
                1000,
                merchant.LastCreateOrderRequest.LineItems[0].TotalAmount
            );
        }

        [Fact]
        public async Task StartAsync_UsesOrderStoredVat_WhenLiveModeFlippedOff()
        {
            await using var context = CreateContext();
            var vat = TummlyVatMath.VatPenceFromNetPence(
                1000,
                TummlyVatMath.DefaultVatRateBps
            );
            var seeded = await SeedAsync(
                context,
                vatPence: vat,
                grossPence: 1000 + vat
            );
            var merchant = new RecordingMerchant();
            var service = CreateService(
                context,
                merchant,
                new TummlySellerVatSettings { IsActive = false }
            );

            await service.StartAsync(
                seeded.Account,
                "Single",
                seeded.Order,
                "key-shop-place-active-pay-off"
            );

            Assert.NotNull(merchant.LastCreateOrderRequest);
            Assert.Equal(1000 + vat, merchant.LastCreateOrderRequest!.AmountMinor);
            Assert.Single(merchant.LastCreateOrderRequest.LineItems![0].Taxes);
            Assert.Equal(
                vat,
                merchant.LastCreateOrderRequest.LineItems[0].Taxes[0].Amount
            );
        }

        [Fact]
        public async Task StartAsync_WhenVatModeActive_Builds20PercentLineTaxes()
        {
            await using var context = CreateContext();
            var vat = TummlyVatMath.VatPenceFromNetPence(
                1000,
                TummlyVatMath.DefaultVatRateBps
            );
            var seeded = await SeedAsync(
                context,
                vatPence: vat,
                grossPence: 1000 + vat
            );
            var merchant = new RecordingMerchant();
            var service = CreateService(
                context,
                merchant,
                new TummlySellerVatSettings { IsActive = true }
            );

            await service.StartAsync(
                seeded.Account,
                "Single",
                seeded.Order,
                "key-shop-active"
            );

            Assert.NotNull(merchant.LastCreateOrderRequest);
            Assert.Equal(1000 + vat, merchant.LastCreateOrderRequest!.AmountMinor);
            Assert.Single(merchant.LastCreateOrderRequest.LineItems![0].Taxes);
            Assert.Equal(
                vat,
                merchant.LastCreateOrderRequest.LineItems[0].Taxes[0].Amount
            );
            Assert.Equal(
                "20.00",
                merchant.LastCreateOrderRequest.LineItems[0].Taxes[0].Percentage
            );
        }

        private ShopMaterialsOrderPaySessionService CreateService(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant,
            TummlySellerVatSettings sellerVat
        )
        {
            return new ShopMaterialsOrderPaySessionService(
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
                new FixedTimeProvider(_now),
                Options.Create(sellerVat)
            );
        }

        private async Task<(
            BillingAccount Account,
            ShopOrder Order
        )> SeedAsync(ApplicationDbContext context, int vatPence, int grossPence)
        {
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Shop Owner",
                Role = "Owner",
                CreatedAt = _now,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Shop Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = _now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                City = "London",
                Postcode = "SW1A1AA",
                LifecycleStatus = LocationLifecycleStatus.Active,
                CreatedAt = _now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var account = new BillingAccount
            {
                RestaurantId = restaurant.Id,
                SubscriptionPlan = BillingSubscriptionPlans.Starter,
                BillingStatus = BillingStatuses.Active,
                BillingCycle = BillingCycles.Monthly,
                RevolutCustomerId = "cust_shop",
                ContractedPricebookId = "TEST",
            };
            context.BillingAccounts.Add(account);

            var order = new ShopOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-1",
                RestaurantId = restaurant.Id,
                LocationId = location.Id,
                LocationNameSnapshot = "Main",
                PlacedByUserId = owner.Id,
                PlacedByNameSnapshot = "Shop Owner",
                MaterialsNetPence = 1000,
                VatPence = vatPence,
                DeliveryNetPence = 0,
                GrossPence = grossPence,
                DeliveryMethod = ShopDeliveryMethods.Standard,
                PaymentStatus = ShopPaymentStatuses.AwaitingPayment,
                ShipToContactName = "Alex",
                ShipToAddressLine1 = "1 High Street",
                ShipToPostcode = "SW1A 1AA",
                ShipToCountry = "United Kingdom",
                CreatedAtUtc = _now,
                UpdatedAtUtc = _now,
            };
            order.Lines.Add(
                new ShopOrderLine
                {
                    Id = Guid.NewGuid(),
                    ShopOrderId = order.Id,
                    CatalogSkuId = "sku-a",
                    TitleSnapshot = "Test SKU",
                    MaterialType = "print",
                    Quantity = 1,
                    UnitNetPence = 1000,
                    LineNetPence = 1000,
                }
            );
            context.ShopOrders.Add(order);
            await context.SaveChangesAsync();

            return (account, order);
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

        private sealed class RecordingMerchant : IRevolutMerchantClient
        {
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
                LastCreateOrderRequest = request;
                return Task.FromResult(
                    new RevolutMerchantCreateResult(
                        Succeeded: true,
                        Id: "ord_shop_1",
                        CheckoutUrl: "https://checkout.revolut.com/shop"
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
                        CheckoutUrl: "https://checkout.revolut.com/shop"
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

            public Task<RevolutMerchantCreateResult> RefundOrderAsync(
                string orderId,
                int? amountMinor,
                string idempotencyKey,
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
