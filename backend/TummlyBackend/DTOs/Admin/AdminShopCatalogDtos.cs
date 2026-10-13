using TummlyBackend.Helpers;

namespace TummlyBackend.DTOs.Admin
{
    public sealed class AdminShopCatalogTagDto
    {
        public int Id { get; init; }

        public required string Slug { get; init; }

        public required string Label { get; init; }
    }

    public sealed class AdminShopCatalogImageDto
    {
        public int Id { get; init; }

        public required string Url { get; init; }

        public int SortOrder { get; init; }
    }

    public sealed class AdminShopCatalogSkuDto
    {
        public required string SkuId { get; init; }

        public required string Title { get; init; }

        public required string Category { get; init; }

        public required string Description { get; init; }

        public required string Material { get; init; }

        public required string Dimensions { get; init; }

        public required string QrType { get; init; }

        public int UnitNetPence { get; init; }

        public int MinOrderQty { get; init; }

        public required string Currency { get; init; }

        public bool MintOnShopFulfilment { get; init; }

        public required string Status { get; init; }

        public string AboutBody { get; init; } = string.Empty;

        public IReadOnlyList<ShopMaterialSpecificationRow> Specifications
        {
            get;
            init;
        } = [];

        public IReadOnlyList<AdminShopCatalogTagDto> Tags { get; init; } = [];

        public IReadOnlyList<AdminShopCatalogImageDto> Images { get; init; } =
            [];

        public DateTime? PublishedAtUtc { get; init; }

        public DateTime UpdatedAtUtc { get; init; }
    }

    public sealed class AdminShopCatalogSkuWriteRequest
    {
        public string? SkuId { get; init; }

        public string? Title { get; init; }

        public string? Category { get; init; }

        public string? Description { get; init; }

        public string? Material { get; init; }

        public string? Dimensions { get; init; }

        public string? QrType { get; init; }

        public int? UnitNetPence { get; init; }

        public int? MinOrderQty { get; init; }

        public string? Currency { get; init; }

        public bool? MintOnShopFulfilment { get; init; }

        public string? AboutBody { get; init; }

        public List<ShopMaterialSpecificationRow>? Specifications { get; init; }

        public List<int>? TagIds { get; init; }
    }

    public sealed class AdminShopCatalogCreateTagRequest
    {
        public required string Label { get; init; }
    }

    public sealed class AdminShopCatalogReorderImagesRequest
    {
        public required List<int> ImageIds { get; init; }
    }
}
