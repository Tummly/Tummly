namespace TummlyBackend.DTOs.Feedback
{
    public sealed class SendFeedbackGuestResponseRequest
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
    }

    public sealed class SendGuestPreviewTestRequest
    {
        public string? Subject { get; set; }

        public string Body { get; set; }
            = string.Empty;

        /// <summary>
        /// Optional Recovery offer draft chrome for offer-wizard send test.
        /// Never creates a live Recovery offer; sample code is applied server-side.
        /// </summary>
        public GuestPreviewTestOfferDto? Offer { get; set; }
    }

    public sealed class GuestPreviewTestOfferDto
    {
        public string Title { get; set; }
            = string.Empty;

        public string Description { get; set; }
            = string.Empty;

        public string ExpiryLabel { get; set; }
            = string.Empty;
    }

    public sealed class FeedbackGuestResponseItemDto
    {
        public int Id { get; init; }

        public string Channel { get; init; }
            = string.Empty;

        public string Intent { get; init; }
            = string.Empty;

        public string MaskedDestination { get; init; }
            = string.Empty;

        public string? Subject { get; init; }

        public string Body { get; init; }
            = string.Empty;

        public string AuthorDisplayName { get; init; }
            = string.Empty;

        public DateTime CreatedAt { get; init; }
    }

    public sealed class SendFeedbackGuestResponseResultDto
    {
        public string WorkflowStatus { get; init; }
            = string.Empty;

        public bool NeedsAttention { get; init; }

        public FeedbackGuestResponseItemDto GuestResponse { get; init; }
            = null!;
    }

    public sealed class CompleteFeedbackRecoveryRequest
    {
        public string Intent { get; set; }
            = string.Empty;
    }

    public sealed class FeedbackRecoveryCompletionItemDto
    {
        public int Id { get; init; }

        public string Intent { get; init; }
            = string.Empty;

        public int WorkflowStatusChangeId { get; init; }

        public string AuthorDisplayName { get; init; }
            = string.Empty;

        public DateTime CreatedAt { get; init; }

        public string FromWorkflowStatus { get; init; }
            = string.Empty;

        public string ToWorkflowStatus { get; init; }
            = string.Empty;
    }

    public sealed class CompleteFeedbackRecoveryResultDto
    {
        public string WorkflowStatus { get; init; }
            = string.Empty;

        public bool NeedsAttention { get; init; }

        public FeedbackRecoveryCompletionItemDto Completion { get; init; }
            = null!;
    }
}
