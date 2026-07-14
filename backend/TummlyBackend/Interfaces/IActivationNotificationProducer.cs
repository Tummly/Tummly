namespace TummlyBackend.Interfaces
{
    public interface IActivationNotificationProducer
    {
        Task<ActivationNotificationBatchResult> ProcessAsync(
            DateTime utcNow,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class ActivationNotificationBatchResult
    {
        public int Produced { get; init; }

        public int Failed { get; init; }
    }
}
