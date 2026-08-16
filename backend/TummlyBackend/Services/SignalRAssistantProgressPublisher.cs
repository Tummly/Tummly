using Microsoft.AspNetCore.SignalR;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Hubs;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class SignalRAssistantProgressPublisher
        : IAssistantProgressPublisher
    {
        public const string TurnProgressEvent = "TurnProgress";

        private readonly IHubContext<AssistantHub> _hub;

        public SignalRAssistantProgressPublisher(
            IHubContext<AssistantHub> hub
        )
        {
            _hub = hub;
        }

        public Task PublishAsync(
            int userId,
            int conversationId,
            string step,
            CancellationToken cancellationToken = default
        )
        {
            return _hub.Clients
                .User(userId.ToString())
                .SendAsync(
                    TurnProgressEvent,
                    new AssistantTurnProgressDto
                    {
                        ConversationId = conversationId,
                        Step = step,
                    },
                    cancellationToken
                );
        }
    }
}
