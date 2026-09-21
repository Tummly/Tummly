using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    internal static class ShopOrderCancelRefund
    {
        public static string IdempotencyKey(Guid shopOrderId) =>
            $"shop-cancel:{shopOrderId:D}";

        /// <summary>
        /// Returns null when refund not needed or Merchant succeeded.
        /// Returns error code when Merchant failed (caller must not cancel).
        /// </summary>
        public static async Task<string?> TryFullRefundAsync(
            IRevolutMerchantClient merchant,
            ShopOrder order,
            CancellationToken cancellationToken
        )
        {
            if (order.IsComplimentary)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(order.RevolutOrderId))
            {
                // Paid materials cannot cancel without a Merchant payment id.
                if (
                    string.Equals(
                        order.PaymentStatus,
                        ShopPaymentStatuses.Paid,
                        StringComparison.Ordinal
                    )
                )
                {
                    return "revolut_order_missing";
                }

                return null;
            }

            var refunded = await merchant.RefundOrderAsync(
                order.RevolutOrderId.Trim(),
                amountMinor: null,
                IdempotencyKey(order.Id),
                cancellationToken
            );
            if (!refunded.Succeeded)
            {
                return refunded.ErrorCode ?? "revolut_refund_failed";
            }

            return null;
        }
    }
}
