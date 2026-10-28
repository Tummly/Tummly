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

            // Dispatched and Delivered need carrier evidence. A dispatch time,
            // tracking URL or delivery time alone falls back to the stage
            // before it. Admin sees the missing evidence marker separately.
            var hasDispatchEvidence = ShopOrderDispatchEvidence.IsComplete(order);

            if (
                order.FulfilmentStatus == ShopFulfilmentStatuses.Delivered
                && order.DeliveredAtUtc != null
                && hasDispatchEvidence
            )
            {
                return "Delivered";
            }

            if (
                hasDispatchEvidence
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

        /// <summary>
        /// The tracking link shows only once carrier evidence exists. A link
        /// without evidence is supporting information for Admin, not proof.
        /// </summary>
        public static bool IsTrackingVisible(ShopOrder order)
        {
            if (string.IsNullOrWhiteSpace(order.TrackingUrl))
            {
                return false;
            }

            return (
                    order.FulfilmentStatus is ShopFulfilmentStatuses.InTransit
                        or ShopFulfilmentStatuses.Delivered
                )
                && ShopOrderDispatchEvidence.IsComplete(order);
        }
    }
}
