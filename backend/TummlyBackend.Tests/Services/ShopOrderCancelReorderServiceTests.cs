using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Shop.MaterialsCatalog;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class ShopOrderCancelReorderServiceTests
    {
        [Fact]
        public async Task CancelAsync_PaidWithRevolutId_RefundsThenCancels()
        {
            await using var context = CreateContext();
            var seeded = await SeedAsync(context);
            var orderId = Guid.NewGuid();
            await InsertOrderAsync(
                context,
                seeded,
                orderId,
                isComplimentary: false,
                revolutOrderId: "  ord_shop_paid_1  ",
                productionStartedAtUtc: null
            );
            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var service = CreateService(context, merchant);

            var result = await service.CancelAsync(
                seeded.RestaurantId,
                seeded.UserId,
                orderId,
                seeded.LocationId,
                ShopCancelReasons.OrderedByMistake
            );

            Assert.Null(result.ErrorCode);
            Assert.NotNull(result.Order);
            Assert.Equal(ShopFulfilmentStatuses.Cancelled, result.Order!.FulfilmentStatus);
            Assert.Equal(ShopPaymentStatuses.Paid, result.Order.PaymentStatus);
            Assert.Equal(1, merchant.RefundOrderCallCount);
            Assert.Equal("ord_shop_paid_1", merchant.LastRefundOrderId);
            Assert.Null(merchant.LastRefundAmountMinor);
            Assert.Equal(
                $"shop-cancel:{orderId:D}",
                merchant.LastRefundIdempotencyKey
            );

            var stored = await context.ShopOrders
                .AsNoTracking()
                .SingleAsync(row => row.Id == orderId);
            Assert.Equal(ShopFulfilmentStatuses.Cancelled, stored.FulfilmentStatus);
            Assert.Equal(ShopPaymentStatuses.Paid, stored.PaymentStatus);
        }

        [Fact]
        public async Task CancelAsync_PaidRevolutFails_DoesNotCancel()
        {
            await using var context = CreateContext();
            var seeded = await SeedAsync(context);
            var orderId = Guid.NewGuid();
            await InsertOrderAsync(
                context,
                seeded,
                orderId,
                isComplimentary: false,
                revolutOrderId: "ord_shop_fail_1",
                productionStartedAtUtc: null
            );
            var merchant = new FakeFirstPaidRevolutMerchantClient
            {
                NextRefundFails = true,
            };
            var service = CreateService(context, merchant);

            var result = await service.CancelAsync(
                seeded.RestaurantId,
                seeded.UserId,
                orderId,
                seeded.LocationId,
                ShopCancelReasons.NoLongerRequired
            );

            Assert.Equal("revolut_refund_failed", result.ErrorCode);
            Assert.Null(result.Order);
            Assert.Equal(1, merchant.RefundOrderCallCount);

            var stored = await context.ShopOrders
                .AsNoTracking()
                .SingleAsync(row => row.Id == orderId);
            Assert.Equal(ShopFulfilmentStatuses.Processing, stored.FulfilmentStatus);
            Assert.Null(stored.CancelledAtUtc);
            Assert.Null(stored.CancelReason);
            Assert.Null(stored.CancelledByUserId);
        }

        [Fact]
        public async Task CancelAsync_Complimentary_CancelsWithoutRefundCall()
        {
            await using var context = CreateContext();
            var seeded = await SeedAsync(context);
            var orderId = Guid.NewGuid();
            await InsertOrderAsync(
                context,
                seeded,
                orderId,
                isComplimentary: true,
                revolutOrderId: null,
                productionStartedAtUtc: null
            );
            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var service = CreateService(context, merchant);

            var result = await service.CancelAsync(
                seeded.RestaurantId,
                seeded.UserId,
                orderId,
                seeded.LocationId,
                ShopCancelReasons.Other
            );

            Assert.Null(result.ErrorCode);
            Assert.NotNull(result.Order);
            Assert.Equal(ShopFulfilmentStatuses.Cancelled, result.Order!.FulfilmentStatus);
            Assert.Equal(0, merchant.RefundOrderCallCount);
        }

        [Fact]
        public async Task CancelAsync_PaidMissingRevolutId_FailsClosed()
        {
            await using var context = CreateContext();
            var seeded = await SeedAsync(context);
            var orderId = Guid.NewGuid();
            await InsertOrderAsync(
                context,
                seeded,
                orderId,
                isComplimentary: false,
                revolutOrderId: null,
                productionStartedAtUtc: null
            );
            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var service = CreateService(context, merchant);

            var result = await service.CancelAsync(
                seeded.RestaurantId,
                seeded.UserId,
                orderId,
                seeded.LocationId,
                ShopCancelReasons.OrderedByMistake
            );

            Assert.Equal("revolut_order_missing", result.ErrorCode);
            Assert.Null(result.Order);
            Assert.Equal(0, merchant.RefundOrderCallCount);

            var stored = await context.ShopOrders
                .AsNoTracking()
                .SingleAsync(row => row.Id == orderId);
            Assert.Equal(ShopFulfilmentStatuses.Processing, stored.FulfilmentStatus);
            Assert.Null(stored.CancelledAtUtc);
        }

        [Fact]
        public async Task CancelAsync_ProductionStampSet_BlocksWithoutRefundCall()
        {
            await using var context = CreateContext();
            var seeded = await SeedAsync(context);
            var orderId = Guid.NewGuid();
            await InsertOrderAsync(
                context,
                seeded,
                orderId,
                isComplimentary: false,
                revolutOrderId: "ord_shop_stamped_1",
                productionStartedAtUtc: DateTime.UtcNow.AddHours(-1)
            );
            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var service = CreateService(context, merchant);

            var result = await service.CancelAsync(
                seeded.RestaurantId,
                seeded.UserId,
                orderId,
                seeded.LocationId,
                ShopCancelReasons.OrderedByMistake
            );

            Assert.Equal("shop_order_not_cancellable", result.ErrorCode);
            Assert.Equal(0, merchant.RefundOrderCallCount);

            var stored = await context.ShopOrders
                .AsNoTracking()
                .SingleAsync(row => row.Id == orderId);
            Assert.Equal(ShopFulfilmentStatuses.Processing, stored.FulfilmentStatus);
            Assert.Null(stored.CancelledAtUtc);
        }

        private static ShopOrderCancelReorderService CreateService(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant
        )
        {
            return new ShopOrderCancelReorderService(
                context,
                new EmptyCatalog(),
                merchant
            );
        }

        private static async Task<(
            int RestaurantId,
            int UserId,
            int LocationId
        )> SeedAsync(ApplicationDbContext context)
        {
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Cancel Owner",
                Role = "Owner",
                CreatedAt = DateTime.UtcNow,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Cancel Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
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
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            return (restaurant.Id, owner.Id, location.Id);
        }

        private static async Task InsertOrderAsync(
            ApplicationDbContext context,
            (int RestaurantId, int UserId, int LocationId) seeded,
            Guid orderId,
            bool isComplimentary,
            string? revolutOrderId,
            DateTime? productionStartedAtUtc
        )
        {
            context.ShopOrders.Add(
                new ShopOrder
                {
                    Id = orderId,
                    OrderNumber = "ORD-1",
                    RestaurantId = seeded.RestaurantId,
                    LocationId = seeded.LocationId,
                    LocationNameSnapshot = "Main",
                    PlacedByUserId = seeded.UserId,
                    PlacedByNameSnapshot = "Cancel Owner",
                    MaterialsNetPence = isComplimentary ? 0 : 2400,
                    VatPence = isComplimentary ? 0 : 480,
                    DeliveryNetPence = 0,
                    GrossPence = isComplimentary ? 0 : 2880,
                    DeliveryMethod = ShopDeliveryMethods.Standard,
                    PaymentStatus = ShopPaymentStatuses.Paid,
                    RevolutOrderId = revolutOrderId,
                    PaidAtUtc = DateTime.UtcNow.AddDays(-1),
                    FulfilmentStatus = ShopFulfilmentStatuses.Processing,
                    ProcessingStartedAtUtc = DateTime.UtcNow.AddHours(-12),
                    ProductionStartedAtUtc = productionStartedAtUtc,
                    IsComplimentary = isComplimentary,
                    ShipToContactName = "Ada",
                    ShipToAddressLine1 = "1 High Street",
                    ShipToPostcode = "SE1 1TQ",
                    ShipToCountry = "United Kingdom",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
                    UpdatedAtUtc = DateTime.UtcNow,
                    Lines =
                    {
                        new ShopOrderLine
                        {
                            Id = Guid.NewGuid(),
                            CatalogSkuId = "table-tents",
                            TitleSnapshot = "Table tents",
                            MaterialType = "Table tents",
                            Quantity = 2,
                            UnitNetPence = isComplimentary ? 0 : 1200,
                            LineNetPence = isComplimentary ? 0 : 2400,
                        },
                    },
                }
            );
            await context.SaveChangesAsync();
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

        private sealed class EmptyCatalog : IMaterialsCatalog
        {
            public string CurrentCatalogId => "test-catalog";

            public MaterialsCatalogSnapshot GetRequired(string catalogId) =>
                throw new NotSupportedException();

            public IReadOnlyList<ShopCatalogListItemDto> BuildList() => [];

            public ShopCatalogDetailDto? TryBuildDetail(string skuId) => null;
        }
    }
}
