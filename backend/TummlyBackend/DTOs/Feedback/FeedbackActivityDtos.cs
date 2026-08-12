namespace TummlyBackend.DTOs.Feedback
{
    public sealed class FeedbackClassificationCorrectionItemDto
    {
        public int Id { get; init; }

        public string FromSentiment { get; init; }
            = string.Empty;

        public string ToSentiment { get; init; }
            = string.Empty;

        public string Reason { get; init; }
            = string.Empty;

        public string? Note { get; init; }

        public string AuthorDisplayName { get; init; }
            = string.Empty;

        public DateTime CreatedAt { get; init; }
    }

    public sealed class FeedbackDetectedTagsChangeItemDto
    {
        public int Id { get; init; }

        public IReadOnlyList<string> FromDetectedTags { get; init; }
            = Array.Empty<string>();

        public IReadOnlyList<string> ToDetectedTags { get; init; }
            = Array.Empty<string>();

        public string? FromSentiment { get; init; }

        public string? ToSentiment { get; init; }

        public string AuthorDisplayName { get; init; }
            = string.Empty;

        public DateTime CreatedAt { get; init; }
    }

    public sealed class FeedbackActivityEventDto
    {
        public string Kind { get; init; }
            = string.Empty;

        public DateTime At { get; init; }

        public string? ActorDisplayName { get; init; }

        public string? FromSentiment { get; init; }

        public string? ToSentiment { get; init; }

        public IReadOnlyList<string>? FromDetectedTags { get; init; }

        public IReadOnlyList<string>? ToDetectedTags { get; init; }

        public string? FromWorkflowStatus { get; init; }

        public string? ToWorkflowStatus { get; init; }

        public string? CloseOutIntent { get; init; }

        public string? CloseOutReason { get; init; }

        public string? Channel { get; init; }

        public string? MaskedDestination { get; init; }

        public string? RecoveryIntent { get; init; }

        public string? Category { get; init; }

        public string? CategoryLabel { get; init; }

        public string? Note { get; init; }

        public string? OfferType { get; init; }

        public string? OfferTitle { get; init; }

        public string? OfferValidity { get; init; }

        public DateTime? OfferExpiryAt { get; init; }

        public string? RedemptionCode { get; init; }

        /// <summary>
        /// Catalog Offer issue only: <c>not_redeemed</c> | <c>redeemed</c>.
        /// </summary>
        public string? RedemptionStatus { get; init; }
    }
}
