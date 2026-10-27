using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class ShopOrderPaymentConfirmService
        : IShopOrderPaymentConfirmService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRevolutMerchantClient _merchant;
        private readonly IRevolutOrderCompletedApplier _applier;
        private readonly IPrintReadyQrMaterialsWork _printWork;
        private readonly ILogger<ShopOrderPaymentConfirmService> _logger;

        public ShopOrderPaymentConfirmService(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant,
            IRevolutOrderCompletedApplier applier,
            IPrintReadyQrMaterialsWork printWork,
            ILogger<ShopOrderPaymentConfirmService> logger
        )
        {
            _context = context;
            _merchant = merchant;
            _applier = applier;
            _printWork = printWork;
            _logger = logger;
        }

        public async Task<bool> ConfirmAsync(
            Guid shopOrderId,
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            var paymentStatus = await _context.ShopOrders
                .AsNoTracking()
                .Where(row =>
                    row.Id == shopOrderId && row.RestaurantId == restaurantId
                )
                .Select(row => row.PaymentStatus)
                .FirstOrDefaultAsync(cancellationToken);
            if (
                !string.Equals(
                    paymentStatus,
                    ShopPaymentStatuses.AwaitingPayment,
                    StringComparison.Ordinal
                )
            )
            {
                return false;
            }

            // Every attempt, open or closed: the /pay reuse path leaves older
            // Revolut orders behind and a closed attempt can still be paid.
            var intents = await _context.RevolutOrderIntents
                .AsNoTracking()
                .Where(row =>
                    row.ShopOrderId == shopOrderId
                    && row.Purpose == RevolutOrderIntentPurposes.ShopMaterialsOrder
                )
                .OrderByDescending(row => row.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            foreach (var intent in intents)
            {
                RevolutOrderRetrieveResult read;
                try
                {
                    read = await _merchant.GetOrderAsync(
                        intent.OrderId,
                        cancellationToken
                    );
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(
                        ex,
                        "Could not read Revolut order {RevolutOrderId} to confirm shop order {ShopOrderId}",
                        intent.OrderId,
                        shopOrderId
                    );
                    continue;
                }

                if (
                    !read.Succeeded
                    || !string.Equals(
                        read.State?.Trim(),
                        "completed",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    continue;
                }

                return await ApplyCompletedAsync(
                    shopOrderId,
                    intent,
                    read,
                    cancellationToken
                );
            }

            return false;
        }

        private async Task<bool> ApplyCompletedAsync(
            Guid shopOrderId,
            RevolutOrderIntent intent,
            RevolutOrderRetrieveResult read,
            CancellationToken cancellationToken
        )
        {
            try
            {
                // Same path as ORDER_COMPLETED and the reservation sweeper: marks
                // the order Paid and converts its reservations in one
                // transaction. A later webhook finds the order Paid.
                await _applier.ApplyAsync(
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
                    cancellationToken
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The webhook or the sweeper settles it later; the money is
                // never touched here.
                _logger.LogError(
                    ex,
                    "Applying the completed Revolut order {RevolutOrderId} failed for shop order {ShopOrderId}",
                    intent.OrderId,
                    shopOrderId
                );
                return false;
            }

            var paid = await _context.ShopOrders
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.Id == shopOrderId
                        && row.PaymentStatus == ShopPaymentStatuses.Paid,
                    cancellationToken
                );
            if (paid)
            {
                await TryQueuePrintMaterialsAsync(shopOrderId);
            }

            return paid;
        }

        private async Task TryQueuePrintMaterialsAsync(Guid shopOrderId)
        {
            try
            {
                // Payment is committed. Print work must not undo it.
                await _printWork.RequestShopOrderEnsureAsync(
                    shopOrderId,
                    CancellationToken.None
                );
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
    }
}
