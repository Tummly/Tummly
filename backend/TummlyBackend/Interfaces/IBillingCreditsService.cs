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

        Task<(
            UpdateBillingContactsResponseDto? Response,
            string? Error,
            int StatusCode
        )> UpdateBillingContactsAsync(
            int actorUserId,
            int restaurantId,
            UpdateBillingContactsRequest request
        );
    }
}
