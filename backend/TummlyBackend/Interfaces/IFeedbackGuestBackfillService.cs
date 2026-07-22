namespace TummlyBackend.Interfaces
{
    public interface IFeedbackGuestBackfillService
    {
        /// <summary>
        /// Links unlinked Feedbacks to Location Guests using live upsert rules.
        /// Idempotent — skips rows that already have a Location Guest FK.
        /// </summary>
        Task BackfillAsync(CancellationToken cancellationToken = default);
    }
}
