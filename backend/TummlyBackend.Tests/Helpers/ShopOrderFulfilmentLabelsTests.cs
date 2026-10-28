using TummlyBackend.Helpers;
using TummlyBackend.Models;
using Xunit;

namespace TummlyBackend.Tests.Helpers
{
    public class ShopOrderFulfilmentLabelsTests
    {
        private static readonly DateTime Stamp = new(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void ToDisplayLabel_PaidOrderWithNoPreparationEvidence_ReadsConfirmed()
        {
            var order = PaidOrder(ShopFulfilmentStatuses.Processing);

            Assert.Equal("Confirmed", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        [Fact]
        public void ToDisplayLabel_ProcessingStartedAloneIsConfirmed()
        {
            // ProcessingStartedAtUtc is set at payment: it means confirmed.
            var order = PaidOrder(ShopFulfilmentStatuses.Processing);
            order.ProcessingStartedAtUtc = Stamp;

            Assert.Equal("Confirmed", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        [Fact]
        public void ToDisplayLabel_PreparationStarted_ReadsPreparing()
        {
            var order = PaidOrder(ShopFulfilmentStatuses.Processing);
            order.PreparationStartedAtUtc = Stamp;

            Assert.Equal("Preparing", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        [Fact]
        public void ToDisplayLabel_ProductionStarted_ReadsInProduction()
        {
            var order = PaidOrder(ShopFulfilmentStatuses.Processing);
            order.PreparationStartedAtUtc = Stamp;
            order.ProductionStartedAtUtc = Stamp.AddHours(1);

            Assert.Equal("In production", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        [Fact]
        public void ToDisplayLabel_PreparationWithoutProduction_IsNotInProduction()
        {
            var order = PaidOrder(ShopFulfilmentStatuses.Processing);
            order.PreparationStartedAtUtc = Stamp;

            Assert.NotEqual("In production", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        [Fact]
        public void ToDisplayLabel_TrackingUrlAloneDoesNotReadDispatched()
        {
            var order = PaidOrder(ShopFulfilmentStatuses.Processing);
            order.PreparationStartedAtUtc = Stamp;
            order.ProductionStartedAtUtc = Stamp.AddHours(1);
            order.TrackingUrl = "https://track.example/label-only";

            Assert.Equal("In production", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        [Fact]
        public void ToDisplayLabel_DispatchEvidence_ReadsDispatched()
        {
            var order = PaidOrder(ShopFulfilmentStatuses.InTransit);
            order.ProductionStartedAtUtc = Stamp;
            order.DispatchedAtUtc = Stamp.AddHours(2);
            order.DispatchCarrierName = "DPD";
            order.DispatchCarrierReference = "DPD-123";

            Assert.Equal("Dispatched", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        [Fact]
        public void ToDisplayLabel_InTransitWithoutDispatchEvidence_DoesNotReadDispatched()
        {
            var order = PaidOrder(ShopFulfilmentStatuses.InTransit);
            order.ProductionStartedAtUtc = Stamp;

            Assert.Equal("In production", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        [Fact]
        public void ToDisplayLabel_DeliveryEvidence_ReadsDelivered()
        {
            var order = PaidOrder(ShopFulfilmentStatuses.Delivered);
            order.DispatchedAtUtc = Stamp;
            order.DeliveredAtUtc = Stamp.AddDays(2);

            Assert.Equal("Delivered", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        [Fact]
        public void ToDisplayLabel_DeliveredWithoutDeliveryEvidence_DoesNotReadDelivered()
        {
            var order = PaidOrder(ShopFulfilmentStatuses.Delivered);
            order.DispatchedAtUtc = Stamp;

            Assert.Equal("Dispatched", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        [Fact]
        public void ToDisplayLabel_Cancelled_ReadsCancelled()
        {
            var order = PaidOrder(ShopFulfilmentStatuses.Cancelled);

            Assert.Equal("Cancelled", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        [Fact]
        public void ToDisplayLabel_UnpaidOrder_ReadsAwaitingPaymentEvenWhenProcessing()
        {
            var order = PaidOrder(ShopFulfilmentStatuses.Processing);
            order.PaymentStatus = ShopPaymentStatuses.AwaitingPayment;
            order.PreparationStartedAtUtc = Stamp;

            Assert.Equal("Awaiting payment", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        private static ShopOrder PaidOrder(string fulfilmentStatus)
        {
            return new ShopOrder
            {
                FulfilmentStatus = fulfilmentStatus,
                PaymentStatus = ShopPaymentStatuses.Paid,
            };
        }
    }
}
