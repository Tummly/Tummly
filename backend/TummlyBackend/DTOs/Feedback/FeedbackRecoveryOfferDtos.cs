namespace TummlyBackend.DTOs.Feedback
{
    public sealed class FeedbackRecoveryOfferPayloadDto
    {
        public string OfferType { get; set; }
            = string.Empty;

        public string Title { get; set; }
            = string.Empty;

        public string Description { get; set; }
            = string.Empty;

        public string Validity { get; set; }
            = string.Empty;

        /// <summary>ISO date (yyyy-MM-dd) when validity is choose_expiry_date.</summary>
        public string? ExpiryDate { get; set; }

        public decimal? DiscountPercentage { get; set; }

        public decimal? DiscountAmount { get; set; }

        public string? FreeItemText { get; set; }

        public string? PurchaseRequirement { get; set; }

        public decimal? MinimumSpend { get; set; }

        public string? AdditionalExclusions { get; set; }

        public string? ReplacementItemText { get; set; }

        public string? StaffInstructions { get; set; }
    }

    public sealed class SendAndIssueFeedbackRecoveryOfferRequest
    {
        public string Channel { get; set; }
            = string.Empty;

        public string? Subject { get; set; }

        public string Body { get; set; }
            = string.Empty;

        public string Intent { get; set; }
            = string.Empty;

        public string? Purpose { get; set; }

        public string? Tone { get; set; }

        public string? IncludeNotes { get; set; }

        public FeedbackRecoveryOfferPayloadDto Offer { get; set; }
            = new();
    }

    public sealed class FeedbackRecoveryOfferItemDto
    {
        public int Id { get; init; }

        public string OfferType { get; init; }
            = string.Empty;

        public string Title { get; init; }
            = string.Empty;

        public string Description { get; init; }
            = string.Empty;

        public string Validity { get; init; }
            = string.Empty;

        public DateTime ExpiryAt { get; init; }

        public decimal? DiscountPercentage { get; init; }

        public decimal? DiscountAmount { get; init; }

        public string? FreeItemText { get; init; }

        public string? PurchaseRequirement { get; init; }

        public decimal? MinimumSpend { get; init; }

        public string? AdditionalExclusions { get; init; }

        public string? ReplacementItemText { get; init; }

        public string RedemptionCode { get; init; }
            = string.Empty;

        public string? StaffInstructions { get; init; }

        public string Intent { get; init; }
            = string.Empty;

        public string AuthorDisplayName { get; init; }
            = string.Empty;

        public DateTime CreatedAt { get; init; }
    }

    public sealed class SendAndIssueFeedbackRecoveryOfferResultDto
    {
        public string WorkflowStatus { get; init; }
            = string.Empty;

        public bool NeedsAttention { get; init; }

        public FeedbackGuestResponseItemDto GuestResponse { get; init; }
            = null!;

        public FeedbackRecoveryOfferItemDto RecoveryOffer { get; init; }
            = null!;
    }
}
