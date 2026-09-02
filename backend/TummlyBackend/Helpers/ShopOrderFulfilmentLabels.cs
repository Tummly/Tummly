using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    internal static class ShopOrderFulfilmentLabels
    {
        public static string ToDisplayLabel(string? storedStatus)
        {
            return storedStatus switch
            {
                ShopFulfilmentStatuses.Processing => "Processing",
                ShopFulfilmentStatuses.InTransit => "Dispatched",
                ShopFulfilmentStatuses.Delivered => "Delivered",
                ShopFulfilmentStatuses.Cancelled => "Cancelled",
                _ => "Processing",
            };
        }

        public static string ToPaymentDisplayLabel(string storedStatus)
        {
            return storedStatus switch
            {
                ShopPaymentStatuses.Paid => "Paid",
                ShopPaymentStatuses.Refunded => "Refunded",
                _ => storedStatus,
            };
        }

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
