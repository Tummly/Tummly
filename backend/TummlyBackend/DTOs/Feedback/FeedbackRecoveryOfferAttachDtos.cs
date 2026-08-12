namespace TummlyBackend.DTOs.Feedback
{
    public sealed class FeedbackRecoveryOfferAttachResponse
    {
        public bool Success { get; set; } = true;

        public int? OfferId { get; set; }
    }

    public sealed class SetFeedbackRecoveryOfferAttachRequest
    {
        /// <summary>Catalog Offer id, or null to clear the Recovery attach.</summary>
        public int? OfferId { get; set; }
    }
}
