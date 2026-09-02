namespace TummlyBackend.DTOs.Shop
{
    public sealed class ShopCartLineDto
    {
        public required string SkuId { get; init; }

        public int Quantity { get; init; }

        public required string Title { get; init; }

        public int UnitNetPence { get; init; }

        public int LineNetPence { get; init; }
    }

    public sealed class ShopCartDto
    {
        public int LocationId { get; init; }

        public required IReadOnlyList<ShopCartLineDto> Lines { get; init; }

        public int MaterialsNetPence { get; init; }

        public required string Currency { get; init; }
    }

    public sealed class UpsertShopCartLineRequest
    {
        public int LocationId { get; set; }

        public string SkuId { get; set; } = string.Empty;

        public int Quantity { get; set; }
    }
}
