namespace TummlyBackend.Interfaces
{
    public interface ILocationGuestActivityBackfillService
    {
        /// <summary>
        /// Idempotent backfill of guest-joined, feedback, tag-applied, and
        /// terminal classification events from existing domain rows.
        /// Classification OccurredAt ≈ ClassificationClaimedAt if set, else
        /// Feedback.CreatedAt (documented approximation).
        /// </summary>
        Task BackfillAsync(CancellationToken cancellationToken = default);
    }
}
