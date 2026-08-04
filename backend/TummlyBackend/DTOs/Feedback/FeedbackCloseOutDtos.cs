namespace TummlyBackend.DTOs.Feedback
{
    public sealed class CloseOutFeedbackRequest
    {
        public string Intent { get; set; }
            = string.Empty;

        public string Reason { get; set; }
            = string.Empty;

        /// <summary>
        /// Required when reason is other; must be omitted for other reasons.
        /// </summary>
        public string? NoteBody { get; set; }
    }

    public sealed class FeedbackCloseOutItemDto
    {
        public int Id { get; init; }

        public string Intent { get; init; }
            = string.Empty;

        public string Reason { get; init; }
            = string.Empty;

        public int WorkflowStatusChangeId { get; init; }

        public int? InternalNoteId { get; init; }

        public string AuthorDisplayName { get; init; }
            = string.Empty;

        public DateTime CreatedAt { get; init; }

        public string FromWorkflowStatus { get; init; }
            = string.Empty;

        public string ToWorkflowStatus { get; init; }
            = string.Empty;
    }

    public sealed class FeedbackCloseOutResultDto
    {
        public string WorkflowStatus { get; init; }
            = string.Empty;

        public bool NeedsAttention { get; init; }

        public FeedbackCloseOutItemDto CloseOut { get; init; }
            = null!;

        public FeedbackInternalNoteItemDto? Note { get; init; }
    }
}
