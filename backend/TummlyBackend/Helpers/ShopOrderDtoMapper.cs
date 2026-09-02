using TummlyBackend.DTOs.Shop;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    internal static class ShopOrderDtoMapper
    {
        private const string CurrencyGbp = "GBP";

        public static ShopOrderDto Map(ShopOrder order)
        {
            return new ShopOrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                LocationId = order.LocationId,
                LocationName = order.LocationNameSnapshot,
                PaymentStatus = order.PaymentStatus,
                FulfilmentStatus = order.FulfilmentStatus,
                DeliveryMethod = order.DeliveryMethod,
                MaterialsNetPence = order.MaterialsNetPence,
                VatPence = order.VatPence,
                DeliveryNetPence = order.DeliveryNetPence,
                GrossPence = order.GrossPence,
                Currency = CurrencyGbp,
                Lines = MapLines(order),
                ShipTo = MapShipTo(order),
            };
        }

        public static ShopOrderOperatorDetailDto MapOperatorDetail(ShopOrder order)
        {
            var trackingVisible = ShopOrderFulfilmentLabels.IsTrackingVisible(
                order.FulfilmentStatus,
                order.TrackingUrl
            );

            return new ShopOrderOperatorDetailDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = ShopOrderPresentation.FormatDisplayDate(order.CreatedAtUtc),
                LocationId = order.LocationId,
                LocationName = order.LocationNameSnapshot,
                PlacedBy = order.PlacedByNameSnapshot,
                PaymentStatus = order.PaymentStatus,
                PaymentStatusLabel = ShopOrderFulfilmentLabels.ToPaymentDisplayLabel(
                    order.PaymentStatus
                ),
                FulfilmentStatus = order.FulfilmentStatus,
                FulfilmentStatusLabel = ShopOrderFulfilmentLabels.ToDisplayLabel(
                    order.FulfilmentStatus
                ),
                DeliveryMethod = order.DeliveryMethod,
                MaterialsNetPence = order.MaterialsNetPence,
                VatPence = order.VatPence,
                DeliveryNetPence = order.DeliveryNetPence,
                GrossPence = order.GrossPence,
                Currency = CurrencyGbp,
                Lines = MapLines(order),
                ShipTo = MapShipTo(order),
                PaymentSummary = new ShopOrderPaymentSummaryDto
                {
                    PaidAtUtc = order.PaidAtUtc,
                    RevolutOrderId = order.RevolutOrderId,
                },
                Progress = new ShopOrderProgressDto
                {
                    OrderReceivedAtUtc = order.PaidAtUtc,
                    ProcessingStartedAtUtc = order.ProcessingStartedAtUtc,
                    DispatchedAtUtc = order.DispatchedAtUtc,
                    DeliveredAtUtc = order.DeliveredAtUtc,
                    TrackingUrl = trackingVisible ? order.TrackingUrl : null,
                },
                UpdatedAtUtc = order.UpdatedAtUtc,
                CanCancel = ShopOrderCancelRules.CanCancel(order),
                CancelBlockReason = ShopOrderCancelRules.CancelBlockReason(order),
            };
        }

        internal static ShopOrderShipToDto MapShipTo(ShopOrder order)
        {
            return new ShopOrderShipToDto
            {
                ContactName = order.ShipToContactName,
                ContactPhone = order.ShipToContactPhone,
                AddressLine1 = order.ShipToAddressLine1,
                AddressLine2 = order.ShipToAddressLine2,
                Postcode = order.ShipToPostcode,
                Country = order.ShipToCountry,
                DeliveryInstructions = order.DeliveryInstructions,
            };
        }

        private static IReadOnlyList<ShopOrderLineDto> MapLines(ShopOrder order)
        {
            return order.Lines
                .OrderBy(line => line.CatalogSkuId)
                .Select(line => new ShopOrderLineDto
                {
                    SkuId = line.CatalogSkuId,
                    Title = line.TitleSnapshot,
                    MaterialType = line.MaterialType,
                    Quantity = line.Quantity,
                    UnitNetPence = line.UnitNetPence,
                    LineNetPence = line.LineNetPence,
                })
                .ToList();
        }
    }
}
