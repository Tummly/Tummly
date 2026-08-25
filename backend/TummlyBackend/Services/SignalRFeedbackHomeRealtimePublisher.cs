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
        private readonly IRestaurantPermissionHelper _permissions;

        public SignalRFeedbackHomeRealtimePublisher(
            IHubContext<FeedbackHomeHub> hub,
            IRestaurantPermissionHelper permissions
        )
        {
            _hub = hub;
            _permissions = permissions;
        }

        public async Task PublishClassificationTerminalAsync(
            int userId,
            int feedbackId,
            int locationId
        )
        {
            var decision = await _permissions.AuthorizeLocationForUserAsync(
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
