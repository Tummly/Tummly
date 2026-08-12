namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Durable Recovery offer attach (Feedback.RecoveryOfferId → CatalogOffers).
    /// Attach alone never creates OfferIssues.
    /// </summary>
    public interface IFeedbackRecoveryOfferAttachService
    {
        Task<int?> GetAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Sets or clears the Recovery offer attach. Pass null to clear.
        /// Non-null offerId must be Active for the feedback location.
        /// </summary>
        Task SetAsync(
            int feedbackId,
            int? offerId,
            CancellationToken cancellationToken = default
        );
    }
}
