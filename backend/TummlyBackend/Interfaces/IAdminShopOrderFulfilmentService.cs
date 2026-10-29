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
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        );

        Task<AdminShopOrderFulfilmentResult> MarkPreparationStartedAsync(
            Guid orderId,
            AdminShopFulfilmentActionRequest request,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        );

        Task<AdminShopOrderFulfilmentResult> MarkProductionStartedAsync(
            Guid orderId,
            AdminShopFulfilmentActionRequest request,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        );

        Task<AdminShopOrderFulfilmentResult> MarkDispatchedAsync(
            Guid orderId,
            AdminShopDispatchRequest request,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        );

        Task<AdminShopOrderFulfilmentResult> MarkDeliveredAsync(
            Guid orderId,
            AdminShopFulfilmentActionRequest request,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        );

        Task<AdminShopOrderFulfilmentResult> RecordDispatchEvidenceAsync(
            Guid orderId,
            AdminShopDispatchEvidenceRequest request,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        );

        Task<AdminShopOrderFulfilmentResult> CorrectStatusAsync(
            Guid orderId,
            AdminShopStatusCorrectionRequest request,
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
