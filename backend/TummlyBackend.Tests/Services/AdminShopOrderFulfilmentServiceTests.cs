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
            Assert.Equal("illegal_fulfilment_transition", result.ErrorCode);
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
            Assert.Equal(
                ShopFulfilmentEvidenceSources.AdminManual,
                stored.ProductionStartSource
            );
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
        public async Task MarkProductionStarted_SecondCall_IsRejectedWithoutSecondAudit()
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

            Assert.False(second.Succeeded);
            Assert.Equal("illegal_fulfilment_transition", second.ErrorCode);
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
        public async Task GetList_ExposesReviewReasonAndHeldReviewReservations()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var order = await db.ShopOrders.SingleAsync(row => row.Id == orderId);
            order.ReviewReason =
                ShopOrderReviewReasons.QrCapacityUnavailableAfterPayment;
            var createdAt = new DateTime(2026, 9, 20, 9, 30, 0, DateTimeKind.Utc);
            db.ShopQrSlotReservations.AddRange(
                new ShopQrSlotReservation
                {
                    Id = Guid.NewGuid(),
                    ShopOrderId = orderId,
                    RestaurantLocationId = order.LocationId,
                    QrType = QrType.GuestCard,
                    Status = ShopQrSlotReservationStatuses.Review,
                    LastReconcileError = "revolut_state:processing",
                    ReconcileAfterUtc = createdAt.AddHours(25),
                    CreatedAtUtc = createdAt,
                    UpdatedAtUtc = createdAt,
                },
                // Only a held `review` row needs Admin attention.
                new ShopQrSlotReservation
                {
                    Id = Guid.NewGuid(),
                    ShopOrderId = orderId,
                    RestaurantLocationId = order.LocationId,
                    QrType = QrType.TableTalker,
                    Status = ShopQrSlotReservationStatuses.Open,
                    ReconcileAfterUtc = createdAt.AddMinutes(30),
                    CreatedAtUtc = createdAt,
                    UpdatedAtUtc = createdAt,
                }
            );
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var list = await service.GetListAsync(
                new AdminShopOrdersListQuery { RestaurantId = seeded.RestaurantId }
            );

            var item = Assert.Single(list.Items, row => row.Id == orderId);
            Assert.Equal(
                ShopOrderReviewReasons.QrCapacityUnavailableAfterPayment,
                item.ReviewReason
            );
            var held = Assert.Single(item.ReviewQrReservations);
            Assert.Equal(ShopQrSlotReservationStatuses.Review, held.Status);
            Assert.Equal("GuestCard", held.QrType);
            Assert.Equal("revolut_state:processing", held.LastReconcileError);
            Assert.Equal(createdAt, held.CreatedAtUtc);
        }

        [Fact]
        public async Task GetList_OrderWithoutReview_HasNoReviewFields()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var service = CreateService(db);

            var list = await service.GetListAsync(
                new AdminShopOrdersListQuery { RestaurantId = seeded.RestaurantId }
            );

            var item = Assert.Single(list.Items, row => row.Id == orderId);
            Assert.Null(item.ReviewReason);
            Assert.Empty(item.ReviewQrReservations);
        }

        [Fact]
        public async Task GetList_ExposesRestaurantNameAndCreatedAtUtc()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var createdAt = new DateTime(2026, 9, 19, 8, 15, 0, DateTimeKind.Utc);
            (await db.ShopOrders.SingleAsync(row => row.Id == orderId))
                .CreatedAtUtc = createdAt;
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var list = await service.GetListAsync(
                new AdminShopOrdersListQuery { RestaurantId = seeded.RestaurantId }
            );

            var item = Assert.Single(list.Items, row => row.Id == orderId);
            Assert.Equal("Mark Production Venue", item.RestaurantName);
            Assert.Equal(createdAt, item.CreatedAtUtc);
            Assert.Equal(DateTimeKind.Utc, item.CreatedAtUtc.Kind);
        }

        [Fact]
        public async Task GetList_NoStatusFilter_ReturnsAllFulfilmentStatuses()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var statuses = new[]
            {
                ShopFulfilmentStatuses.Processing,
                ShopFulfilmentStatuses.InTransit,
                ShopFulfilmentStatuses.Delivered,
                ShopFulfilmentStatuses.Cancelled,
            };
            foreach (var status in statuses)
            {
                var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
                (await db.ShopOrders.SingleAsync(row => row.Id == orderId))
                    .FulfilmentStatus = status;
                await db.SaveChangesAsync();
            }

            var service = CreateService(db);

            var list = await service.GetListAsync(
                new AdminShopOrdersListQuery { RestaurantId = seeded.RestaurantId }
            );

            Assert.Equal(4, list.TotalCount);
            Assert.Equal(
                statuses.OrderBy(status => status),
                list.Items.Select(row => row.FulfilmentStatus).OrderBy(status => status)
            );
        }

        [Fact]
        public async Task GetList_ExplicitStatusFilter_StillNarrows()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            await InsertPaidProcessingOrderAsync(db, seeded);
            var deliveredId = await InsertPaidProcessingOrderAsync(db, seeded);
            (await db.ShopOrders.SingleAsync(row => row.Id == deliveredId))
                .FulfilmentStatus = ShopFulfilmentStatuses.Delivered;
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var list = await service.GetListAsync(
                new AdminShopOrdersListQuery
                {
                    RestaurantId = seeded.RestaurantId,
                    FulfilmentStatus = [ShopFulfilmentStatuses.Delivered],
                }
            );

            var item = Assert.Single(list.Items);
            Assert.Equal(deliveredId, item.Id);
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
        public async Task MarkPreparationStarted_SetsStampAndAppendsAudit()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(
                db,
                seeded,
                preparationStarted: false
            );
            var service = CreateService(db);

            var result = await service.MarkPreparationStartedAsync(
                orderId,
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.True(result.Succeeded);
            var stored = await db.ShopOrders
                .AsNoTracking()
                .SingleAsync(row => row.Id == orderId);
            Assert.Equal(ShopFulfilmentStatuses.Processing, stored.FulfilmentStatus);
            Assert.Equal(FixedUtc.UtcDateTime, stored.PreparationStartedAtUtc);
            Assert.Equal(seeded.AdminId, stored.PreparationStartedByAdminUserId);
            Assert.Null(stored.ProductionStartedAtUtc);

            var row = Assert.Single(await db.AdminAuditEvents.ToListAsync());
            Assert.Equal(AdminAuditActions.ShopPreparationStarted, row.Action);
            Assert.Equal(AdminAuditTargetTypes.ShopOrder, row.TargetType);
            Assert.Equal(orderId.ToString("D"), row.TargetId);
            Assert.Equal(seeded.AdminId, row.ActorAdminUserId);
            Assert.Equal(seeded.AdminEmail, row.ActorIdentity);
        }

        [Fact]
        public async Task MarkPreparationStarted_Repeated_IsRejectedWithoutSecondAudit()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(
                db,
                seeded,
                preparationStarted: false
            );
            var service = CreateService(db);
            Assert.True(
                (await service.MarkPreparationStartedAsync(
                    orderId,
                    seeded.AdminId,
                    seeded.AdminEmail
                )).Succeeded
            );

            var second = await service.MarkPreparationStartedAsync(
                orderId,
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.False(second.Succeeded);
            Assert.Equal("illegal_fulfilment_transition", second.ErrorCode);
            Assert.Equal(1, await db.AdminAuditEvents.CountAsync());
        }

        [Fact]
        public async Task MarkProductionStarted_BeforePreparation_IsRejectedOutOfOrder()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(
                db,
                seeded,
                preparationStarted: false
            );
            var service = CreateService(db);

            var result = await service.MarkProductionStartedAsync(
                orderId,
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.False(result.Succeeded);
            Assert.Equal("illegal_fulfilment_transition", result.ErrorCode);
            var stored = await db.ShopOrders
                .AsNoTracking()
                .SingleAsync(row => row.Id == orderId);
            Assert.Null(stored.ProductionStartedAtUtc);
            Assert.Empty(await db.AdminAuditEvents.ToListAsync());
        }

        [Fact]
        public async Task MarkDispatched_BeforeProduction_IsRejectedOutOfOrder()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var service = CreateService(db);

            var result = await service.MarkDispatchedAsync(
                orderId,
                DispatchRequest(),
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.False(result.Succeeded);
            Assert.Equal("illegal_fulfilment_transition", result.ErrorCode);
            var stored = await db.ShopOrders
                .AsNoTracking()
                .SingleAsync(row => row.Id == orderId);
            Assert.Equal(ShopFulfilmentStatuses.Processing, stored.FulfilmentStatus);
            Assert.Null(stored.DispatchedAtUtc);
            Assert.Empty(await db.AdminAuditEvents.ToListAsync());
        }

        [Fact]
        public async Task MarkDispatched_MissingCarrierName_IsRejected()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertProductionStartedOrderAsync(db, seeded);
            var service = CreateService(db);

            var result = await service.MarkDispatchedAsync(
                orderId,
                DispatchRequest(carrierName: "   ", trackingUrl: "https://track.example/1"),
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.Equal("carrier_name_required", result.ErrorCode);
            Assert.Equal(ShopFulfilmentStatuses.Processing, (await Reload(db, orderId)).FulfilmentStatus);
        }

        [Fact]
        public async Task MarkDispatched_MissingCarrierReference_IsRejectedEvenWithTrackingUrl()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertProductionStartedOrderAsync(db, seeded);
            var service = CreateService(db);

            var result = await service.MarkDispatchedAsync(
                orderId,
                DispatchRequest(carrierReference: "  ", trackingUrl: "https://track.example/1"),
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.False(result.Succeeded);
            Assert.Equal("carrier_reference_required", result.ErrorCode);
            var stored = await Reload(db, orderId);
            Assert.Equal(ShopFulfilmentStatuses.Processing, stored.FulfilmentStatus);
            Assert.Null(stored.DispatchedAtUtc);
            Assert.Null(stored.TrackingUrl);
            Assert.Empty(await db.AdminAuditEvents.ToListAsync());
        }

        [Fact]
        public async Task MarkDispatched_RejectsNonHttpsTrackingUrl()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertProductionStartedOrderAsync(db, seeded);
            var service = CreateService(db);

            var result = await service.MarkDispatchedAsync(
                orderId,
                DispatchRequest(trackingUrl: "http://track.example/insecure"),
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.Equal("invalid_tracking_url", result.ErrorCode);
            Assert.Equal(ShopFulfilmentStatuses.Processing, (await Reload(db, orderId)).FulfilmentStatus);
        }

        [Fact]
        public async Task MarkDispatched_WithCarrierEvidence_SetsEvidenceAndAppendsAudit()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertProductionStartedOrderAsync(db, seeded);
            var service = CreateService(db);

            var result = await service.MarkDispatchedAsync(
                orderId,
                DispatchRequest(trackingUrl: "https://track.example/1"),
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.True(result.Succeeded);
            var stored = await Reload(db, orderId);
            Assert.Equal(ShopFulfilmentStatuses.InTransit, stored.FulfilmentStatus);
            Assert.Equal(FixedUtc.UtcDateTime, stored.DispatchedAtUtc);
            Assert.Equal(seeded.AdminId, stored.DispatchedByAdminUserId);
            Assert.Equal(ShopFulfilmentEvidenceSources.AdminManual, stored.DispatchSource);
            Assert.Equal("DPD", stored.DispatchCarrierName);
            Assert.Equal("DPD-123", stored.DispatchCarrierReference);
            Assert.Equal("https://track.example/1", stored.TrackingUrl);
            Assert.Null(stored.DeliveredAtUtc);

            var row = Assert.Single(await db.AdminAuditEvents.ToListAsync());
            Assert.Equal(AdminAuditActions.ShopDispatched, row.Action);
            Assert.Equal(orderId.ToString("D"), row.TargetId);
            Assert.Equal(seeded.AdminId, row.ActorAdminUserId);
            Assert.Contains("DPD-123", row.DetailJson);
        }

        [Fact]
        public async Task MarkDispatched_WithoutTrackingUrl_SucceedsWithCarrierEvidence()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertProductionStartedOrderAsync(db, seeded);
            var service = CreateService(db);

            var result = await service.MarkDispatchedAsync(
                orderId,
                DispatchRequest(),
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.True(result.Succeeded);
            var stored = await Reload(db, orderId);
            Assert.Equal(ShopFulfilmentStatuses.InTransit, stored.FulfilmentStatus);
            Assert.Null(stored.TrackingUrl);
            Assert.Equal("DPD-123", stored.DispatchCarrierReference);
        }

        [Fact]
        public async Task MarkDispatched_Repeated_IsRejectedWithoutSecondAudit()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertProductionStartedOrderAsync(db, seeded);
            var service = CreateService(db);
            Assert.True(
                (await service.MarkDispatchedAsync(
                    orderId,
                    DispatchRequest(),
                    seeded.AdminId,
                    seeded.AdminEmail
                )).Succeeded
            );

            var second = await service.MarkDispatchedAsync(
                orderId,
                DispatchRequest(carrierReference: "DPD-999"),
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.Equal("illegal_fulfilment_transition", second.ErrorCode);
            var stored = await Reload(db, orderId);
            Assert.Equal("DPD-123", stored.DispatchCarrierReference);
            Assert.Equal(1, await db.AdminAuditEvents.CountAsync());
        }

        [Fact]
        public async Task MarkDelivered_BeforeDispatch_IsRejectedOutOfOrder()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertProductionStartedOrderAsync(db, seeded);
            var service = CreateService(db);

            var result = await service.MarkDeliveredAsync(
                orderId,
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.Equal("illegal_fulfilment_transition", result.ErrorCode);
            var stored = await Reload(db, orderId);
            Assert.Equal(ShopFulfilmentStatuses.Processing, stored.FulfilmentStatus);
            Assert.Null(stored.DeliveredAtUtc);
            Assert.Empty(await db.AdminAuditEvents.ToListAsync());
        }

        [Fact]
        public async Task MarkDelivered_AfterDispatch_RecordsSourceAndAppendsAudit()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertProductionStartedOrderAsync(db, seeded);
            var service = CreateService(db);
            Assert.True(
                (await service.MarkDispatchedAsync(
                    orderId,
                    DispatchRequest(),
                    seeded.AdminId,
                    seeded.AdminEmail
                )).Succeeded
            );

            var result = await service.MarkDeliveredAsync(
                orderId,
                seeded.AdminId,
                seeded.AdminEmail
            );

            Assert.True(result.Succeeded);
            var stored = await Reload(db, orderId);
            Assert.Equal(ShopFulfilmentStatuses.Delivered, stored.FulfilmentStatus);
            Assert.Equal(FixedUtc.UtcDateTime, stored.DeliveredAtUtc);
            Assert.Equal(ShopFulfilmentEvidenceSources.AdminManual, stored.DeliverySource);
            Assert.Equal(
                new[]
                {
                    AdminAuditActions.ShopDelivered,
                    AdminAuditActions.ShopDispatched,
                },
                // Audit ids are GUIDs, so sort by action for a stable order.
                (await db.AdminAuditEvents
                    .OrderBy(row => row.Action)
                    .ToListAsync())
                    .Select(row => row.Action)
            );
        }

        [Fact]
        public async Task MarkDelivered_Repeated_IsRejected()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertProductionStartedOrderAsync(db, seeded);
            var service = CreateService(db);
            await service.MarkDispatchedAsync(
                orderId,
                DispatchRequest(),
                seeded.AdminId,
                seeded.AdminEmail
            );
            Assert.True(
                (await service.MarkDeliveredAsync(orderId, seeded.AdminId, seeded.AdminEmail)).Succeeded
            );

            var second = await service.MarkDeliveredAsync(orderId, seeded.AdminId, seeded.AdminEmail);

            Assert.Equal("illegal_fulfilment_transition", second.ErrorCode);
            Assert.Equal(2, await db.AdminAuditEvents.CountAsync());
        }

        [Fact]
        public async Task UpdateFulfilment_CannotChangeStatus_UseTransitionActions()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var service = CreateService(db);

            var patch = new AdminShopOrderFulfilmentPatchDto
            {
                FulfilmentStatus = ShopFulfilmentStatuses.InTransit,
            };
            var result = await service.UpdateFulfilmentAsync(orderId, patch);

            Assert.Equal("illegal_fulfilment_transition", result.ErrorCode);
            var stored = await Reload(db, orderId);
            Assert.Equal(ShopFulfilmentStatuses.Processing, stored.FulfilmentStatus);
            Assert.Null(stored.DispatchedAtUtc);
        }

        [Fact]
        public async Task UpdateFulfilment_TrackingUrlBeforeDispatch_IsRejected()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var service = CreateService(db);

            var patch = new AdminShopOrderFulfilmentPatchDto { TrackingUrl = "https://track.example/1" };
            var result = await service.UpdateFulfilmentAsync(orderId, patch);

            Assert.Equal("invalid_tracking_url", result.ErrorCode);
            Assert.Null((await Reload(db, orderId)).TrackingUrl);
        }

        private static AdminShopDispatchRequest DispatchRequest(
            string carrierName = "DPD",
            string carrierReference = "DPD-123",
            string? trackingUrl = null
        ) =>
            new()
            {
                CarrierName = carrierName,
                CarrierReference = carrierReference,
                TrackingUrl = trackingUrl,
            };

        private static async Task<ShopOrder> Reload(ApplicationDbContext db, Guid orderId)
        {
            return await db.ShopOrders
                .AsNoTracking()
                .SingleAsync(row => row.Id == orderId);
        }

        /// <summary>
        /// Paid order with preparation and production already recorded, set
        /// directly so the audit table only holds the rows a test writes.
        /// </summary>
        private static async Task<Guid> InsertProductionStartedOrderAsync(
            ApplicationDbContext db,
            Seeded seeded
        )
        {
            var orderId = await InsertPaidProcessingOrderAsync(db, seeded);
            var order = await db.ShopOrders.SingleAsync(row => row.Id == orderId);
            order.ProductionStartedAtUtc = DateTime.UtcNow.AddHours(-5);
            order.ProductionStartedByAdminUserId = seeded.AdminId;
            order.ProductionStartSource = ShopFulfilmentEvidenceSources.AdminManual;
            await db.SaveChangesAsync();
            return orderId;
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
            Seeded seeded,
            bool preparationStarted = true
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
                    PreparationStartedAtUtc = preparationStarted
                        ? DateTime.UtcNow.AddHours(-10)
                        : null,
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

            public Task<ShopPrintAssetPreviewDto?> GetShopOrderPreviewAsync(
                Guid shopOrderId,
                QrType qrType,
                CancellationToken cancellationToken = default
            ) => Task.FromResult<ShopPrintAssetPreviewDto?>(null);

            public Task<ShopPrintAssetReadinessDto?> ApproveShopOrderAsync(
                Guid shopOrderId,
                QrType qrType,
                int actorAdminUserId,
                string actorIdentity,
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
