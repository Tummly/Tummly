namespace TummlyBackend.Interfaces
{
    public interface IBilledAiActionCoordinator
    {
        Task<BilledAiActionResult> ExecuteAsync(
            BilledAiActionRequest request,
            Func<CancellationToken, Task<BilledAiGenerationResult>> generateAsync,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class BilledAiActionRequest
    {
        public int RestaurantId { get; init; }

        public int LocationId { get; init; }

        public string? IdempotencyKey { get; init; }

        public string PackKey { get; init; } = string.Empty;
    }

    public sealed class BilledAiDraftPayload
    {
        public string Body { get; init; } = string.Empty;

        public string? Subject { get; init; }

        public string Channel { get; init; } = string.Empty;
    }

    public abstract record BilledAiGenerationResult
    {
        public sealed record Ok(BilledAiDraftPayload Payload)
            : BilledAiGenerationResult;

        public sealed record Failed(string Message, bool Retryable)
            : BilledAiGenerationResult;

        public sealed record NotFound(string Message)
            : BilledAiGenerationResult;
    }

    public abstract record BilledAiActionResult
    {
        public sealed record Cached(BilledAiDraftPayload Payload)
            : BilledAiActionResult;

        public sealed record Succeeded(BilledAiDraftPayload Payload)
            : BilledAiActionResult;

        public sealed record HardStopped(int Remaining)
            : BilledAiActionResult;

        public sealed record ConsumeFailed(string Code, int Remaining)
            : BilledAiActionResult;

        public sealed record ProviderFailed(string Message, bool Retryable)
            : BilledAiActionResult;

        public sealed record ResourceNotFound(string Message)
            : BilledAiActionResult;

        public sealed record IdempotencyKeyRequired()
            : BilledAiActionResult;
    }
}
