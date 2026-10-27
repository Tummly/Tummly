using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Settles Shop QR slot reservations that stayed <c>open</c> past their
    /// reconcile deadline (spec 4.4.4). Revolut is the source of truth: a
    /// reservation is released only after Revolut confirms the payment was
    /// cancelled or failed, or when no Revolut order exists. A payment that can
    /// still complete keeps its slot. Never refunds.
    /// </summary>
    public sealed class ShopQrSlotReservationSweeperBackgroundService
        : BackgroundService
    {
        public static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// After this long without a final answer a reservation moves to
        /// <c>review</c> (still held).
        /// </summary>
        public static readonly TimeSpan ReviewAfter = TimeSpan.FromHours(24);

        private const int MaxErrorLength = 500;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeProvider _clock;
        private readonly ILogger<ShopQrSlotReservationSweeperBackgroundService> _logger;

        public ShopQrSlotReservationSweeperBackgroundService(
            IServiceScopeFactory scopeFactory,
            TimeProvider clock,
            ILogger<ShopQrSlotReservationSweeperBackgroundService> logger
        )
        {
            _scopeFactory = scopeFactory;
            _clock = clock;
            _logger = logger;
        }

        /// <summary>
        /// Wait after a non-final result: 5, 15, then 60 minutes (capped).
        /// </summary>
        public static TimeSpan BackoffAfterAttempt(int attempts) =>
            attempts switch
            {
                <= 1 => TimeSpan.FromMinutes(5),
                2 => TimeSpan.FromMinutes(15),
                _ => TimeSpan.FromMinutes(60),
            };

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SweepAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Shop QR slot reservation sweeper failed.");
                }

                await Task.Delay(SweepInterval, stoppingToken);
            }
        }

        public async Task SweepAsync(CancellationToken cancellationToken)
        {
            var now = _clock.GetUtcNow().UtcDateTime;

            List<Guid> orderIds;
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                orderIds = await context.ShopQrSlotReservations
                    .Where(row =>
                        row.Status == ShopQrSlotReservationStatuses.Open
                        && row.ReconcileAfterUtc <= now
                    )
                    .Select(row => row.ShopOrderId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }

            foreach (var orderId in orderIds)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    await ReconcileOrderAsync(
                        scope.ServiceProvider,
                        orderId,
                        now,
                        cancellationToken
                    );
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(
                        ex,
                        "Shop QR slot reconcile failed for order {ShopOrderId}",
                        orderId
                    );
                    await RecordFailureAsync(orderId, ex, now, cancellationToken);
                }
            }
        }

        private async Task ReconcileOrderAsync(
            IServiceProvider services,
            Guid orderId,
            DateTime now,
            CancellationToken ct
        )
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var due = await DueReservationsAsync(context, orderId, now, ct);
            if (due.Count == 0)
            {
                return;
            }

            var order = await context.ShopOrders
                .Include(row => row.Lines)
                .FirstOrDefaultAsync(row => row.Id == orderId, ct);
            if (
                order != null
                && string.Equals(
                    order.PaymentStatus,
                    ShopPaymentStatuses.Paid,
                    StringComparison.Ordinal
                )
            )
            {
                // Paid but still holding open rows: convert what can convert.
                await ConvertPaidOrderAsync(services, context, order, now, ct);
                return;
            }

            var intent = await context.RevolutOrderIntents
                .Where(row =>
                    row.ShopOrderId == orderId
                    && row.Purpose == RevolutOrderIntentPurposes.ShopMaterialsOrder
                    && row.IsOpen
                )
                .OrderByDescending(row => row.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);
            if (intent == null)
            {
                // Pay never opened a session, so no payment can exist.
                await ReleaseAsync(context, orderId, now, ct);
                return;
            }

            var merchant = services.GetRequiredService<IRevolutMerchantClient>();
            RevolutOrderRetrieveResult read;
            try
            {
                read = await merchant.GetOrderAsync(intent.OrderId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await BackOffAsync(context, due, DescribeException(ex), now, ct);
                return;
            }

            if (!read.Succeeded)
            {
                await BackOffAsync(
                    context,
                    due,
                    read.ErrorCode ?? "revolut_get_order_failed",
                    now,
                    ct
                );
                return;
            }

            var state = read.State?.Trim().ToLowerInvariant();
            switch (state)
            {
                case "completed":
                    await ApplyCompletedAsync(services, context, intent, read, now, ct);
                    return;

                case "pending":
                case "authorised":
                    await CancelPendingAsync(
                        services,
                        context,
                        merchant,
                        intent,
                        due,
                        now,
                        ct
                    );
                    return;

                case "cancelled":
                case "failed":
                    await FailAndReleaseAsync(context, intent, orderId, now, ct);
                    return;

                default:
                    await BackOffAsync(
                        context,
                        due,
                        $"revolut_state:{state ?? "unknown"}",
                        now,
                        ct
                    );
                    return;
            }
        }

        private async Task CancelPendingAsync(
            IServiceProvider services,
            ApplicationDbContext context,
            IRevolutMerchantClient merchant,
            RevolutOrderIntent intent,
            List<ShopQrSlotReservation> due,
            DateTime now,
            CancellationToken ct
        )
        {
            RevolutOrderRetrieveResult cancel;
            try
            {
                cancel = await merchant.CancelOrderAsync(intent.OrderId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await BackOffAsync(context, due, DescribeException(ex), now, ct);
                return;
            }

            if (cancel.Succeeded && IsCancelledState(cancel.State))
            {
                await FailAndReleaseAsync(context, intent, intent.ShopOrderId!.Value, now, ct);
                return;
            }

            // The cancel may have raced a payment or a retry; read Revolut again.
            RevolutOrderRetrieveResult reread;
            try
            {
                reread = await merchant.GetOrderAsync(intent.OrderId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await BackOffAsync(context, due, DescribeException(ex), now, ct);
                return;
            }

            if (reread.Succeeded && IsCancelledState(reread.State))
            {
                await FailAndReleaseAsync(context, intent, intent.ShopOrderId!.Value, now, ct);
                return;
            }

            if (
                reread.Succeeded
                && string.Equals(
                    reread.State?.Trim(),
                    "completed",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                await ApplyCompletedAsync(services, context, intent, reread, now, ct);
                return;
            }

            await BackOffAsync(
                context,
                due,
                $"cancel_failed:{cancel.ErrorCode ?? cancel.State ?? "unknown"}",
                now,
                ct
            );
        }

        private async Task ApplyCompletedAsync(
            IServiceProvider services,
            ApplicationDbContext context,
            RevolutOrderIntent intent,
            RevolutOrderRetrieveResult read,
            DateTime now,
            CancellationToken ct
        )
        {
            // Same path as ORDER_COMPLETED: marks the order Paid and converts the
            // reservations in one transaction. A later webhook finds the order
            // Paid and does nothing.
            await services.GetRequiredService<IRevolutOrderCompletedApplier>()
                .ApplyAsync(
                    new RevolutOrderCompletedApplyRequest(
                        OrderId: intent.OrderId,
                        OrderState: "completed",
                        BillingReason: read.BillingReason,
                        SubscriptionId: read.SubscriptionId
                            ?? intent.RevolutSubscriptionId,
                        RawWebhookBody: "{}",
                        RawOrderBody: read.RawBody ?? string.Empty,
                        PaymentMethodSummary: read.PaymentMethodSummary
                    ),
                    ct
                );

            if (intent.ShopOrderId is Guid shopOrderId)
            {
                await ReleaseLeftoversAsync(context, shopOrderId, now, ct);
                await TryQueuePrintMaterialsAsync(services, shopOrderId);
            }
        }

        private async Task ConvertPaidOrderAsync(
            IServiceProvider services,
            ApplicationDbContext context,
            ShopOrder order,
            DateTime now,
            CancellationToken ct
        )
        {
            var capacity = services.GetRequiredService<IActiveQrCapacity>();
            var slots = services.GetRequiredService<IShopQrSlotReservationService>();
            await using var transaction =
                await QrCapacityTransaction.BeginIfNeededAsync(context, ct);
            await capacity.LockBillingAccountAsync(order.RestaurantId, ct);
            await slots.ConvertForPaidOrderAsync(order, ct);
            await ReleaseLeftoversAsync(context, order.Id, now, ct);
            if (transaction != null)
            {
                await transaction.CommitAsync(ct);
            }
        }

        private static async Task ReleaseLeftoversAsync(
            ApplicationDbContext context,
            Guid shopOrderId,
            DateTime now,
            CancellationToken ct
        )
        {
            // A paid order's open rows that conversion did not take can never
            // convert; give their slot back.
            if (
                await ShopQrSlotReservationRelease.ReleaseOpenForOrderAsync(
                    context,
                    shopOrderId,
                    now,
                    ct
                ) > 0
            )
            {
                await context.SaveChangesAsync(ct);
            }
        }

        private static async Task FailAndReleaseAsync(
            ApplicationDbContext context,
            RevolutOrderIntent intent,
            Guid shopOrderId,
            DateTime now,
            CancellationToken ct
        )
        {
            // Marks the order payment_failed, closes the intent and releases.
            await ShopMaterialsOrderPaymentFailure.TryMarkFailedAsync(
                context,
                intent.OrderId,
                ct
            );
            await ReleaseAsync(context, shopOrderId, now, ct);
        }

        private static async Task ReleaseAsync(
            ApplicationDbContext context,
            Guid shopOrderId,
            DateTime now,
            CancellationToken ct
        )
        {
            if (
                await ShopQrSlotReservationRelease.ReleaseOpenForOrderAsync(
                    context,
                    shopOrderId,
                    now,
                    ct
                ) > 0
            )
            {
                await context.SaveChangesAsync(ct);
            }
        }

        private async Task BackOffAsync(
            ApplicationDbContext context,
            List<ShopQrSlotReservation> reservations,
            string error,
            DateTime now,
            CancellationToken ct
        )
        {
            var trimmed = error.Length > MaxErrorLength
                ? error[..MaxErrorLength]
                : error;

            foreach (var reservation in reservations)
            {
                reservation.ReconcileAttempts++;
                reservation.LastReconcileAtUtc = now;
                reservation.LastReconcileError = trimmed;
                reservation.UpdatedAtUtc = now;

                if (now >= reservation.CreatedAtUtc + ReviewAfter)
                {
                    reservation.Status = ShopQrSlotReservationStatuses.Review;
                    _logger.LogWarning(
                        "Shop QR slot reservation {ReservationId} for order {ShopOrderId} ({QrType} at location {LocationId}) moved to review after {Attempts} attempts; last error {LastError}",
                        reservation.Id,
                        reservation.ShopOrderId,
                        reservation.QrType,
                        reservation.RestaurantLocationId,
                        reservation.ReconcileAttempts,
                        trimmed
                    );
                }
                else
                {
                    reservation.ReconcileAfterUtc =
                        now + BackoffAfterAttempt(reservation.ReconcileAttempts);
                }
            }

            await context.SaveChangesAsync(ct);
        }

        private async Task RecordFailureAsync(
            Guid orderId,
            Exception ex,
            DateTime now,
            CancellationToken ct
        )
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var due = await DueReservationsAsync(context, orderId, now, ct);
                if (due.Count > 0)
                {
                    await BackOffAsync(context, due, DescribeException(ex), now, ct);
                }
            }
            catch (Exception inner) when (inner is not OperationCanceledException)
            {
                _logger.LogError(
                    inner,
                    "Could not record the reconcile failure for order {ShopOrderId}",
                    orderId
                );
            }
        }

        private static Task<List<ShopQrSlotReservation>> DueReservationsAsync(
            ApplicationDbContext context,
            Guid orderId,
            DateTime now,
            CancellationToken ct
        ) =>
            context.ShopQrSlotReservations
                .Where(row =>
                    row.ShopOrderId == orderId
                    && row.Status == ShopQrSlotReservationStatuses.Open
                    && row.ReconcileAfterUtc <= now
                )
                .ToListAsync(ct);

        private async Task TryQueuePrintMaterialsAsync(
            IServiceProvider services,
            Guid shopOrderId
        )
        {
            try
            {
                // Payment is committed. Print work must not undo it.
                await services.GetRequiredService<IPrintReadyQrMaterialsWork>()
                    .RequestShopOrderEnsureAsync(shopOrderId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Shop order {ShopOrderId} was committed Paid but its print-ready work could not be queued",
                    shopOrderId
                );
            }
        }

        private static bool IsCancelledState(string? state) =>
            string.Equals(state?.Trim(), "cancelled", StringComparison.OrdinalIgnoreCase)
            || string.Equals(state?.Trim(), "failed", StringComparison.OrdinalIgnoreCase);

        private static string DescribeException(Exception ex) =>
            $"{ex.GetType().Name}: {ex.Message}";
    }
}
