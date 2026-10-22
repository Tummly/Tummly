using TummlyBackend.Helpers;

namespace TummlyBackend.DTOs.Admin
{
    public sealed class AdminShopInterestProductImageDto
    {
        public int Id { get; init; }

        public required string Url { get; init; }

        public int SortOrder { get; init; }
    }

    public sealed class AdminShopInterestProductDto
    {
        public required string ProductKey { get; init; }

        public required string Title { get; init; }

        public required string Description { get; init; }

        public required string Material { get; init; }

        public required string Dimensions { get; init; }

        public bool IsActive { get; init; }

        public string? LinkedSkuId { get; init; }

        public int RegistrationCount { get; init; }

        public IReadOnlyList<AdminShopInterestProductImageDto> Images
        {
            get;
            init;
        } = [];

        public DateTime UpdatedAtUtc { get; init; }
    }

    public sealed class AdminShopInterestProductWriteRequest
    {
        public string? Title { get; init; }

        public string? Description { get; init; }

        public string? Material { get; init; }

        public string? Dimensions { get; init; }
    }

    public sealed class AdminShopInterestPromoteRequest
    {
        public int PlacementRecipeId { get; init; }

        public string? Category { get; init; }

        public List<ShopMaterialPackRow>? Packs { get; init; }

        public int? DeliveryNetPence { get; init; }

        public string? SupplierName { get; init; }

        public int? InternalCostNetPence { get; init; }

        public bool? MintOnShopFulfilment { get; init; }

        public string? AboutBody { get; init; }

        public List<ShopMaterialSpecificationRow>? Specifications { get; init; }

        public List<int>? TagIds { get; init; }
    }
}
