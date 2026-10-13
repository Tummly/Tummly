using TummlyBackend.DTOs.Admin;
using TummlyBackend.Helpers;

namespace TummlyBackend.Interfaces
{
    public interface IAdminShopCatalogService
    {
        Task<IReadOnlyList<AdminPlacementRecipeDto>> ListPlacementRecipesAsync(
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<AdminShopCatalogSkuDto>> ListSkusAsync(
            string? statusFilter = null,
            CancellationToken cancellationToken = default
        );

        Task<AdminShopCatalogSkuDto?> GetSkuAsync(
            string skuId,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopCatalogSkuDto? Sku, string? Error)> CreateSkuAsync(
            AdminShopCatalogSkuWriteRequest request,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopCatalogSkuDto? Sku, string? Error)> UpdateSkuAsync(
            string skuId,
            AdminShopCatalogSkuWriteRequest request,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopCatalogSkuDto? Sku, string? Error)> PublishAsync(
            string skuId,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopCatalogSkuDto? Sku, string? Error)> UnpublishAsync(
            string skuId,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopCatalogSkuDto? Sku, string? Error)> DeleteAsync(
            string skuId,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<AdminShopCatalogTagDto>> ListTagsAsync(
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopCatalogTagDto? Tag, string? Error)> CreateTagAsync(
            string label,
            CancellationToken cancellationToken = default
        );

        Task<(bool Ok, string? Error)> DeleteTagAsync(
            int tagId,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopCatalogImageDto? Image, string? Error)> UploadImageAsync(
            string skuId,
            IFormFile file,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopCatalogSkuDto? Sku, string? Error)> ReorderImagesAsync(
            string skuId,
            IReadOnlyList<int> imageIds,
            CancellationToken cancellationToken = default
        );

        Task<(bool Ok, string? Error)> DeleteImageAsync(
            string skuId,
            int imageId,
            CancellationToken cancellationToken = default
        );
    }
}
