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
                CreditTopUpRequestDto request
            );

        Task<ExtraLocationResultDto?> AddExtraGroupLocationAsync(
            int userId,
            int restaurantId
        );

        Task<ExtraLocationResultDto?> RemoveExtraGroupLocationAsync(
            int userId,
            int restaurantId
        );

        Task<CancelPlanResultDto?> CancelPlanAsync(
            int userId,
            int restaurantId
        );

        Task<BillingActivityListDto?> GetActivityAsync(
            int restaurantId,
            int skip,
            int take
        );
    }
}
