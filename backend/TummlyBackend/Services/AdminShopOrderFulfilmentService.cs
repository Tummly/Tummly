using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class AdminShopOrderFulfilmentService
        : IAdminShopOrderFulfilmentService
    {
        private const int MaxPageSize = 100;
        private const int MaxTrackingUrlLength = 2048;
        private const int MaxOpsNotesLength = 2000;
        private const int MaxForceCancelReasonLength = 500;
        private const int MaxCarrierFieldLength = 200;
        private const int MaxCorrectionReasonLength = 500;

        private static readonly HashSet<string> ValidFulfilmentFilters =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ShopFulfilmentStatuses.Processing,
                ShopFulfilmentStatuses.InTransit,
                ShopFulfilmentStatuses.Delivered,
                ShopFulfilmentStatuses.Cancelled,
            };

        private static readonly string[] ExportHeaders =
        [
            "orderNumber",
            "locationNameSnapshot",
            "shipToContactName",
            "shipToContactPhone",
            "shipToAddressLine1",
            "shipToAddressLine2",
            "shipToPostcode",
            "shipToCountry",
            "deliveryInstructions",
            "deliveryMethod",
            "catalogSkuId",
            "titleSnapshot",
            "quantity",
            "unitNetPence",
            "lineNetPence",
            "fulfilmentStatus",
        ];

        private readonly ApplicationDbContext _context;
        private readonly IPrintReadyQrMaterialsService _printReadyQrMaterials;
        private readonly IAdminAuditService _audit;
        private readonly TimeProvider _clock;
        private readonly IRevolutMerchantClient _merchant;
        private readonly IShopOrderEmailNotifier _shopOrderEmail;

        public AdminShopOrderFulfilmentService(
            ApplicationDbContext context,
            IPrintReadyQrMaterialsService printReadyQrMaterials,
            IAdminAuditService audit,
            TimeProvider clock,
            IRevolutMerchantClient merchant,
            IShopOrderEmailNotifier? shopOrderEmail = null
        )
        {
            _context = context;
            _printReadyQrMaterials = printReadyQrMaterials;
            _audit = audit;
            _clock = clock;
            _merchant = merchant;
            _shopOrderEmail = shopOrderEmail ?? NoOpShopOrderEmailNotifier.Instance;
        }

        public async Task<AdminShopOrderListResponseDto> GetListAsync(
            AdminShopOrdersListQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ValidateListQuery(query);

            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
            var filtered = ApplyFilters(BaseAdminQuery(), query);

            var totalCount = await filtered.CountAsync(cancellationToken);
            var pageRows = await filtered
                .OrderByDescending(row => row.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(row => row.Lines)
                .ToListAsync(cancellationToken);

            var items = new List<AdminShopOrderListItemDto>(pageRows.Count);
            foreach (var order in pageRows)
            {
                items.Add(
                    await MapWithPrintAssetsAsync(order, cancellationToken)
                );
            }

            return new AdminShopOrderListResponseDto
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<AdminShopOrderFulfilmentResult> UpdateFulfilmentAsync(
            Guid orderId,
            AdminShopOrderFulfilmentPatchDto patch,
            CancellationToken cancellationToken = default
        )
        {
            var order = await FindOrderAsync(orderId, cancellationToken);
            if (order == null)
            {
                return OrderNotFound();
            }

            if (
                string.IsNullOrWhiteSpace(order.FulfilmentStatus)
                || order.PaymentStatus != ShopPaymentStatuses.Paid
                    && order.PaymentStatus != ShopPaymentStatuses.Refunded
            )
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "illegal_fulfilment_transition",
                    "Shop order is not eligible for admin fulfilment updates."
                );
            }

            // Status moves only through the preparation, production, dispatch
            // and delivery actions, which each write their own audit row.
            var currentStatus = order.FulfilmentStatus;
            if (
                !string.IsNullOrWhiteSpace(patch.FulfilmentStatus)
                && !string.Equals(
                    patch.FulfilmentStatus.Trim(),
                    currentStatus,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "illegal_fulfilment_transition",
                    "Use the preparation, production, dispatch or delivery action to change fulfilment status."
                );
            }

            if (patch.OpsNotesSet)
            {
                var notesError = ValidateOpsNotes(patch.OpsNotes);
                if (notesError != null)
                {
                    return notesError;
                }
            }

            if (patch.TrackingUrlSet)
            {
                if (currentStatus == ShopFulfilmentStatuses.Delivered)
                {
                    return AdminShopOrderFulfilmentResult.Fail(
                        "tracking_url_read_only",
                        "trackingUrl is read-only once the order is delivered."
                    );
                }

                if (currentStatus != ShopFulfilmentStatuses.InTransit)
                {
                    return AdminShopOrderFulfilmentResult.Fail(
                        "invalid_tracking_url",
                        "trackingUrl can only be changed once the order is dispatched."
                    );
                }

                var trackingError = ValidateTrackingUrl(patch.TrackingUrl);
                if (trackingError != null)
                {
                    return trackingError;
                }
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            if (patch.TrackingUrlSet)
            {
                order.TrackingUrl = NormalizeTrackingUrl(patch.TrackingUrl);
            }

            if (patch.OpsNotesSet)
            {
                order.OpsNotes = NormalizeOpsNotes(patch.OpsNotes);
            }

            order.UpdatedAtUtc = now;
            await _context.SaveChangesAsync(cancellationToken);

            return AdminShopOrderFulfilmentResult.Ok(
                await MapWithPrintAssetsAsync(order, cancellationToken)
            );
        }

        public async Task<AdminShopOrderFulfilmentResult> MarkPreparationStartedAsync(
            Guid orderId,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        )
        {
            var order = await FindOrderAsync(orderId, cancellationToken);
            if (order == null)
            {
                return OrderNotFound();
            }

            if (
                !IsPaidProcessing(order)
                || order.PreparationStartedAtUtc != null
                || order.ProductionStartedAtUtc != null
            )
            {
                return IllegalTransition(
                    "Preparation can only start once, for a paid order still in processing."
                );
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            order.PreparationStartedAtUtc = now;
            order.PreparationStartedByAdminUserId = actorAdminUserId;
            order.UpdatedAtUtc = now;

            _audit.Append(
                new AdminAuditAppendRequest(
                    AdminAuditActions.ShopPreparationStarted,
                    actorIdentity,
                    AdminAuditTargetTypes.ShopOrder,
                    orderId.ToString("D"),
                    ActorAdminUserId: actorAdminUserId,
                    RestaurantId: order.RestaurantId,
                    DetailJson: null
                )
            );

            await _context.SaveChangesAsync(cancellationToken);

            return AdminShopOrderFulfilmentResult.Ok(
                await MapWithPrintAssetsAsync(order, cancellationToken)
            );
        }

        public async Task<AdminShopOrderFulfilmentResult> MarkProductionStartedAsync(
            Guid orderId,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        )
        {
            var order = await FindOrderAsync(orderId, cancellationToken);
            if (order == null)
            {
                return OrderNotFound();
            }

            if (
                !IsPaidProcessing(order)
                || order.PreparationStartedAtUtc == null
                || order.ProductionStartedAtUtc != null
            )
            {
                return IllegalTransition(
                    "Production can only start once preparation has started, for a paid order still in processing."
                );
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            order.ProductionStartedAtUtc = now;
            order.ProductionStartedByAdminUserId = actorAdminUserId;
            order.ProductionStartSource = ShopFulfilmentEvidenceSources.AdminManual;
            order.UpdatedAtUtc = now;

            _audit.Append(
                new AdminAuditAppendRequest(
                    AdminAuditActions.ShopProductionStarted,
                    actorIdentity,
                    AdminAuditTargetTypes.ShopOrder,
                    orderId.ToString("D"),
                    ActorAdminUserId: actorAdminUserId,
                    RestaurantId: order.RestaurantId,
                    DetailJson: null
                )
            );

            await _context.SaveChangesAsync(cancellationToken);

            return AdminShopOrderFulfilmentResult.Ok(
                await MapWithPrintAssetsAsync(order, cancellationToken)
            );
        }

        public async Task<AdminShopOrderFulfilmentResult> MarkDispatchedAsync(
            Guid orderId,
            AdminShopDispatchRequest request,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        )
        {
            var order = await FindOrderAsync(orderId, cancellationToken);
            if (order == null)
            {
                return OrderNotFound();
            }

            // Carrier evidence is the dispatch event. A tracking URL on its
            // own (a generated label or link) does not mark the order dispatched.
            var carrierName = request.CarrierName?.Trim() ?? string.Empty;
            if (carrierName.Length == 0)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "carrier_name_required",
                    "A carrier or supplier name is required to mark the order as dispatched."
                );
            }

            if (carrierName.Length > MaxCarrierFieldLength)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_carrier_name",
                    $"carrierName must be at most {MaxCarrierFieldLength} characters."
                );
            }

            var carrierReference = request.CarrierReference?.Trim() ?? string.Empty;
            if (carrierReference.Length == 0)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "carrier_reference_required",
                    "A carrier or supplier reference is required to mark the order as dispatched."
                );
            }

            if (carrierReference.Length > MaxCarrierFieldLength)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_carrier_reference",
                    $"carrierReference must be at most {MaxCarrierFieldLength} characters."
                );
            }

            var trackingError = ValidateTrackingUrl(request.TrackingUrl);
            if (trackingError != null)
            {
                return trackingError;
            }

            if (!IsPaidProcessing(order) || order.ProductionStartedAtUtc == null)
            {
                return IllegalTransition(
                    "Dispatch can only be recorded once, after production has started, for a paid order in processing."
                );
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            order.FulfilmentStatus = ShopFulfilmentStatuses.InTransit;
            order.DispatchedAtUtc = now;
            order.DispatchedByAdminUserId = actorAdminUserId;
            order.DispatchSource = ShopFulfilmentEvidenceSources.AdminManual;
            order.DispatchCarrierName = carrierName;
            order.DispatchCarrierReference = carrierReference;
            order.TrackingUrl = NormalizeTrackingUrl(request.TrackingUrl);
            order.UpdatedAtUtc = now;

            _audit.Append(
                new AdminAuditAppendRequest(
                    AdminAuditActions.ShopDispatched,
                    actorIdentity,
                    AdminAuditTargetTypes.ShopOrder,
                    orderId.ToString("D"),
                    ActorAdminUserId: actorAdminUserId,
                    RestaurantId: order.RestaurantId,
                    DetailJson: System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            carrierName,
                            carrierReference,
                            trackingUrl = order.TrackingUrl,
                        }
                    )
                )
            );

            await _context.SaveChangesAsync(cancellationToken);
            await _shopOrderEmail.TryNotifyDispatchedAsync(
                order.Id,
                cancellationToken
            );

            return AdminShopOrderFulfilmentResult.Ok(
                await MapWithPrintAssetsAsync(order, cancellationToken)
            );
        }

        public async Task<AdminShopOrderFulfilmentResult> MarkDeliveredAsync(
            Guid orderId,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        )
        {
            var order = await FindOrderAsync(orderId, cancellationToken);
            if (order == null)
            {
                return OrderNotFound();
            }

            if (
                !string.Equals(
                    order.FulfilmentStatus,
                    ShopFulfilmentStatuses.InTransit,
                    StringComparison.Ordinal
                )
                || order.DispatchedAtUtc == null
                || order.DeliveredAtUtc != null
            )
            {
                return IllegalTransition(
                    "Delivery can only be recorded once, after dispatch."
                );
            }

            if (!ShopOrderDispatchEvidence.IsComplete(order))
            {
                return IllegalTransition(
                    "Record dispatch evidence before marking the order as delivered."
                );
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            order.FulfilmentStatus = ShopFulfilmentStatuses.Delivered;
            order.DeliveredAtUtc = now;
            order.DeliverySource = ShopFulfilmentEvidenceSources.AdminManual;
            order.UpdatedAtUtc = now;

            _audit.Append(
                new AdminAuditAppendRequest(
                    AdminAuditActions.ShopDelivered,
                    actorIdentity,
                    AdminAuditTargetTypes.ShopOrder,
                    orderId.ToString("D"),
                    ActorAdminUserId: actorAdminUserId,
                    RestaurantId: order.RestaurantId,
                    DetailJson: null
                )
            );

            await _context.SaveChangesAsync(cancellationToken);

            return AdminShopOrderFulfilmentResult.Ok(
                await MapWithPrintAssetsAsync(order, cancellationToken)
            );
        }

        /// <summary>
        /// Backfills carrier evidence for a legacy dispatch (a dispatch stamped
        /// with only a tracking URL). Admin supplies the carrier name, the
        /// reference and the actual dispatch time. The status and tracking URL
        /// stay as they are. The previous values go into the audit row.
        /// </summary>
        public async Task<AdminShopOrderFulfilmentResult> RecordDispatchEvidenceAsync(
            Guid orderId,
            AdminShopDispatchEvidenceRequest request,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        )
        {
            var order = await FindOrderAsync(orderId, cancellationToken);
            if (order == null)
            {
                return OrderNotFound();
            }

            if (!ShopOrderDispatchEvidence.IsMissing(order))
            {
                return IllegalTransition(
                    "Dispatch evidence can only be recorded for a dispatched or delivered order that has no evidence."
                );
            }

            var carrierName = request.CarrierName?.Trim() ?? string.Empty;
            if (carrierName.Length == 0)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "carrier_name_required",
                    "A carrier or supplier name is required to record dispatch evidence."
                );
            }

            if (carrierName.Length > MaxCarrierFieldLength)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_carrier_name",
                    $"carrierName must be at most {MaxCarrierFieldLength} characters."
                );
            }

            var carrierReference = request.CarrierReference?.Trim() ?? string.Empty;
            if (carrierReference.Length == 0)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "carrier_reference_required",
                    "A carrier or supplier reference is required to record dispatch evidence."
                );
            }

            if (carrierReference.Length > MaxCarrierFieldLength)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_carrier_reference",
                    $"carrierReference must be at most {MaxCarrierFieldLength} characters."
                );
            }

            if (request.DispatchedAtUtc == null)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "dispatched_at_required",
                    "The actual dispatch time is required to record dispatch evidence."
                );
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var dispatchedAt = request.DispatchedAtUtc.Value.UtcDateTime;
            if (
                dispatchedAt > now
                || dispatchedAt < order.CreatedAtUtc
                || (order.DeliveredAtUtc is DateTime deliveredAt && dispatchedAt > deliveredAt)
            )
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_dispatched_at",
                    "The dispatch time must be after the order was placed, before delivery, and not in the future."
                );
            }

            var previous = DispatchEvidenceSnapshot(order);
            order.DispatchedAtUtc = dispatchedAt;
            order.DispatchedByAdminUserId = actorAdminUserId;
            order.DispatchSource = ShopFulfilmentEvidenceSources.LegacyBackfill;
            order.DispatchCarrierName = carrierName;
            order.DispatchCarrierReference = carrierReference;
            order.UpdatedAtUtc = now;

            _audit.Append(
                new AdminAuditAppendRequest(
                    AdminAuditActions.ShopDispatchEvidenceRecorded,
                    actorIdentity,
                    AdminAuditTargetTypes.ShopOrder,
                    orderId.ToString("D"),
                    ActorAdminUserId: actorAdminUserId,
                    RestaurantId: order.RestaurantId,
                    DetailJson: System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            previous,
                            next = DispatchEvidenceSnapshot(order),
                        }
                    )
                )
            );

            await _context.SaveChangesAsync(cancellationToken);

            return AdminShopOrderFulfilmentResult.Ok(
                await MapWithPrintAssetsAsync(order, cancellationToken)
            );
        }

        /// <summary>
        /// Returns an order with unsupported dispatch or delivery evidence to its
        /// last substantiated state (processing; the label then reads from the
        /// preparation and production stamps). Clears the dispatch, delivery and
        /// tracking fields. The audit row keeps every original value and the reason.
        /// </summary>
        public async Task<AdminShopOrderFulfilmentResult> CorrectStatusAsync(
            Guid orderId,
            AdminShopStatusCorrectionRequest request,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        )
        {
            var order = await FindOrderAsync(orderId, cancellationToken);
            if (order == null)
            {
                return OrderNotFound();
            }

            if (!ShopOrderDispatchEvidence.IsMissing(order))
            {
                return IllegalTransition(
                    "Only a dispatched or delivered order with no dispatch evidence can be corrected."
                );
            }

            var reason = request.Reason?.Trim() ?? string.Empty;
            if (reason.Length == 0)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_correction_reason",
                    "A reason is required to correct the status."
                );
            }

            if (reason.Length > MaxCorrectionReasonLength)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_correction_reason",
                    $"reason must be at most {MaxCorrectionReasonLength} characters."
                );
            }

            var previous = new
            {
                fulfilmentStatus = order.FulfilmentStatus,
                dispatchedAtUtc = order.DispatchedAtUtc,
                dispatchedByAdminUserId = order.DispatchedByAdminUserId,
                dispatchSource = order.DispatchSource,
                dispatchCarrierName = order.DispatchCarrierName,
                dispatchCarrierReference = order.DispatchCarrierReference,
                deliveredAtUtc = order.DeliveredAtUtc,
                deliverySource = order.DeliverySource,
                trackingUrl = order.TrackingUrl,
            };

            var now = _clock.GetUtcNow().UtcDateTime;
            order.FulfilmentStatus = ShopFulfilmentStatuses.Processing;
            order.DispatchedAtUtc = null;
            order.DispatchedByAdminUserId = null;
            order.DispatchSource = null;
            order.DispatchCarrierName = null;
            order.DispatchCarrierReference = null;
            order.DeliveredAtUtc = null;
            order.DeliverySource = null;
            order.TrackingUrl = null;
            order.UpdatedAtUtc = now;

            _audit.Append(
                new AdminAuditAppendRequest(
                    AdminAuditActions.ShopStatusCorrected,
                    actorIdentity,
                    AdminAuditTargetTypes.ShopOrder,
                    orderId.ToString("D"),
                    ActorAdminUserId: actorAdminUserId,
                    RestaurantId: order.RestaurantId,
                    DetailJson: System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            reason,
                            previous,
                            next = new { fulfilmentStatus = order.FulfilmentStatus },
                        }
                    )
                )
            );

            await _context.SaveChangesAsync(cancellationToken);

            return AdminShopOrderFulfilmentResult.Ok(
                await MapWithPrintAssetsAsync(order, cancellationToken)
            );
        }

        public async Task<AdminShopOrderFulfilmentResult> ForceCancelAsync(
            Guid orderId,
            AdminShopForceCancelRequest request,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        )
        {
            var order = await _context.ShopOrders
                .Include(row => row.Lines)
                .FirstOrDefaultAsync(row => row.Id == orderId, cancellationToken);

            if (order == null)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "order_not_found",
                    "Shop order was not found."
                );
            }

            if (
                string.Equals(
                    order.FulfilmentStatus,
                    ShopFulfilmentStatuses.Cancelled,
                    StringComparison.Ordinal
                )
            )
            {
                return AdminShopOrderFulfilmentResult.Ok(
                    await MapWithPrintAssetsAsync(order, cancellationToken)
                );
            }

            var reason = request.Reason?.Trim() ?? string.Empty;
            if (reason.Length == 0)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_cancel_reason",
                    "reason must be a non-empty cancel reason (slug or free text)."
                );
            }

            if (reason.Length > MaxForceCancelReasonLength)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_cancel_reason",
                    $"reason must be at most {MaxForceCancelReasonLength} characters."
                );
            }

            if (
                string.Equals(
                    order.FulfilmentStatus,
                    ShopFulfilmentStatuses.InTransit,
                    StringComparison.Ordinal
                )
            )
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "in_transit",
                    "This shop order cannot be force-cancelled while in transit."
                );
            }

            if (
                string.Equals(
                    order.FulfilmentStatus,
                    ShopFulfilmentStatuses.Delivered,
                    StringComparison.Ordinal
                )
            )
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "delivered",
                    "This shop order cannot be force-cancelled after delivery."
                );
            }

            if (
                !string.Equals(
                    order.FulfilmentStatus,
                    ShopFulfilmentStatuses.Processing,
                    StringComparison.Ordinal
                )
            )
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "shop_order_not_cancellable",
                    "This shop order cannot be force-cancelled."
                );
            }

            var refundAttempted = false;
            if (!request.SkipRefund)
            {
                refundAttempted =
                    !order.IsComplimentary
                    && !string.IsNullOrWhiteSpace(order.RevolutOrderId);

                var refundError = await ShopOrderCancelRefund.TryFullRefundAsync(
                    _merchant,
                    order,
                    cancellationToken
                );
                if (refundError != null)
                {
                    return AdminShopOrderFulfilmentResult.Fail(
                        refundError,
                        "The Revolut refund failed. The shop order was not cancelled."
                    );
                }
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            order.FulfilmentStatus = ShopFulfilmentStatuses.Cancelled;
            order.CancelReason = reason;
            order.CancelledAtUtc = now;
            order.CancelledByUserId = null;
            order.UpdatedAtUtc = now;

            _audit.Append(
                new AdminAuditAppendRequest(
                    AdminAuditActions.ShopForceCancel,
                    actorIdentity,
                    AdminAuditTargetTypes.ShopOrder,
                    orderId.ToString("D"),
                    ActorAdminUserId: actorAdminUserId,
                    RestaurantId: order.RestaurantId,
                    DetailJson: System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            skipRefund = request.SkipRefund,
                            refundAttempted,
                            reason,
                        }
                    )
                )
            );

            await _context.SaveChangesAsync(cancellationToken);

            return AdminShopOrderFulfilmentResult.Ok(
                await MapWithPrintAssetsAsync(order, cancellationToken)
            );
        }

        public async Task<AdminShopOrdersExportResult> ExportCsvAsync(
            AdminShopOrdersListQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ValidateListQuery(query);

            var orders = await ApplyFilters(BaseAdminQuery(), query)
                .OrderByDescending(row => row.CreatedAtUtc)
                .Include(row => row.Lines)
                .ToListAsync(cancellationToken);

            var csvRows = new List<IReadOnlyList<string>>();
            foreach (var order in orders)
            {
                IEnumerable<ShopOrderLine?> lines =
                    order.Lines.Count == 0
                        ? new ShopOrderLine?[] { null }
                        : order.Lines
                            .OrderBy(line => line.CatalogSkuId)
                            .Cast<ShopOrderLine?>();

                foreach (var line in lines)
                {
                    csvRows.Add(
                        [
                            order.OrderNumber,
                            order.LocationNameSnapshot,
                            order.ShipToContactName,
                            order.ShipToContactPhone ?? string.Empty,
                            order.ShipToAddressLine1,
                            order.ShipToAddressLine2 ?? string.Empty,
                            order.ShipToPostcode,
                            order.ShipToCountry,
                            order.DeliveryInstructions ?? string.Empty,
                            order.DeliveryMethod,
                            line?.CatalogSkuId ?? string.Empty,
                            line?.TitleSnapshot ?? string.Empty,
                            line?.Quantity.ToString() ?? string.Empty,
                            line?.UnitNetPence.ToString() ?? string.Empty,
                            line?.LineNetPence.ToString() ?? string.Empty,
                            order.FulfilmentStatus ?? string.Empty,
                        ]
                    );
                }
            }

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            return new AdminShopOrdersExportResult
            {
                FileName = $"tummly-shop-orders-{stamp}Z.csv",
                ContentType = "text/csv",
                Content = Rfc4180Csv.WriteUtf8(ExportHeaders, csvRows),
            };
        }

        private IQueryable<ShopOrder> BaseAdminQuery()
        {
            return _context.ShopOrders
                .AsNoTracking()
                .Where(row =>
                    row.PaymentStatus == ShopPaymentStatuses.Paid
                    || row.PaymentStatus == ShopPaymentStatuses.Refunded
                );
        }

        private static IQueryable<ShopOrder> ApplyFilters(
            IQueryable<ShopOrder> query,
            AdminShopOrdersListQuery listQuery
        )
        {
            var fulfilmentFilters = listQuery.FulfilmentStatus
                .Where(status => !string.IsNullOrWhiteSpace(status))
                .Select(status => status.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            // No status chosen means every status.
            if (fulfilmentFilters.Count > 0)
            {
                query = query.Where(row =>
                    row.FulfilmentStatus != null
                    && fulfilmentFilters.Contains(row.FulfilmentStatus)
                );
            }

            if (listQuery.DispatchEvidenceMissing)
            {
                query = query.Where(ShopOrderDispatchEvidence.MissingExpression);
            }

            if (listQuery.RestaurantId is int restaurantId)
            {
                query = query.Where(row => row.RestaurantId == restaurantId);
            }

            var normalizedQuery = listQuery.Q?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(normalizedQuery))
            {
                var term = normalizedQuery.ToLowerInvariant();
                query = query.Where(row =>
                    row.OrderNumber.ToLower().Contains(term)
                    || row.LocationNameSnapshot.ToLower().Contains(term)
                    || row.PlacedByNameSnapshot.ToLower().Contains(term)
                    || row.Lines.Any(line =>
                        line.TitleSnapshot.ToLower().Contains(term)
                    )
                );
            }

            return query;
        }

        private static void ValidateListQuery(AdminShopOrdersListQuery query)
        {
            if (query.PageSize < 1 || query.PageSize > MaxPageSize)
            {
                throw new ArgumentException(
                    $"pageSize must be between 1 and {MaxPageSize}."
                );
            }

            if (
                query.FulfilmentStatus.Any(status =>
                    !string.IsNullOrWhiteSpace(status)
                    && !ValidFulfilmentFilters.Contains(status.Trim())
                )
            )
            {
                throw new ArgumentException("Invalid fulfilmentStatus filter.");
            }
        }

        private static AdminShopOrderFulfilmentResult? ValidateOpsNotes(
            string? opsNotes
        )
        {
            if (opsNotes == null)
            {
                return null;
            }

            if (opsNotes.Length > MaxOpsNotesLength)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_ops_notes",
                    $"opsNotes must be at most {MaxOpsNotesLength} characters."
                );
            }

            return null;
        }

        private static AdminShopOrderFulfilmentResult? ValidateTrackingUrl(
            string? trackingUrl
        )
        {
            var normalized = NormalizeTrackingUrl(trackingUrl);
            if (normalized == null)
            {
                return null;
            }

            if (normalized.Length > MaxTrackingUrlLength)
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_tracking_url",
                    $"trackingUrl must be at most {MaxTrackingUrlLength} characters."
                );
            }

            if (
                !Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
                || !string.Equals(
                    uri.Scheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return AdminShopOrderFulfilmentResult.Fail(
                    "invalid_tracking_url",
                    "trackingUrl must be an https URL."
                );
            }

            return null;
        }

        private static bool IsPaidProcessing(ShopOrder order)
        {
            return string.Equals(
                    order.PaymentStatus,
                    ShopPaymentStatuses.Paid,
                    StringComparison.Ordinal
                )
                && string.Equals(
                    order.FulfilmentStatus,
                    ShopFulfilmentStatuses.Processing,
                    StringComparison.Ordinal
                );
        }

        private async Task<ShopOrder?> FindOrderAsync(
            Guid orderId,
            CancellationToken cancellationToken
        )
        {
            return await _context.ShopOrders
                .Include(row => row.Lines)
                .FirstOrDefaultAsync(row => row.Id == orderId, cancellationToken);
        }

        private static AdminShopOrderFulfilmentResult OrderNotFound()
        {
            return AdminShopOrderFulfilmentResult.Fail(
                "order_not_found",
                "Shop order was not found."
            );
        }

        private static AdminShopOrderFulfilmentResult IllegalTransition(string message)
        {
            return AdminShopOrderFulfilmentResult.Fail(
                "illegal_fulfilment_transition",
                message
            );
        }

        private static string? NormalizeTrackingUrl(string? trackingUrl)
        {
            var trimmed = trackingUrl?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        private static string? NormalizeOpsNotes(string? opsNotes)
        {
            if (opsNotes == null)
            {
                return null;
            }

            var trimmed = opsNotes.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }

        private async Task<AdminShopOrderListItemDto> MapWithPrintAssetsAsync(
            ShopOrder order,
            CancellationToken cancellationToken
        )
        {
            var item = MapListItem(order);
            item.RestaurantName = await _context.Restaurants
                .AsNoTracking()
                .Where(row => row.Id == order.RestaurantId)
                .Select(row => row.Name)
                .FirstOrDefaultAsync(cancellationToken)
                ?? string.Empty;
            var printAssets =
                await _printReadyQrMaterials.ListShopOrderReadinessAsync(
                    order.Id,
                    cancellationToken
                );
            item.PrintAssets = printAssets
                .Select(row => new AdminShopPrintAssetDto
                {
                    QrType = row.QrType,
                    ProductTitle = row.ProductTitle,
                    Quantity = row.Quantity,
                    Status = row.Status,
                    FileName = row.FileName,
                    LastError = row.LastError,
                    Approved = row.Approved,
                    PrintRoute = row.PrintRoute,
                })
                .ToList();
            var reviewRows = await _context.ShopQrSlotReservations
                .AsNoTracking()
                .Where(row =>
                    row.ShopOrderId == order.Id
                    && row.Status == ShopQrSlotReservationStatuses.Review
                )
                .OrderBy(row => row.CreatedAtUtc)
                .ToListAsync(cancellationToken);
            item.ReviewQrReservations = reviewRows
                .Select(row => new AdminShopQrReservationReviewDto
                {
                    Status = row.Status,
                    QrType = row.QrType.ToString(),
                    LastReconcileError = row.LastReconcileError,
                    CreatedAtUtc = row.CreatedAtUtc,
                })
                .ToList();
            return item;
        }

        private static DateTime? AsUtc(DateTime? value)
        {
            return value == null
                ? null
                : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
        }

        /// <summary>Dispatch evidence fields, for the audit trail of a change.</summary>
        private static object DispatchEvidenceSnapshot(ShopOrder order)
        {
            return new
            {
                fulfilmentStatus = order.FulfilmentStatus,
                dispatchedAtUtc = order.DispatchedAtUtc,
                dispatchedByAdminUserId = order.DispatchedByAdminUserId,
                dispatchSource = order.DispatchSource,
                dispatchCarrierName = order.DispatchCarrierName,
                dispatchCarrierReference = order.DispatchCarrierReference,
            };
        }

        private static AdminShopOrderListItemDto MapListItem(ShopOrder order)
        {
            return new AdminShopOrderListItemDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                RestaurantId = order.RestaurantId,
                LocationId = order.LocationId,
                LocationNameSnapshot = order.LocationNameSnapshot,
                FulfilmentStatus = order.FulfilmentStatus ?? string.Empty,
                PaymentStatus = order.PaymentStatus,
                IsComplimentary = order.IsComplimentary,
                RevolutOrderId = order.RevolutOrderId,
                TrackingUrl = order.TrackingUrl,
                OpsNotes = order.OpsNotes,
                CreatedAtUtc = DateTime.SpecifyKind(
                    order.CreatedAtUtc,
                    DateTimeKind.Utc
                ),
                PaidAtUtc = order.PaidAtUtc,
                PreparationStartedAtUtc = AsUtc(order.PreparationStartedAtUtc),
                ProductionStartedAtUtc = AsUtc(order.ProductionStartedAtUtc),
                DispatchedAtUtc = AsUtc(order.DispatchedAtUtc),
                DispatchCarrierName = order.DispatchCarrierName,
                DispatchCarrierReference = order.DispatchCarrierReference,
                DispatchSource = order.DispatchSource,
                DispatchedByAdminUserId = order.DispatchedByAdminUserId,
                DeliveredAtUtc = AsUtc(order.DeliveredAtUtc),
                GrossPence = order.GrossPence,
                ReviewReason = order.ReviewReason,
                Lines = order.Lines
                    .OrderBy(line => line.CatalogSkuId, StringComparer.OrdinalIgnoreCase)
                    .Select(line => new AdminShopOrderLineSummaryDto
                    {
                        CatalogSkuId = line.CatalogSkuId,
                        TitleSnapshot = line.TitleSnapshot,
                        Quantity = line.Quantity,
                        UnitNetPence = line.UnitNetPence,
                        LineNetPence = line.LineNetPence,
                    })
                    .ToList(),
            };
        }
    }
}
