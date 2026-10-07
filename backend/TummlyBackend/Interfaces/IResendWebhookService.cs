namespace TummlyBackend.Interfaces
{
    public enum ResendWebhookHandleStatus
    {
        Accepted,
        BadSignature,
        Misconfigured,
    }

    /// <summary>
    /// Resend (Svix) webhooks — Campaign email.opened → engagement.
    /// </summary>
    public interface IResendWebhookService
    {
        Task<ResendWebhookHandleStatus> HandleAsync(
            string rawBody,
            string? svixId,
            string? svixTimestamp,
            string? svixSignature,
            CancellationToken cancellationToken = default
        );
    }
}
