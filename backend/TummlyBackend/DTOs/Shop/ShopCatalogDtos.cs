using TummlyBackend.Helpers;

namespace TummlyBackend.DTOs.Shop
{
    public sealed class ShopCatalogListItemDto
    {
        public required string SkuId { get; init; }

        public required string Title { get; init; }

        public required string Category { get; init; }

        public required string Description { get; init; }

        public int UnitNetPence { get; init; }

        public IReadOnlyList<ShopMaterialPackRow> Packs { get; init; } = [];

        public int DeliveryNetPence { get; init; }

        public required string Currency { get; init; }

        public required string ImageUrl { get; init; }

        public IReadOnlyList<string> ImageUrls { get; init; } = [];

        public required string QrType { get; init; }

        public bool? IsPlanIncluded { get; init; }

        public string? PopularBadge { get; init; }

        public IReadOnlyList<string> Tags { get; init; } = [];
    }

    public sealed class ShopCatalogDetailDto
    {
        public required string SkuId { get; init; }

        public required string Title { get; init; }

        public required string Category { get; init; }

        public required string Description { get; init; }

        public int UnitNetPence { get; init; }

        public required string Currency { get; init; }

        public required string ImageUrl { get; init; }

        public IReadOnlyList<string> ImageUrls { get; init; } = [];

        public required string QrType { get; init; }

        public bool? IsPlanIncluded { get; init; }

        public string? PopularBadge { get; init; }

        public IReadOnlyList<string> Tags { get; init; } = [];

        public required string Material { get; init; }

        public required string Dimensions { get; init; }

        public int MinOrderQty { get; init; }

        public IReadOnlyList<ShopMaterialPackRow> Packs { get; init; } = [];

        public int DeliveryNetPence { get; init; }

        public required string CatalogVersion { get; init; }

        public bool MintOnShopFulfilment { get; init; }

        public string AboutBody { get; init; } = string.Empty;

        public IReadOnlyList<ShopMaterialSpecificationRow> Specifications
        {
            get;
            init;
        } = [];
    }
}
