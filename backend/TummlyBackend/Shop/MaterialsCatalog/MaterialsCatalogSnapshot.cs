namespace TummlyBackend.Shop.MaterialsCatalog
{
    public sealed class MaterialsCatalogSnapshot
    {
        public required string Id { get; init; }

        public required IReadOnlyList<MaterialsCatalogSku> Skus { get; init; }
    }

    public sealed class MaterialsCatalogSku
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

        public required string ImageUrl { get; init; }

        public bool IsPlanIncluded { get; init; }

        public string? PopularBadge { get; init; }

        public bool MintOnShopFulfilment { get; init; }
    }
}
