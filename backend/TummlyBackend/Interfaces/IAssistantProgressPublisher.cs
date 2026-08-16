namespace TummlyBackend.Interfaces
{
    public interface IAssistantProgressPublisher
    {
        Task PublishAsync(
            int userId,
            int conversationId,
            string step,
            CancellationToken cancellationToken = default
        );
    }
}
