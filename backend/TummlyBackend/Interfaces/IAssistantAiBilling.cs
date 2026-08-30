using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Interfaces
{
    public interface IAssistantAiBilling
    {
        Task<AssistantConversationDto?> TryGetCachedOutcomeAsync(
            int restaurantId,
            string idempotencyKey,
            CancellationToken cancellationToken = default
        );

        Task<int?> TryResolveRestaurantIdAsync(
            int locationId,
            CancellationToken cancellationToken = default
        );

        Task<int> GetAiRemainingAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        );

        Task<CreditLedgerWriteResult> ConsumeCompletedAnswerAsync(
            int restaurantId,
            int locationId,
            CancellationToken cancellationToken = default
        );

        Task StoreOutcomeAsync(
            int restaurantId,
            string idempotencyKey,
            AssistantConversationDto conversation,
            CancellationToken cancellationToken = default
        );
    }
}
