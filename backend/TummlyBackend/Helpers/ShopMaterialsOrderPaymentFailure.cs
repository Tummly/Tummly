using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Marks Shop materials orders payment_failed when Revolut reports a terminal
    /// non-completed state (PRD legal transition awaiting_payment → payment_failed).
    /// </summary>
    public static class ShopMaterialsOrderPaymentFailure
    {
        public static async Task<bool> TryMarkFailedAsync(
            ApplicationDbContext context,
            string revolutOrderId,
            CancellationToken cancellationToken = default
        )
        {
            var trimmed = revolutOrderId.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return false;
            }

            var intent = await context.RevolutOrderIntents
                .FirstOrDefaultAsync(
                    row =>
                        row.OrderId == trimmed
                        && row.Purpose
                            == RevolutOrderIntentPurposes.ShopMaterialsOrder,
                    cancellationToken
                );
            if (intent?.ShopOrderId is not Guid shopOrderId)
            {
                return false;
            }

            var shopOrder = await context.ShopOrders
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == shopOrderId
                        && row.RestaurantId == intent.RestaurantId,
                    cancellationToken
                );
            if (shopOrder == null)
            {
                return false;
            }

            var now = DateTime.UtcNow;
            var changed = false;

            if (
                string.Equals(
                    shopOrder.PaymentStatus,
                    ShopPaymentStatuses.AwaitingPayment,
                    StringComparison.Ordinal
                )
            )
            {
                shopOrder.PaymentStatus = ShopPaymentStatuses.PaymentFailed;
                shopOrder.UpdatedAtUtc = now;
                changed = true;
            }

            if (intent.IsOpen)
            {
                intent.IsOpen = false;
                changed = true;
            }

            if (changed)
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            return changed;
        }
    }
}
