using TummlyBackend.Helpers;
using TummlyBackend.Models;
using Xunit;

namespace TummlyBackend.Tests.Helpers
{
    public class ShopOrderCancelRulesTests
    {
        private static ShopOrder PaidProcessing() =>
            new()
            {
                PaymentStatus = ShopPaymentStatuses.Paid,
                FulfilmentStatus = ShopFulfilmentStatuses.Processing,
            };

        [Fact]
        public void CanCancel_True_WhenPaidProcessingAndNoStamp()
        {
            Assert.True(ShopOrderCancelRules.CanCancel(PaidProcessing()));
        }

        [Fact]
        public void CanCancel_False_WhenProductionStarted()
        {
            var order = PaidProcessing();
            order.ProductionStartedAtUtc = DateTime.UtcNow;
            Assert.False(ShopOrderCancelRules.CanCancel(order));
            Assert.Equal(
                "production_started",
                ShopOrderCancelRules.CancelBlockReason(order)
            );
        }

        [Fact]
        public void CancelBlockReason_InTransit_Unchanged()
        {
            var order = PaidProcessing();
            order.FulfilmentStatus = ShopFulfilmentStatuses.InTransit;
            Assert.Equal(
                "in_transit",
                ShopOrderCancelRules.CancelBlockReason(order)
            );
        }
    }
}
