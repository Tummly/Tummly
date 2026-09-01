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
            PlanChangeRequestDto request,
            string? idempotencyKey = null
        );

        Task<(bool Success, string? ErrorCode)?> ClearScheduledChangeAsync(
            int userId,
            int restaurantId
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

        Task<(CreditTopUpConfirmDto? Response, int StatusCode, string? ErrorMessage)>
            ConfirmCreditTopUpAsync(
                int userId,
                int restaurantId,
                bool actorCanManage,
                CreditTopUpRequestDto request
            );

        Task<(CreditTopUpPayDto? Response, int StatusCode, string? ErrorMessage)>
            PayCreditTopUpAsync(
                int userId,
                int restaurantId,
                bool actorCanManage,
                CreditTopUpRequestDto request,
                string? idempotencyKey = null
            );

        Task<CancelPlanResultDto?> CancelPlanAsync(
            int userId,
            int restaurantId,
            CancelPlanRequestDto request
        );

        Task<BillingActivityListDto?> GetActivityAsync(
            int restaurantId,
            int skip,
            int take
        );
    }
}
