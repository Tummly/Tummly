using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class NullFeedbackHomeRealtimePublisher
        : IFeedbackHomeRealtimePublisher
    {
        public Task PublishClassificationTerminalAsync(
            int userId,
            int feedbackId,
            int locationId
        ) => Task.CompletedTask;
    }
}
