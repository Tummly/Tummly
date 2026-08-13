using TummlyBackend.DTOs.Assistant;
using TummlyBackend.DTOs.OwnedLocation;

namespace TummlyBackend.Interfaces
{
    public interface IAssistantConversationService
    {
        Task<AssistantTurnOutcome> SendTurnAsync(
            int ownerUserId,
            SendAssistantTurnRequest request,
            CancellationToken cancellationToken = default
        );

        Task<AssistantTurnOutcome> GetAsync(
            int ownerUserId,
            int conversationId,
            CancellationToken cancellationToken = default
        );

        Task<AssistantTurnOutcome> ApplyScopeAsync(
            int ownerUserId,
            int conversationId,
            ApplyAssistantScopeRequest request,
            CancellationToken cancellationToken = default
        );

        Task<AssistantTurnOutcome> RetryTurnAsync(
            int ownerUserId,
            int conversationId,
            CancellationToken cancellationToken = default
        );

        Task<AssistantListOutcome> ListAsync(
            int ownerUserId,
            bool archived,
            CancellationToken cancellationToken = default
        );

        Task<AssistantTurnOutcome> SetArchivedAsync(
            int ownerUserId,
            int conversationId,
            bool archived,
            CancellationToken cancellationToken = default
        );

        Task<AssistantDeleteOutcome> DeleteAsync(
            int ownerUserId,
            int conversationId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Hard-deletes every Assistant conversation and message for the
        /// Operator user. Removes messages first so InMemory tests match
        /// SQL cascade. Used when the Operator user record is removed.
        /// </summary>
        Task DeleteAllForOwnerAsync(
            int ownerUserId,
            CancellationToken cancellationToken = default
        );
    }

    public abstract record AssistantListOutcome
    {
        public sealed record Ok(
            IReadOnlyList<AssistantConversationListItemDto> Conversations
        ) : AssistantListOutcome;
    }

    public abstract record AssistantDeleteOutcome
    {
        public sealed record Ok() : AssistantDeleteOutcome;

        public sealed record NotFound() : AssistantDeleteOutcome;
    }

    public abstract record AssistantTurnOutcome
    {
        public sealed record Ok(AssistantConversationDto Conversation)
            : AssistantTurnOutcome;

        public sealed record Invalid(string Message) : AssistantTurnOutcome;

        public sealed record NotFound() : AssistantTurnOutcome;

        public sealed record LocationDenied(OwnedLocationResult Location)
            : AssistantTurnOutcome;
    }
}
