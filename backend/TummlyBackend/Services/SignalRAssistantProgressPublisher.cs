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
        private readonly IServiceScopeFactory _scopeFactory;

        public SignalRAssistantProgressPublisher(
            IHubContext<AssistantHub> hub,
            IServiceScopeFactory scopeFactory
        )
        {
            _hub = hub;
            _scopeFactory = scopeFactory;
        }

        public async Task PublishAsync(
            int userId,
            int conversationId,
            string step,
            CancellationToken cancellationToken = default
        )
        {
            using var scope = _scopeFactory.CreateScope();
            var permissions = scope.ServiceProvider
                .GetRequiredService<IRestaurantPermissionHelper>();

            var decision = await permissions.AuthorizeUserAsync(
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
