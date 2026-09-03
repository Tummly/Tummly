namespace TummlyBackend.DTOs.Shop
{
    public sealed class CancelShopOrderRequest
    {
        public int LocationId { get; set; }

        public string Reason { get; set; } = string.Empty;
    }

    public sealed class ShopReorderPrefillLineDto
    {
        public required string SkuId { get; init; }

        public int Quantity { get; init; }

        public required string Title { get; init; }

        public int UnitNetPence { get; init; }

        public int LineNetPence { get; init; }
    }

    public sealed class ShopReorderPrefillDto
    {
        public int LocationId { get; init; }

        public required IReadOnlyList<ShopReorderPrefillLineDto> Lines { get; init; }

        public required ShopOrderShipToDto ShipTo { get; init; }

        public required string DeliveryMethod { get; init; }

        public required string SourceOrderNumber { get; init; }
    }
}
