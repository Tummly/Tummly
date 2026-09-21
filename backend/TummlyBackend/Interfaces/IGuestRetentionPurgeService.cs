namespace TummlyBackend.Interfaces
{
    public interface IGuestRetentionPurgeService
    {
        Task<GuestRetentionPurgeBatchResult> ProcessOnceAsync(
            DateTime nowUtc,
            CancellationToken cancellationToken = default
        );
    }

    public sealed record GuestRetentionPurgeBatchResult(
        int EligibleFound,
        int Purged,
        int Failed,
        int GuestsDeleted
    );
}
