namespace TummlyBackend.Interfaces
{
    public interface IGuestTagBackfillService
    {
        /// <summary>
        /// One-shot: Succeeded Feedbacks with LocationGuestId → ensure catalog
        /// + union membership. Idempotent; skips null guest FK; empty tags no-op.
        /// </summary>
        Task BackfillAsync(CancellationToken cancellationToken = default);
    }
}
