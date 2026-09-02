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
                Lines = order.Lines
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
                    .ToList(),
                ShipTo = new ShopOrderShipToDto
                {
                    ContactName = order.ShipToContactName,
                    ContactPhone = order.ShipToContactPhone,
                    AddressLine1 = order.ShipToAddressLine1,
                    AddressLine2 = order.ShipToAddressLine2,
                    Postcode = order.ShipToPostcode,
                    Country = order.ShipToCountry,
                    DeliveryInstructions = order.DeliveryInstructions,
                },
            };
        }
    }
}
