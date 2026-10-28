using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    internal static class ShopOrderFulfilmentLabels
    {
        public const string AwaitingPaymentLabel = "Awaiting payment";

        public const string ComplimentaryStarterKitLabel =
            "Complimentary Starter Kit";

        /// <summary>
        /// Fulfilment label. An order that is not paid yet has no fulfilment,
        /// so it reads "Awaiting payment" instead of a default "Processing".
        /// </summary>
        public static string ToDisplayLabel(
            string? storedStatus,
            string? paymentStatus = null
        )
        {
            if (
                string.Equals(
                    paymentStatus,
                    ShopPaymentStatuses.AwaitingPayment,
                    StringComparison.Ordinal
                )
                || string.Equals(
                    paymentStatus,
                    ShopPaymentStatuses.PaymentFailed,
                    StringComparison.Ordinal
                )
            )
            {
                return AwaitingPaymentLabel;
            }

            return storedStatus switch
            {
                ShopFulfilmentStatuses.Processing => "Processing",
                ShopFulfilmentStatuses.InTransit => "Dispatched",
                ShopFulfilmentStatuses.Delivered => "Delivered",
                ShopFulfilmentStatuses.Cancelled => "Cancelled",
                _ => "Processing",
            };
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
