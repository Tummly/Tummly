using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class ShopOrderCancelRules
    {
        public static bool CanCancel(ShopOrder order)
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
                )
                && order.ProductionStartedAtUtc == null;
        }

        public static string? CancelBlockReason(ShopOrder order)
        {
            if (
                string.Equals(
                    order.FulfilmentStatus,
                    ShopFulfilmentStatuses.InTransit,
                    StringComparison.Ordinal
                )
            )
            {
                return "in_transit";
            }

            if (
                string.Equals(
                    order.FulfilmentStatus,
                    ShopFulfilmentStatuses.Delivered,
                    StringComparison.Ordinal
                )
            )
            {
                return "delivered";
            }

            if (
                order.ProductionStartedAtUtc != null
                && string.Equals(
                    order.FulfilmentStatus,
                    ShopFulfilmentStatuses.Processing,
                    StringComparison.Ordinal
                )
            )
            {
                return "production_started";
            }

            return null;
        }

        public static bool IsReorderEligible(ShopOrder order)
        {
            return string.Equals(
                    order.PaymentStatus,
                    ShopPaymentStatuses.Paid,
                    StringComparison.Ordinal
                )
                || string.Equals(
                    order.PaymentStatus,
                    ShopPaymentStatuses.Refunded,
                    StringComparison.Ordinal
                );
        }
    }
}
