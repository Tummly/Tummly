namespace TummlyBackend.Models
{
    public sealed record FeedbackRecoveryDraftConfirmedOffer(
        string OfferType,
        string Title,
        string Description,
        string Validity,
        string? ExpiryDate,
        decimal? DiscountPercentage,
        decimal? DiscountAmount,
        string? FreeItemText,
        string? PurchaseRequirement,
        decimal? MinimumSpend,
        string? AdditionalExclusions,
        string? ReplacementItemText
    );

    /// <summary>
    /// Inputs for the recovery guest-message draft adapter.
    /// Must never include raw email/phone.
    /// </summary>
    public sealed record FeedbackRecoveryDraftInput(
        string FeedbackComment,
        string? Sentiment,
        IReadOnlyList<string> IssueTags,
        string GuestDisplayName,
        string LocationName,
        string Channel,
        string Purpose,
        string Tone,
        string? IncludeNotes,
        string Mode,
        string? CurrentBody,
        string? CurrentSubject,
        FeedbackRecoveryDraftConfirmedOffer? ConfirmedOffer = null
    );

    /// <summary>
    /// Result of drafting a guest-response message via the draft provider.
    /// </summary>
    public abstract record FeedbackRecoveryDraftResult
    {
        private FeedbackRecoveryDraftResult()
        {
        }

        public sealed record Succeeded(
            string Body,
            string? Subject,
            string Channel
        ) : FeedbackRecoveryDraftResult;

        public sealed record Failed(bool Retryable = true)
            : FeedbackRecoveryDraftResult;
    }
}
