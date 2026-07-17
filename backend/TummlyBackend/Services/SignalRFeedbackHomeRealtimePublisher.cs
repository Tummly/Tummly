using Microsoft.AspNetCore.SignalR;
using TummlyBackend.DTOs.Feedback;
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

        public SignalRFeedbackHomeRealtimePublisher(
            IHubContext<FeedbackHomeHub> hub
        )
        {
            _hub = hub;
        }

        public Task PublishClassificationTerminalAsync(
            int userId,
            int feedbackId,
            int locationId
        )
        {
            return _hub.Clients
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
