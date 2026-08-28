using TummlyBackend.DTOs.BillingCredits;

namespace TummlyBackend.Interfaces
{
    public interface IBillingCreditsService
    {
        Task<BillingCreditsPageDto?> GetPageAsync(
            int userId,
            int restaurantId,
            bool actorCanManage
        );

        Task<CreditsUsageSnapshotDto?> GetUsageAsync(int restaurantId);

        Task<(byte[] Content, string FileName)?> GetInvoicePdfAsync(
            int restaurantId,
            string invoiceNo
        );

        Task<PaymentMethodUpdateSessionDto?> CreatePaymentMethodUpdateSessionAsync(
            int restaurantId
        );

        Task<PlanChangeResultDto?> SubmitPlanChangeAsync(
            int userId,
            int restaurantId,
            PlanChangeRequestDto request
        );

        Task<(
            UpdateBillingContactsResponseDto? Response,
            string? Error,
            int StatusCode
        )> UpdateBillingContactsAsync(
            int actorUserId,
            int restaurantId,
            UpdateBillingContactsRequest request
        );

        Task<BillingActivityListDto?> GetActivityAsync(
            int restaurantId,
            int page,
            int pageSize
        );
    }
}
