using TummlyBackend.DTOs.Admin;

namespace TummlyBackend.Interfaces
{
    public interface IAdminShopOrderFulfilmentService
    {
        Task<AdminShopOrderListResponseDto> GetListAsync(
            AdminShopOrdersListQuery query,
            CancellationToken cancellationToken = default
        );

        Task<AdminShopOrderFulfilmentResult> UpdateFulfilmentAsync(
            Guid orderId,
            AdminShopOrderFulfilmentPatchDto patch,
            CancellationToken cancellationToken = default
        );

        Task<AdminShopOrdersExportResult> ExportCsvAsync(
            AdminShopOrdersListQuery query,
            CancellationToken cancellationToken = default
        );
    }
}
