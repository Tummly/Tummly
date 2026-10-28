using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class ShopOrderFulfilmentLabels
    {
        public const string AwaitingPaymentLabel = "Awaiting payment";

        public const string ComplimentaryStarterKitLabel =
            "Complimentary Starter Kit";

        /// <summary>
        /// Customer fulfilment label, read from the order's evidence. Each
        /// stage needs its own evidence timestamp, so a status with no proof
        /// falls back to the stage before it. The stored status does not change.
        /// </summary>
        public static string ToDisplayLabel(ShopOrder order)
        {
            if (
                string.Equals(
                    order.PaymentStatus,
                    ShopPaymentStatuses.AwaitingPayment,
                    StringComparison.Ordinal
                )
                || string.Equals(
                    order.PaymentStatus,
                    ShopPaymentStatuses.PaymentFailed,
                    StringComparison.Ordinal
                )
            )
            {
                return AwaitingPaymentLabel;
            }

            if (order.FulfilmentStatus == ShopFulfilmentStatuses.Cancelled)
            {
                return "Cancelled";
            }

            if (
                order.FulfilmentStatus == ShopFulfilmentStatuses.Delivered
                && order.DeliveredAtUtc != null
            )
            {
                return "Delivered";
            }

            if (
                order.DispatchedAtUtc != null
                && (
                    order.FulfilmentStatus == ShopFulfilmentStatuses.InTransit
                    || order.FulfilmentStatus == ShopFulfilmentStatuses.Delivered
                )
            )
            {
                return "Dispatched";
            }

            if (order.ProductionStartedAtUtc != null)
            {
                return "In production";
            }

            return order.PreparationStartedAtUtc != null ? "Preparing" : "Confirmed";
        }

        public static string ToPaymentDisplayLabel(
            string storedStatus,
            bool isComplimentary = false
        )
        {
            if (
                isComplimentary
                && string.Equals(
                    storedStatus,
                    ShopPaymentStatuses.Paid,
                    StringComparison.Ordinal
                )
            )
            {
                return "Free";
            }

            return storedStatus switch
            {
                ShopPaymentStatuses.Paid => "Paid",
                ShopPaymentStatuses.Refunded => "Refunded",
                ShopPaymentStatuses.AwaitingPayment => AwaitingPaymentLabel,
                ShopPaymentStatuses.PaymentFailed => "Payment failed",
                _ => storedStatus,
            };
        }

        public static string? ToOrderKindLabel(bool isComplimentary) =>
            isComplimentary ? ComplimentaryStarterKitLabel : null;

        public static bool IsTrackingVisible(string? storedStatus, string? trackingUrl)
        {
            if (string.IsNullOrWhiteSpace(trackingUrl))
            {
                return false;
            }

            return storedStatus is ShopFulfilmentStatuses.InTransit
                or ShopFulfilmentStatuses.Delivered;
        }
    }
}
