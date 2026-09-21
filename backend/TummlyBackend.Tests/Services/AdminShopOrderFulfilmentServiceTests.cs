using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Shop.MaterialsCatalog;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class AdminShopOrderFulfilmentServiceTests
    {
        private static readonly DateTimeOffset FixedUtc = new(
            2026,
            9,
            21,
            12,
            0,
            0,
            TimeSpan.Zero
        );

        [Fact]
        public async Task MarkProductionStarted_Unpaid_Fails()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var order = await db.ShopOrders.SingleAsync(row => row.Id == orderId);
            order.PaymentStatus = ShopPaymentStatuses.AwaitingPayment;
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.MarkProductionStartedAsync(
                orderId,
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.False(result.Succeeded);
            Assert.Equal("shop_order_not_in_production", result.ErrorCode);
            var stored = await db.ShopOrders
                .AsNoTracking()
                .SingleAsync(row => row.Id == orderId);
            Assert.Null(stored.ProductionStartedAtUtc);
        }

        [Fact]
        public async Task MarkProductionStarted_SetsStampAndAppendsAudit()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var service = CreateService(db);

            var result = await service.MarkProductionStartedAsync(
                orderId,
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Order);

            var stored = await db.ShopOrders
                .AsNoTracking()
                .SingleAsync(row => row.Id == orderId);
            Assert.Equal(FixedUtc.UtcDateTime, stored.ProductionStartedAtUtc);
            Assert.Equal(seeded.AdminId, stored.ProductionStartedByAdminUserId);
            Assert.Equal(FixedUtc.UtcDateTime, stored.UpdatedAtUtc);
            Assert.False(ShopOrderCancelRules.CanCancel(stored));
            Assert.Equal(
                "production_started",
                ShopOrderCancelRules.CancelBlockReason(stored)
            );

            var events = await db.AdminAuditEvents.ToListAsync();
            Assert.Single(events);
            var row = events[0];
            Assert.Equal(AdminAuditActions.ShopProductionStarted, row.Action);
            Assert.Equal(AdminAuditTargetTypes.ShopOrder, row.TargetType);
            Assert.Equal(orderId.ToString("D"), row.TargetId);
            Assert.Equal(seeded.RestaurantId, row.RestaurantId);
            Assert.Equal(seeded.AdminId, row.ActorAdminUserId);
            Assert.Equal(seeded.AdminEmail, row.ActorIdentity);
            Assert.Null(row.DetailJson);
        }

        [Fact]
        public async Task MarkProductionStarted_SecondCall_IsIdempotentWithoutSecondAudit()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var service = CreateService(db);

            var first = await service.MarkProductionStartedAsync(
                orderId,
                seeded.AdminId,
                seeded.AdminEmail
            );
            Assert.True(first.Succeeded);

            var second = await service.MarkProductionStartedAsync(
                orderId,
                seeded.AdminId + 99,
                "other@tummly.com"
            );

            Assert.True(second.Succeeded);
            Assert.Equal(1, await db.AdminAuditEvents.CountAsync());

            var stored = await db.ShopOrders
                .AsNoTracking()
                .SingleAsync(row => row.Id == orderId);
            Assert.Equal(FixedUtc.UtcDateTime, stored.ProductionStartedAtUtc);
            Assert.Equal(seeded.AdminId, stored.ProductionStartedByAdminUserId);
        }

        [Fact]
        public async Task MarkProductionStarted_ThenOperatorCancel_IsBlocked()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var fulfilment = CreateService(db);
            var mark = await fulfilment.MarkProductionStartedAsync(
                orderId,
                seeded.AdminId,
                seeded.AdminEmail
            );
            Assert.True(mark.Succeeded);

            var cancel = new ShopOrderCancelReorderService(
                db,
                new EmptyCatalog(),
                new FakeFirstPaidRevolutMerchantClient()
            );
            var result = await cancel.CancelAsync(
                seeded.RestaurantId,
                seeded.OwnerUserId,
                orderId,
                seeded.LocationId,
                ShopCancelReasons.OrderedByMistake
            );

            Assert.Equal("shop_order_not_cancellable", result.ErrorCode);
        }

        [Fact]
        public async Task MarkProductionStarted_OrderMissing_ReturnsNotFound()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var service = CreateService(db);

            var result = await service.MarkProductionStartedAsync(
                Guid.NewGuid(),
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.False(result.Succeeded);
            Assert.Equal("order_not_found", result.ErrorCode);
            Assert.Empty(await db.AdminAuditEvents.ToListAsync());
        }

        [Fact]
        public async Task ForceCancel_AfterProductionStamp_RefundsCancelsAndAudits()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var service = CreateService(db, merchant);

            var mark = await service.MarkProductionStartedAsync(
                orderId,
                seeded.AdminId,
                seeded.AdminEmail
            );
            Assert.True(mark.Succeeded);

            var result = await service.ForceCancelAsync(
                orderId,
                new AdminShopForceCancelRequest
                {
                    Reason = ShopCancelReasons.NoLongerRequired,
                    SkipRefund = false,
                },
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Order);
            Assert.Equal(
                ShopFulfilmentStatuses.Cancelled,
                result.Order!.FulfilmentStatus
            );

            Assert.Equal(1, merchant.RefundOrderCallCount);
            Assert.Equal("ord_mark_prod_1", merchant.LastRefundOrderId);
            Assert.Null(merchant.LastRefundAmountMinor);
            Assert.Equal(
                $"shop-cancel:{orderId:D}",
                merchant.LastRefundIdempotencyKey
            );

            var stored = await db.ShopOrders
                .AsNoTracking()
                .SingleAsync(row => row.Id == orderId);
            Assert.Equal(ShopFulfilmentStatuses.Cancelled, stored.FulfilmentStatus);
            Assert.Equal(ShopCancelReasons.NoLongerRequired, stored.CancelReason);
            Assert.Equal(FixedUtc.UtcDateTime, stored.CancelledAtUtc);
            Assert.Null(stored.CancelledByUserId);
            Assert.NotNull(stored.ProductionStartedAtUtc);

            var forceEvents = await db.AdminAuditEvents
                .Where(row => row.Action == AdminAuditActions.ShopForceCancel)
                .ToListAsync();
            Assert.Single(forceEvents);
            var audit = forceEvents[0];
            Assert.Equal(AdminAuditTargetTypes.ShopOrder, audit.TargetType);
            Assert.Equal(orderId.ToString("D"), audit.TargetId);
            Assert.Equal(seeded.RestaurantId, audit.RestaurantId);
            Assert.Equal(seeded.AdminId, audit.ActorAdminUserId);
            Assert.Equal(seeded.AdminEmail, audit.ActorIdentity);
            Assert.Contains("\"skipRefund\":false", audit.DetailJson);
            Assert.Contains("\"refundAttempted\":true", audit.DetailJson);
            Assert.Contains(
                $"\"reason\":\"{ShopCancelReasons.NoLongerRequired}\"",
                audit.DetailJson
            );
        }

        [Fact]
        public async Task ForceCancel_SkipRefund_CancelsWithoutMerchantCall()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var service = CreateService(db, merchant);

            await service.MarkProductionStartedAsync(
                orderId,
                seeded.AdminId,
                seeded.AdminEmail
            );

            var result = await service.ForceCancelAsync(
                orderId,
                new AdminShopForceCancelRequest
                {
                    Reason = "ops override free text",
                    SkipRefund = true,
                },
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.True(result.Succeeded);
            Assert.Equal(0, merchant.RefundOrderCallCount);

            var stored = await db.ShopOrders
                .AsNoTracking()
                .SingleAsync(row => row.Id == orderId);
            Assert.Equal(ShopFulfilmentStatuses.Cancelled, stored.FulfilmentStatus);
            Assert.Equal("ops override free text", stored.CancelReason);
            Assert.Null(stored.CancelledByUserId);

            var audit = Assert.Single(
                await db.AdminAuditEvents
                    .Where(row => row.Action == AdminAuditActions.ShopForceCancel)
                    .ToListAsync()
            );
            Assert.Contains("\"skipRefund\":true", audit.DetailJson);
            Assert.Contains("\"refundAttempted\":false", audit.DetailJson);
        }

        [Fact]
        public async Task ForceCancel_RevolutFails_LeavesFulfilmentUnchanged()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var merchant = new FakeFirstPaidRevolutMerchantClient
            {
                NextRefundFails = true,
            };
            var service = CreateService(db, merchant);

            await service.MarkProductionStartedAsync(
                orderId,
                seeded.AdminId,
                seeded.AdminEmail
            );

            var result = await service.ForceCancelAsync(
                orderId,
                new AdminShopForceCancelRequest
                {
                    Reason = ShopCancelReasons.Other,
                    SkipRefund = false,
                },
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.False(result.Succeeded);
            Assert.Equal("revolut_refund_failed", result.ErrorCode);
            Assert.Equal(1, merchant.RefundOrderCallCount);

            var stored = await db.ShopOrders
                .AsNoTracking()
                .SingleAsync(row => row.Id == orderId);
            Assert.Equal(ShopFulfilmentStatuses.Processing, stored.FulfilmentStatus);
            Assert.Null(stored.CancelledAtUtc);
            Assert.Null(stored.CancelReason);
            Assert.Null(stored.CancelledByUserId);
            Assert.NotNull(stored.ProductionStartedAtUtc);

            Assert.Empty(
                await db.AdminAuditEvents
                    .Where(row => row.Action == AdminAuditActions.ShopForceCancel)
                    .ToListAsync()
            );
        }

        [Fact]
        public async Task ForceCancel_AlreadyCancelled_IsIdempotentWithoutSecondRefund()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var service = CreateService(db, merchant);

            var first = await service.ForceCancelAsync(
                orderId,
                new AdminShopForceCancelRequest
                {
                    Reason = ShopCancelReasons.OrderedByMistake,
                    SkipRefund = false,
                },
                seeded.AdminId,
                seeded.AdminEmail
            );
            Assert.True(first.Succeeded);
            Assert.Equal(1, merchant.RefundOrderCallCount);

            var second = await service.ForceCancelAsync(
                orderId,
                new AdminShopForceCancelRequest
                {
                    Reason = ShopCancelReasons.Other,
                    SkipRefund = false,
                },
                seeded.AdminId + 1,
                "other@tummly.com"
            );

            Assert.True(second.Succeeded);
            Assert.Equal(1, merchant.RefundOrderCallCount);
            Assert.Equal(
                1,
                await db.AdminAuditEvents.CountAsync(row =>
                    row.Action == AdminAuditActions.ShopForceCancel
                )
            );

            var stored = await db.ShopOrders
                .AsNoTracking()
                .SingleAsync(row => row.Id == orderId);
            Assert.Equal(
                ShopCancelReasons.OrderedByMistake,
                stored.CancelReason
            );
        }

        [Fact]
        public async Task ForceCancel_InTransit_RejectsWithoutRefund()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var order = await db.ShopOrders.SingleAsync(row => row.Id == orderId);
            order.FulfilmentStatus = ShopFulfilmentStatuses.InTransit;
            await db.SaveChangesAsync();

            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var service = CreateService(db, merchant);

            var result = await service.ForceCancelAsync(
                orderId,
                new AdminShopForceCancelRequest
                {
                    Reason = ShopCancelReasons.Other,
                    SkipRefund = false,
                },
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.False(result.Succeeded);
            Assert.Equal("in_transit", result.ErrorCode);
            Assert.Equal(0, merchant.RefundOrderCallCount);
        }

        [Fact]
        public async Task ForceCancel_Delivered_RejectsWithoutRefund()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var order = await db.ShopOrders.SingleAsync(row => row.Id == orderId);
            order.FulfilmentStatus = ShopFulfilmentStatuses.Delivered;
            await db.SaveChangesAsync();

            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var service = CreateService(db, merchant);

            var result = await service.ForceCancelAsync(
                orderId,
                new AdminShopForceCancelRequest
                {
                    Reason = ShopCancelReasons.Other,
                    SkipRefund = false,
                },
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.False(result.Succeeded);
            Assert.Equal("delivered", result.ErrorCode);
            Assert.Equal(0, merchant.RefundOrderCallCount);
        }

        [Fact]
        public async Task ForceCancel_Complimentary_CancelsWithoutRefundCall()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var order = await db.ShopOrders.SingleAsync(row => row.Id == orderId);
            order.IsComplimentary = true;
            order.RevolutOrderId = null;
            await db.SaveChangesAsync();

            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var service = CreateService(db, merchant);

            var result = await service.ForceCancelAsync(
                orderId,
                new AdminShopForceCancelRequest
                {
                    Reason = ShopCancelReasons.Other,
                    SkipRefund = false,
                },
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.True(result.Succeeded);
            Assert.Equal(0, merchant.RefundOrderCallCount);

            var audit = Assert.Single(
                await db.AdminAuditEvents
                    .Where(row => row.Action == AdminAuditActions.ShopForceCancel)
                    .ToListAsync()
            );
            Assert.Contains("\"refundAttempted\":false", audit.DetailJson);
        }

        private static AdminShopOrderFulfilmentService CreateService(
            ApplicationDbContext db,
            IRevolutMerchantClient? merchant = null
        )
        {
            var audit = new AdminAuditService(db, new FixedTimeProvider(FixedUtc));
            return new AdminShopOrderFulfilmentService(
                db,
                new EmptyPrintReadyQrMaterials(),
                audit,
                new FixedTimeProvider(FixedUtc),
                merchant ?? new FakeFirstPaidRevolutMerchantClient()
            );
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            return new ApplicationDbContext(options);
        }

        private static async Task<Seeded> SeedAsync(ApplicationDbContext db)
        {
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Shop Owner",
                Role = "Owner",
                PhoneNumber = "07700900111",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            db.Users.Add(owner);
            await db.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Mark Production Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            db.Restaurants.Add(restaurant);
            await db.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                City = "London",
                Postcode = "SE1 1TQ",
                LifecycleStatus = LocationLifecycleStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            db.RestaurantLocations.Add(location);
            await db.SaveChangesAsync();

            var admin = new Admin
            {
                FullName = "Tummly Admin",
                Email = $"admin-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            db.Admins.Add(admin);
            await db.SaveChangesAsync();

            return new Seeded(
                restaurant.Id,
                location.Id,
                owner.Id,
                admin.Id,
                admin.Email
            );
        }

        private static async Task<Guid> InsertPaidProcessingOrderAsync(
            ApplicationDbContext db,
            Seeded seeded
        )
        {
            var orderId = Guid.NewGuid();
            db.ShopOrders.Add(
                new ShopOrder
                {
                    Id = orderId,
                    OrderNumber = "ORD-1",
                    RestaurantId = seeded.RestaurantId,
                    LocationId = seeded.LocationId,
                    LocationNameSnapshot = "Main",
                    PlacedByUserId = seeded.OwnerUserId,
                    PlacedByNameSnapshot = "Shop Owner",
                    MaterialsNetPence = 2400,
                    VatPence = 480,
                    DeliveryNetPence = 0,
                    GrossPence = 2880,
                    DeliveryMethod = ShopDeliveryMethods.Standard,
                    PaymentStatus = ShopPaymentStatuses.Paid,
                    RevolutOrderId = "ord_mark_prod_1",
                    PaidAtUtc = DateTime.UtcNow.AddDays(-1),
                    FulfilmentStatus = ShopFulfilmentStatuses.Processing,
                    ProcessingStartedAtUtc = DateTime.UtcNow.AddHours(-12),
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
                            UnitNetPence = 1200,
                            LineNetPence = 2400,
                        },
                    },
                }
            );
            await db.SaveChangesAsync();
            return orderId;
        }

        private sealed record Seeded(
            int RestaurantId,
            int LocationId,
            int OwnerUserId,
            int AdminId,
            string AdminEmail
        );

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;

            public FixedTimeProvider(DateTimeOffset utcNow)
            {
                _utcNow = utcNow;
            }

            public override DateTimeOffset GetUtcNow() => _utcNow;
        }

        private sealed class EmptyPrintReadyQrMaterials : IPrintReadyQrMaterialsService
        {
            public Task<bool> TryInvalidateAfterQrRotationAsync(
                int locationId,
                QrType qrType,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(false);

            public Task EnsureStarterMaterialsAsync(
                int locationId,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;

            public Task EnsureShopOrderMaterialsAsync(
                Guid shopOrderId,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;

            public Task EnsureAllStarterMaterialsForOperatorAsync(
                int operatorUserId,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;

            public Task<IReadOnlyList<PrintMaterialsLocationReadinessDto>>
                ListReadinessAsync(
                    int operatorUserId,
                    CancellationToken cancellationToken = default
                ) =>
                Task.FromResult<IReadOnlyList<PrintMaterialsLocationReadinessDto>>(
                    Array.Empty<PrintMaterialsLocationReadinessDto>()
                );

            public Task<IReadOnlyList<ShopPrintAssetReadinessDto>>
                ListShopOrderReadinessAsync(
                    Guid shopOrderId,
                    CancellationToken cancellationToken = default
                ) =>
                Task.FromResult<IReadOnlyList<ShopPrintAssetReadinessDto>>(
                    Array.Empty<ShopPrintAssetReadinessDto>()
                );

            public Task<PrintReadyQrDownload?> DownloadAsync(
                int operatorUserId,
                int locationId,
                QrType qrType,
                CancellationToken cancellationToken = default
            ) => Task.FromResult<PrintReadyQrDownload?>(null);

            public Task<PrintReadyQrDownload?> DownloadShopOrderAsync(
                Guid shopOrderId,
                QrType qrType,
                CancellationToken cancellationToken = default
            ) => Task.FromResult<PrintReadyQrDownload?>(null);

            public Task<PrintMaterialsAssetReadinessDto?> RetryAsync(
                int operatorUserId,
                int locationId,
                QrType qrType,
                CancellationToken cancellationToken = default
            ) => Task.FromResult<PrintMaterialsAssetReadinessDto?>(null);

            public Task<ShopPrintAssetReadinessDto?> RetryShopOrderAsync(
                Guid shopOrderId,
                QrType qrType,
                CancellationToken cancellationToken = default
            ) => Task.FromResult<ShopPrintAssetReadinessDto?>(null);
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
