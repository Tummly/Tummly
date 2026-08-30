namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Revolut webhook verify → retrieve gate → unique claim store
    /// (ticket 15 / lock 04).
    /// </summary>
    public interface IRevolutWebhookService
    {
        Task<RevolutWebhookHandleResult> HandleAsync(
            string rawBody,
            string? signatureHeader,
            string? requestTimestamp,
            CancellationToken cancellationToken = default
        );
    }

    public enum RevolutWebhookHandleStatus
    {
        Accepted,
        Replay,
        BadSignature,
        RetryLater,
    }

    public sealed record RevolutWebhookHandleResult(
        RevolutWebhookHandleStatus Status
    );
}
