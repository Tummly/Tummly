namespace TummlyBackend.DTOs.Feedback
{
    public sealed class FeedbackClassificationCorrectionItemDto
    {
        public int Id { get; init; }

        public string FromSentiment { get; init; }
            = string.Empty;

        public string ToSentiment { get; init; }
            = string.Empty;

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
    }
}
