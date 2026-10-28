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

        /// <summary>
        /// Stable name for alerting: a held reservation moved to <c>review</c>
        /// and Admin must look at the order (spec 4.4.5).
        /// </summary>
        public static readonly EventId ReservationReviewEvent =
            new(5201, "ShopQrReservationReview");

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

        private enum AttemptOutcome
        {
            /// <summary>Revolut confirmed this attempt cancelled or failed.</summary>
            Confirmed,

            /// <summary>The attempt may still complete or could not be read.</summary>
            NotConfirmed,

            /// <summary>The attempt completed and the paid path ran.</summary>
            Completed,
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
            if (order == null)
            {
                return;
            }

            if (
                string.Equals(
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

            // Every Revolut attempt for the order, open or closed: a declined or
            // authorised event closes an intent while its Revolut order can still
            // be paid, and the /pay reuse path leaves older attempts behind.
            var intents = await context.RevolutOrderIntents
                .Where(row =>
                    row.ShopOrderId == orderId
                    && row.Purpose == RevolutOrderIntentPurposes.ShopMaterialsOrder
                )
                .OrderByDescending(row => row.CreatedAtUtc)
                .ToListAsync(ct);
            if (intents.Count == 0)
            {
                // Pay never opened a session, so no payment can exist. Under the
                // lock the release still checks for a pay that began meanwhile.
                await ReleaseConfirmedAsync(services, context, order, due, intents, now, ct);
                return;
            }

            var merchant = services.GetRequiredService<IRevolutMerchantClient>();
            var errors = new List<string>();
            foreach (var intent in intents)
            {
                var (outcome, error) = await ReconcileIntentAsync(
                    services,
                    context,
                    merchant,
                    order,
                    intent,
                    now,
                    ct
                );
                if (outcome == AttemptOutcome.Completed)
                {
                    return;
                }

                if (outcome == AttemptOutcome.NotConfirmed)
                {
                    errors.Add(error ?? "revolut_state:unknown");
                }
            }

            if (errors.Count == 0)
            {
                // Every attempt is confirmed cancelled or failed: no payment can
                // still complete, so the slot goes back.
                await ReleaseConfirmedAsync(services, context, order, due, intents, now, ct);
                return;
            }

            await BackOffAsync(
                services,
                context,
                order.RestaurantId,
                due,
                string.Join("; ", errors.Distinct()),
                now,
                ct
            );
        }

        private async Task<(AttemptOutcome Outcome, string? Error)> ReconcileIntentAsync(
            IServiceProvider services,
            ApplicationDbContext context,
            IRevolutMerchantClient merchant,
            ShopOrder order,
            RevolutOrderIntent intent,
            DateTime now,
            CancellationToken ct
        )
        {
            RevolutOrderRetrieveResult read;
            try
            {
                read = await merchant.GetOrderAsync(intent.OrderId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return (AttemptOutcome.NotConfirmed, DescribeException(ex));
            }

            if (!read.Succeeded)
            {
                return (
                    AttemptOutcome.NotConfirmed,
                    read.ErrorCode ?? "revolut_get_order_failed"
                );
            }

            var state = read.State?.Trim().ToLowerInvariant();
            switch (state)
            {
                case "completed":
                    await ApplyCompletedAsync(services, context, order, intent, read, now, ct);
                    return (AttemptOutcome.Completed, null);

                case "pending":
                case "authorised":
                    return await CancelPendingAsync(
                        services,
                        context,
                        merchant,
                        order,
                        intent,
                        now,
                        ct
                    );

                case "cancelled":
                case "failed":
                    return (AttemptOutcome.Confirmed, null);

                default:
                    return (
                        AttemptOutcome.NotConfirmed,
                        $"revolut_state:{state ?? "unknown"}"
                    );
            }
        }

        private async Task<(AttemptOutcome Outcome, string? Error)> CancelPendingAsync(
            IServiceProvider services,
            ApplicationDbContext context,
            IRevolutMerchantClient merchant,
            ShopOrder order,
            RevolutOrderIntent intent,
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
                return (AttemptOutcome.NotConfirmed, DescribeException(ex));
            }

            if (cancel.Succeeded && IsCancelledState(cancel.State))
            {
                return (AttemptOutcome.Confirmed, null);
            }

            // The cancel may have raced a payment or a retry; read Revolut again.
            RevolutOrderRetrieveResult reread;
            try
            {
                reread = await merchant.GetOrderAsync(intent.OrderId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return (AttemptOutcome.NotConfirmed, DescribeException(ex));
            }

            if (reread.Succeeded && IsCancelledState(reread.State))
            {
                return (AttemptOutcome.Confirmed, null);
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
                await ApplyCompletedAsync(services, context, order, intent, reread, now, ct);
                return (AttemptOutcome.Completed, null);
            }

            return (
                AttemptOutcome.NotConfirmed,
                $"cancel_failed:{cancel.ErrorCode ?? cancel.State ?? "unknown"}"
            );
        }

        private async Task ApplyCompletedAsync(
            IServiceProvider services,
            ApplicationDbContext context,
            ShopOrder order,
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

            // A completed payment never gives a slot back. If the applier paid the
            // order (or the webhook did), convert any row still open; if it did
            // not, leave the rows open and retry later.
            var paid = await SettleAsync(
                services,
                context,
                order,
                "completed_not_applied",
                now,
                ct
            );
            if (paid)
            {
                await TryQueuePrintMaterialsAsync(services, order.Id);
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
            await SettleAsync(
                services,
                context,
                order,
                "paid_order_reservation_not_converted",
                now,
                ct
            );
        }

        /// <summary>
        /// Under the billing lock, re-reads the order. If it is Paid, converts
        /// its open reservations. Rows that stay open are backed off, never
        /// released: a payment completed (or the order is not yet confirmed).
        /// Returns whether the order is Paid.
        /// </summary>
        private async Task<bool> SettleAsync(
            IServiceProvider services,
            ApplicationDbContext context,
            ShopOrder order,
            string leftoverError,
            DateTime now,
            CancellationToken ct
        )
        {
            var slots = services.GetRequiredService<IShopQrSlotReservationService>();
            var paid = false;
            var leftovers = new List<ShopQrSlotReservation>();
            await InLockAsync(
                services,
                context,
                order.RestaurantId,
                async () =>
                {
                    await context.Entry(order).ReloadAsync(ct);
                    paid = string.Equals(
                        order.PaymentStatus,
                        ShopPaymentStatuses.Paid,
                        StringComparison.Ordinal
                    );
                    if (paid)
                    {
                        await slots.ConvertForPaidOrderAsync(order, ct);
                    }

                    leftovers = await context.ShopQrSlotReservations
                        .Where(row =>
                            row.ShopOrderId == order.Id
                            && row.Status == ShopQrSlotReservationStatuses.Open
                        )
                        .ToListAsync(ct);
                },
                ct
            );

            if (leftovers.Count > 0)
            {
                await BackOffAsync(
                    services,
                    context,
                    order.RestaurantId,
                    leftovers,
                    paid ? "paid_order_reservation_not_converted" : leftoverError,
                    now,
                    ct
                );
            }

            return paid;
        }

        private static async Task ReleaseLeftoversInLockAsync(
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

        private enum ReleaseOutcome
        {
            Released,

            /// <summary>The order was paid meanwhile: convert, never release.</summary>
            Paid,

            /// <summary>
            /// /pay extended a deadline since this pass read the rows. Its new
            /// Revolut order is not read yet, so nothing is touched.
            /// </summary>
            Superseded,

            /// <summary>A Revolut attempt this pass never read now exists.</summary>
            NewerAttempt,
        }

        /// <summary>
        /// Gives the slot back after every Revolut attempt this pass read was
        /// confirmed cancelled or failed. The Revolut reads ran outside the lock,
        /// so under the lock this re-checks what could have changed: the order
        /// was paid, /pay extended the deadline, or /pay opened a newer attempt.
        /// In each case the slot stays held (spec 4.4.4).
        /// </summary>
        private async Task ReleaseConfirmedAsync(
            IServiceProvider services,
            ApplicationDbContext context,
            ShopOrder order,
            List<ShopQrSlotReservation> due,
            List<RevolutOrderIntent> intents,
            DateTime now,
            CancellationToken ct
        )
        {
            var outcome = ReleaseOutcome.Released;
            await InLockAsync(
                services,
                context,
                order.RestaurantId,
                async () =>
                {
                    await context.Entry(order).ReloadAsync(ct);
                    if (
                        string.Equals(
                            order.PaymentStatus,
                            ShopPaymentStatuses.Paid,
                            StringComparison.Ordinal
                        )
                    )
                    {
                        outcome = ReleaseOutcome.Paid;
                        return;
                    }

                    var deadlines = await context.ShopQrSlotReservations
                        .AsNoTracking()
                        .Where(row =>
                            row.ShopOrderId == order.Id
                            && row.Status == ShopQrSlotReservationStatuses.Open
                        )
                        .Select(row => row.ReconcileAfterUtc)
                        .ToListAsync(ct);
                    if (deadlines.Any(deadline => deadline > now))
                    {
                        outcome = ReleaseOutcome.Superseded;
                        return;
                    }

                    var seen = intents.Select(intent => intent.Id).ToList();
                    if (
                        await context.RevolutOrderIntents
                            .AsNoTracking()
                            .AnyAsync(
                                row =>
                                    row.ShopOrderId == order.Id
                                    && row.Purpose
                                        == RevolutOrderIntentPurposes.ShopMaterialsOrder
                                    && !seen.Contains(row.Id),
                                ct
                            )
                    )
                    {
                        outcome = ReleaseOutcome.NewerAttempt;
                        return;
                    }

                    // Marks the order payment_failed, closes every attempt and
                    // releases the rows that are still open.
                    foreach (var intent in intents)
                    {
                        await context.Entry(intent).ReloadAsync(ct);
                        await ShopMaterialsOrderPaymentFailure.TryMarkFailedAsync(
                            context,
                            intent.OrderId,
                            ct,
                            releaseQrReservations: true,
                            nowUtc: now
                        );
                    }

                    await ReleaseLeftoversInLockAsync(context, order.Id, now, ct);
                },
                ct
            );

            switch (outcome)
            {
                case ReleaseOutcome.Paid:
                    // Paid: no failure mark and no release, convert instead.
                    await SettleAsync(
                        services,
                        context,
                        order,
                        "completed_not_applied",
                        now,
                        ct
                    );
                    break;

                case ReleaseOutcome.NewerAttempt:
                    await BackOffAsync(
                        services,
                        context,
                        order.RestaurantId,
                        due,
                        "newer_payment_attempt",
                        now,
                        ct
                    );
                    break;
            }
        }

        /// <summary>
        /// Runs a reservation write under the billing-account lock that the
        /// paid path also takes, so a row the paid path converted meanwhile is
        /// never overwritten.
        /// </summary>
        private static async Task InLockAsync(
            IServiceProvider services,
            ApplicationDbContext context,
            int restaurantId,
            Func<Task> write,
            CancellationToken ct
        )
        {
            var capacity = services.GetRequiredService<IActiveQrCapacity>();
            await using var transaction =
                await QrCapacityTransaction.BeginIfNeededAsync(context, ct);
            await capacity.LockBillingAccountAsync(restaurantId, ct);
            await write();
            if (transaction != null)
            {
                await transaction.CommitAsync(ct);
            }
        }

        private Task BackOffAsync(
            IServiceProvider services,
            ApplicationDbContext context,
            int restaurantId,
            List<ShopQrSlotReservation> reservations,
            string error,
            DateTime now,
            CancellationToken ct
        ) =>
            InLockAsync(
                services,
                context,
                restaurantId,
                async () =>
                {
                    var trimmed = error.Length > MaxErrorLength
                        ? error[..MaxErrorLength]
                        : error;

                    foreach (var reservation in reservations)
                    {
                        // The row may have changed since the sweeper read it.
                        await context.Entry(reservation).ReloadAsync(ct);
                        if (reservation.Status != ShopQrSlotReservationStatuses.Open)
                        {
                            continue;
                        }

                        reservation.ReconcileAttempts++;
                        reservation.LastReconcileAtUtc = now;
                        reservation.LastReconcileError = trimmed;
                        reservation.UpdatedAtUtc = now;

                        if (now >= reservation.CreatedAtUtc + ReviewAfter)
                        {
                            reservation.Status = ShopQrSlotReservationStatuses.Review;
                            _logger.LogWarning(
                                ReservationReviewEvent,
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
                },
                ct
            );

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
                var restaurantId = await context.ShopOrders
                    .Where(row => row.Id == orderId)
                    .Select(row => (int?)row.RestaurantId)
                    .FirstOrDefaultAsync(ct);
                var due = await DueReservationsAsync(context, orderId, now, ct);
                if (due.Count > 0 && restaurantId is int restaurant)
                {
                    await BackOffAsync(
                        scope.ServiceProvider,
                        context,
                        restaurant,
                        due,
                        DescribeException(ex),
                        now,
                        ct
                    );
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
