using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class ShopOrderFulfilmentLabels
    {
        public const string AwaitingPaymentLabel = "Awaiting payment";

        public const string ComplimentaryStarterKitLabel =
            "Complimentary Starter Kit";

        /// <summary>
        /// Fulfilment label. An order that is not paid yet has no fulfilment,
        /// so it reads "Awaiting payment". A paid order still in processing
        /// reads "Confirmed", or "Preparing" once Admin marks production
        /// started. The stored status does not change.
        /// </summary>
        public static string ToDisplayLabel(
            string? storedStatus,
            string? paymentStatus = null,
            bool productionStarted = false
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

            var processingLabel = productionStarted ? "Preparing" : "Confirmed";
            return storedStatus switch
            {
                ShopFulfilmentStatuses.Processing => processingLabel,
                ShopFulfilmentStatuses.InTransit => "Dispatched",
                ShopFulfilmentStatuses.Delivered => "Delivered",
                ShopFulfilmentStatuses.Cancelled => "Cancelled",
                _ => processingLabel,
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
