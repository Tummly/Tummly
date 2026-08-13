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
