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
            SetDispatchEvidence(order);

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
        public void ToDisplayLabel_LegacyDispatchTimeWithoutCarrierReference_ReadsLastSubstantiatedState()
        {
            // Round 1 stamped DispatchedAtUtc with only a tracking URL.
            var order = PaidOrder(ShopFulfilmentStatuses.InTransit);
            order.PreparationStartedAtUtc = Stamp;
            order.ProductionStartedAtUtc = Stamp.AddHours(1);
            order.DispatchedAtUtc = Stamp.AddHours(2);
            order.DispatchCarrierName = "DPD";
            order.TrackingUrl = "https://track.example/legacy";

            Assert.Equal("In production", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        [Fact]
        public void ToDisplayLabel_LegacyDispatchWithoutProduction_ReadsPreparing()
        {
            var order = PaidOrder(ShopFulfilmentStatuses.InTransit);
            order.PreparationStartedAtUtc = Stamp;
            order.DispatchedAtUtc = Stamp.AddHours(2);

            Assert.Equal("Preparing", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        [Fact]
        public void ToDisplayLabel_DispatchWithoutSourceOrActor_DoesNotReadDispatched()
        {
            var order = PaidOrder(ShopFulfilmentStatuses.InTransit);
            order.ProductionStartedAtUtc = Stamp;
            SetDispatchEvidence(order);
            order.DispatchSource = null;
            order.DispatchedByAdminUserId = null;

            Assert.Equal("In production", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        [Fact]
        public void ToDisplayLabel_DeliveryEvidence_ReadsDelivered()
        {
            var order = PaidOrder(ShopFulfilmentStatuses.Delivered);
            order.ProductionStartedAtUtc = Stamp;
            SetDispatchEvidence(order);
            order.DeliveredAtUtc = Stamp.AddDays(2);

            Assert.Equal("Delivered", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        [Fact]
        public void ToDisplayLabel_DeliveredWithoutDispatchEvidence_DoesNotReadDelivered()
        {
            var order = PaidOrder(ShopFulfilmentStatuses.Delivered);
            order.ProductionStartedAtUtc = Stamp;
            order.DispatchedAtUtc = Stamp;
            order.DeliveredAtUtc = Stamp.AddDays(2);

            Assert.Equal("In production", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        [Fact]
        public void ToDisplayLabel_DispatchEvidenceWithoutDeliveryTime_ReadsDispatched()
        {
            var order = PaidOrder(ShopFulfilmentStatuses.Delivered);
            order.ProductionStartedAtUtc = Stamp;
            SetDispatchEvidence(order);

            Assert.Equal("Dispatched", ShopOrderFulfilmentLabels.ToDisplayLabel(order));
        }

        [Fact]
        public void IsDispatchEvidenceMissing_DispatchedOrDeliveredWithoutEvidence_IsTrue()
        {
            var inTransit = PaidOrder(ShopFulfilmentStatuses.InTransit);
            inTransit.DispatchedAtUtc = Stamp;
            var delivered = PaidOrder(ShopFulfilmentStatuses.Delivered);
            delivered.DispatchedAtUtc = Stamp;
            delivered.DeliveredAtUtc = Stamp.AddDays(1);

            Assert.True(ShopOrderDispatchEvidence.IsMissing(inTransit));
            Assert.True(ShopOrderDispatchEvidence.IsMissing(delivered));
        }

        [Fact]
        public void IsDispatchEvidenceMissing_CompleteEvidenceOrOtherStatus_IsFalse()
        {
            var complete = PaidOrder(ShopFulfilmentStatuses.InTransit);
            SetDispatchEvidence(complete);
            var processing = PaidOrder(ShopFulfilmentStatuses.Processing);
            processing.ProductionStartedAtUtc = Stamp;

            Assert.False(ShopOrderDispatchEvidence.IsMissing(complete));
            Assert.False(ShopOrderDispatchEvidence.IsMissing(processing));
        }

        [Theory]
        [InlineData(null, null, null, null)]
        [InlineData("", "DPD", "DPD-123", 7)]
        [InlineData("admin_manual", null, "DPD-123", 7)]
        [InlineData("admin_manual", "DPD", null, 7)]
        [InlineData("admin_manual", "DPD", "", 7)]
        [InlineData("admin_manual", "DPD", "DPD-123", null)]
        public void CompleteEvidenceExpression_AgreesWithIsComplete(
            string? source,
            string? carrierName,
            string? carrierReference,
            int? actorId
        )
        {
            var order = PaidOrder(ShopFulfilmentStatuses.InTransit);
            order.DispatchedAtUtc = Stamp;
            order.DispatchSource = source;
            order.DispatchCarrierName = carrierName;
            order.DispatchCarrierReference = carrierReference;
            order.DispatchedByAdminUserId = actorId;
            var rows = new[] { order }.AsQueryable();

            var fromExpression = rows.Where(ShopOrderDispatchEvidence.CompleteExpression).Any();

            Assert.Equal(ShopOrderDispatchEvidence.IsComplete(order), fromExpression);
        }

        [Theory]
        [InlineData(ShopFulfilmentStatuses.InTransit)]
        [InlineData(ShopFulfilmentStatuses.Delivered)]
        [InlineData(ShopFulfilmentStatuses.Processing)]
        public void MissingExpression_AgreesWithIsMissing(string fulfilment)
        {
            var order = PaidOrder(fulfilment);
            order.DispatchedAtUtc = Stamp;
            var rows = new[] { order }.AsQueryable();

            var fromExpression = rows.Where(ShopOrderDispatchEvidence.MissingExpression).Any();

            Assert.Equal(ShopOrderDispatchEvidence.IsMissing(order), fromExpression);
        }

        [Fact]
        public void IsTrackingVisible_TrackingUrlWithoutDispatchEvidence_IsHidden()
        {
            var order = PaidOrder(ShopFulfilmentStatuses.InTransit);
            order.DispatchedAtUtc = Stamp;
            order.TrackingUrl = "https://track.example/legacy";

            Assert.False(ShopOrderFulfilmentLabels.IsTrackingVisible(order));

            SetDispatchEvidence(order);
            Assert.True(ShopOrderFulfilmentLabels.IsTrackingVisible(order));
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

        private static void SetDispatchEvidence(ShopOrder order)
        {
            order.DispatchedAtUtc = Stamp.AddHours(2);
            order.DispatchSource = ShopFulfilmentEvidenceSources.AdminManual;
            order.DispatchedByAdminUserId = 7;
            order.DispatchCarrierName = "DPD";
            order.DispatchCarrierReference = "DPD-123";
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
