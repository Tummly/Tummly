using TummlyBackend.DTOs.Admin;

namespace TummlyBackend.Interfaces
{
    public interface IAdminShopInterestProductService
    {
        Task<IReadOnlyList<AdminShopInterestProductDto>> ListProductsAsync(
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopInterestProductDto? Product, string? Error)> UpdateProductAsync(
            string productKey,
            AdminShopInterestProductWriteRequest request,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopInterestProductImageDto? Image, string? Error)> UploadImageAsync(
            string productKey,
            IFormFile file,
            CancellationToken cancellationToken = default
        );

        Task<(bool Ok, string? Error)> DeleteImageAsync(
            string productKey,
            int imageId,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopInterestProductDto? Product, string? Error)> PromoteAsync(
            string productKey,
            AdminShopInterestPromoteRequest request,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopInterestProductDto? Product, string? Error)> DemoteByProductKeyAsync(
            string productKey,
            CancellationToken cancellationToken = default
        );

        Task<(AdminShopInterestProductDto? Product, string? Error)> DemoteBySkuIdAsync(
            string skuId,
            CancellationToken cancellationToken = default
        );
    }
}
