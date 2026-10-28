using System.Security.Claims;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Helpers;

namespace TummlyBackend.Interfaces
{
    public sealed record AdminShopActor(int AdminUserId, string Identity)
    {
        /// <summary>
        /// The signed-in admin, or null when the token lacks a user id or identity.
        /// </summary>
        public static AdminShopActor? FromPrincipal(ClaimsPrincipal user)
        {
            var staffIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var identity = user.FindFirst(ClaimTypes.Email)?.Value ?? user.Identity?.Name;
            if (
                string.IsNullOrEmpty(staffIdClaim)
                || !int.TryParse(staffIdClaim, out var staffId)
                || string.IsNullOrWhiteSpace(identity)
            )
            {
                return null;
            }

            return new AdminShopActor(staffId, identity);
        }
    }

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
            AdminShopActor actor,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopCatalogSkuDto? Sku, string? Error)> UpdateSkuAsync(
            string skuId,
            AdminShopCatalogSkuWriteRequest request,
            AdminShopActor actor,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopCatalogSkuDto? Sku, string? Error)> PublishAsync(
            string skuId,
            AdminShopActor actor,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopCatalogSkuDto? Sku, string? Error)> SetAvailableInShopAsync(
            string skuId,
            bool availableInShop,
            AdminShopActor actor,
            string? approvalReference = null,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopCatalogSkuDto? Sku, string? Error)> UnpublishAsync(
            string skuId,
            AdminShopActor actor,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopCatalogSkuDto? Sku, string? Error)> DeleteAsync(
            string skuId,
            AdminShopActor actor,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<AdminShopCatalogTagDto>> ListTagsAsync(
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopCatalogTagDto? Tag, string? Error)> CreateTagAsync(
            string label,
            AdminShopActor actor,
            CancellationToken cancellationToken = default
        );

        Task<(bool Ok, string? Error)> DeleteTagAsync(
            int tagId,
            AdminShopActor actor,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopCatalogImageDto? Image, string? Error)> UploadImageAsync(
            string skuId,
            IFormFile file,
            AdminShopActor actor,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopCatalogSkuDto? Sku, string? Error)> ReorderImagesAsync(
            string skuId,
            IReadOnlyList<int> imageIds,
            AdminShopActor actor,
            CancellationToken cancellationToken = default
        );

        Task<(bool Ok, string? Error)> DeleteImageAsync(
            string skuId,
            int imageId,
            AdminShopActor actor,
            CancellationToken cancellationToken = default
        );
    }
}
