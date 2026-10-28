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
        /// <param name="releaseQrReservations">
        /// Release the order's open QR slot reservations. Pass true only from
        /// the reservation sweeper, under the billing-account lock, after it
        /// confirmed that every Revolut attempt of the order is cancelled or
        /// failed. Webhooks, the /pay reuse path and unreadable sessions pass
        /// false: one Revolut order ending does not prove no other can be paid.
        /// </param>
        /// <param name="nowUtc">Clock reading of the caller; defaults to UTC now.</param>
        public static async Task<bool> TryMarkFailedAsync(
            ApplicationDbContext context,
            string revolutOrderId,
            CancellationToken cancellationToken = default,
            bool releaseQrReservations = false,
            DateTime? nowUtc = null
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

            var now = nowUtc ?? DateTime.UtcNow;
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

            // A paid order's reservations are converted and final. Only an
            // order that is not paid gives its open slots back.
            if (
                releaseQrReservations
                && !string.Equals(
                    shopOrder.PaymentStatus,
                    ShopPaymentStatuses.Paid,
                    StringComparison.Ordinal
                )
                && await ShopQrSlotReservationRelease.ReleaseOpenForOrderAsync(
                    context,
                    shopOrder.Id,
                    now,
                    cancellationToken
                ) > 0
            )
            {
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
