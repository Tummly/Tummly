namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Verifies and durably accepts Revolut webhooks without running payment
    /// side effects on the HTTP request lifetime.
    /// </summary>
    public interface IRevolutWebhookReceiver
    {
        Task<RevolutWebhookHandleResult> ReceiveAsync(
            string rawBody,
            string? signatureHeader,
            string? requestTimestamp,
            CancellationToken cancellationToken = default
        );
    }
}
