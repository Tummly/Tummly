using Microsoft.AspNetCore.SignalR;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Helpers;
using TummlyBackend.Hubs;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class SignalRAssistantProgressPublisher
        : IAssistantProgressPublisher
    {
        public const string TurnProgressEvent = "TurnProgress";

        private readonly IHubContext<AssistantHub> _hub;
        private readonly IRestaurantPermissionHelper _permissions;

        public SignalRAssistantProgressPublisher(
            IHubContext<AssistantHub> hub,
            IRestaurantPermissionHelper permissions
        )
        {
            _hub = hub;
            _permissions = permissions;
        }

        public async Task PublishAsync(
            int userId,
            int conversationId,
            string step,
            CancellationToken cancellationToken = default
        )
        {
            var decision = await _permissions.AuthorizeUserAsync(
                userId,
                OperatorAreaIds.AiAssistant,
                PermissionLevel.View
            );
            if (decision.Status != RestaurantPermissionStatus.Allowed)
            {
                return;
            }

            await _hub.Clients
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
