using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class NullAssistantProgressPublisher
        : IAssistantProgressPublisher
    {
        public Task PublishAsync(
            int userId,
            int conversationId,
            string step,
            CancellationToken cancellationToken = default
        )
        {
            return Task.CompletedTask;
        }
    }
}
