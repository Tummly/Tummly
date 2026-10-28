using TummlyBackend.Helpers;
using TummlyBackend.Models;
using Xunit;

namespace TummlyBackend.Tests.Helpers
{
    public class ShopOrderFulfilmentLabelsTests
    {
        [Fact]
        public void ToDisplayLabel_PaidProcessingOrder_ReadsConfirmed()
        {
            Assert.Equal(
                "Confirmed",
                ShopOrderFulfilmentLabels.ToDisplayLabel(
                    ShopFulfilmentStatuses.Processing,
                    ShopPaymentStatuses.Paid
                )
            );
        }

        [Fact]
        public void ToDisplayLabel_ProcessingWithProductionStarted_ReadsPreparing()
        {
            Assert.Equal(
                "Preparing",
                ShopOrderFulfilmentLabels.ToDisplayLabel(
                    ShopFulfilmentStatuses.Processing,
                    ShopPaymentStatuses.Paid,
                    productionStarted: true
                )
            );
        }

        [Fact]
        public void ToDisplayLabel_ProductionStartedIsIgnoredAfterDispatch()
        {
            Assert.Equal(
                "Dispatched",
                ShopOrderFulfilmentLabels.ToDisplayLabel(
                    ShopFulfilmentStatuses.InTransit,
                    ShopPaymentStatuses.Paid,
                    productionStarted: true
                )
            );
        }

        [Fact]
        public void ToDisplayLabel_UnpaidOrder_ReadsAwaitingPaymentEvenWhenProcessing()
        {
            Assert.Equal(
                "Awaiting payment",
                ShopOrderFulfilmentLabels.ToDisplayLabel(
                    ShopFulfilmentStatuses.Processing,
                    ShopPaymentStatuses.AwaitingPayment
                )
            );
        }

        [Theory]
        [InlineData(ShopFulfilmentStatuses.InTransit, "Dispatched")]
        [InlineData(ShopFulfilmentStatuses.Delivered, "Delivered")]
        [InlineData(ShopFulfilmentStatuses.Cancelled, "Cancelled")]
        public void ToDisplayLabel_LaterStates_KeepTheirLabels(
            string storedStatus,
            string expected
        )
        {
            Assert.Equal(
                expected,
                ShopOrderFulfilmentLabels.ToDisplayLabel(
                    storedStatus,
                    ShopPaymentStatuses.Paid
                )
            );
        }
    }
}
