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

        Task<AdminShopOrderFulfilmentResult> MarkProductionStartedAsync(
            Guid orderId,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        );

        Task<AdminShopOrderFulfilmentResult> ForceCancelAsync(
            Guid orderId,
            AdminShopForceCancelRequest request,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        );

        Task<AdminShopOrdersExportResult> ExportCsvAsync(
            AdminShopOrdersListQuery query,
            CancellationToken cancellationToken = default
        );
    }
}
