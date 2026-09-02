namespace TummlyBackend.DTOs.Shop
{
    public sealed class PlaceShopOrderLineRequest
    {
        public string SkuId { get; set; } = string.Empty;

        public int Quantity { get; set; }
    }

    public sealed class PlaceShopOrderShipToRequest
    {
        public string ContactName { get; set; } = string.Empty;

        public string? ContactPhone { get; set; }

        public string AddressLine1 { get; set; } = string.Empty;

        public string? AddressLine2 { get; set; }

        public string Postcode { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;

        public string? DeliveryInstructions { get; set; }
    }

    public sealed class PlaceShopOrderRequest
    {
        public int LocationId { get; set; }

        public bool? FromCart { get; set; }

        public List<PlaceShopOrderLineRequest>? Lines { get; set; }

        public string DeliveryMethod { get; set; } = string.Empty;

        public int ExpectedGrossPence { get; set; }

        public PlaceShopOrderShipToRequest ShipTo { get; set; } = new();
    }

    public sealed class ShopOrderLineDto
    {
        public required string SkuId { get; init; }

        public required string Title { get; init; }

        public required string MaterialType { get; init; }

        public int Quantity { get; init; }

        public int UnitNetPence { get; init; }

        public int LineNetPence { get; init; }
    }

    public sealed class ShopOrderDto
    {
        public Guid Id { get; init; }

        public required string OrderNumber { get; init; }

        public int LocationId { get; init; }

        public required string LocationName { get; init; }

        public required string PaymentStatus { get; init; }

        public string? FulfilmentStatus { get; init; }

        public required string DeliveryMethod { get; init; }

        public int MaterialsNetPence { get; init; }

        public int VatPence { get; init; }

        public int DeliveryNetPence { get; init; }

        public int GrossPence { get; init; }

        public required string Currency { get; init; }

        public required IReadOnlyList<ShopOrderLineDto> Lines { get; init; }

        public required ShopOrderShipToDto ShipTo { get; init; }
    }

    public sealed class ShopOrderPayResponseDto
    {
        public required string Outcome { get; init; }

        public required string RedirectUrl { get; init; }
    }

    public sealed class ShopOrderShipToDto
    {
        public required string ContactName { get; init; }

        public string? ContactPhone { get; init; }

        public required string AddressLine1 { get; init; }

        public string? AddressLine2 { get; init; }

        public required string Postcode { get; init; }

        public required string Country { get; init; }

        public string? DeliveryInstructions { get; init; }
    }

    public sealed class ShopDeliveryDefaultsDto
    {
        public int LocationId { get; init; }

        public required string LocationName { get; init; }

        public required string ContactName { get; init; }

        public string? ContactPhone { get; init; }

        public required string AddressLine1 { get; init; }

        public string? AddressLine2 { get; init; }

        public required string Postcode { get; init; }

        public required string Country { get; init; }
    }
}
