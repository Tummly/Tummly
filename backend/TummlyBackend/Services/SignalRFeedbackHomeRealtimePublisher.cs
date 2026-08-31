using Microsoft.AspNetCore.SignalR;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Hubs;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class SignalRFeedbackHomeRealtimePublisher
        : IFeedbackHomeRealtimePublisher
    {
        public const string ClassificationTerminalEvent =
            "ClassificationTerminal";

        private readonly IHubContext<FeedbackHomeHub> _hub;
        private readonly IServiceScopeFactory _scopeFactory;

        public SignalRFeedbackHomeRealtimePublisher(
            IHubContext<FeedbackHomeHub> hub,
            IServiceScopeFactory scopeFactory
        )
        {
            _hub = hub;
            _scopeFactory = scopeFactory;
        }

        public async Task PublishClassificationTerminalAsync(
            int userId,
            int feedbackId,
            int locationId
        )
        {
            using var scope = _scopeFactory.CreateScope();
            var permissions = scope.ServiceProvider
                .GetRequiredService<IRestaurantPermissionHelper>();

            var decision = await permissions.AuthorizeLocationForUserAsync(
                userId,
                OperatorAreaIds.Feedback,
                PermissionLevel.View,
                locationId
            );
            if (decision.Status != RestaurantPermissionStatus.Allowed)
            {
                return;
            }

            await _hub.Clients
                .User(userId.ToString())
                .SendAsync(
                    ClassificationTerminalEvent,
                    new ClassificationTerminalSignalDto
                    {
                        FeedbackId = feedbackId,
                        LocationId = locationId,
                    }
                );
        }
    }
}
