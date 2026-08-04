namespace TummlyBackend.DTOs.Feedback
{
    public sealed class RecordFeedbackInternalActionRequest
    {
        public string Category { get; set; }
            = string.Empty;

        public string Note { get; set; }
            = string.Empty;

        public string Intent { get; set; }
            = string.Empty;
    }

    public sealed class FeedbackInternalActionItemDto
    {
        public int Id { get; init; }

        public string Category { get; init; }
            = string.Empty;

        public string CategoryLabel { get; init; }
            = string.Empty;

        public string Note { get; init; }
            = string.Empty;

        public string Intent { get; init; }
            = string.Empty;

        public string AuthorDisplayName { get; init; }
            = string.Empty;

        public DateTime CreatedAt { get; init; }
    }

    public sealed class RecordFeedbackInternalActionResultDto
    {
        public string WorkflowStatus { get; init; }
            = string.Empty;

        public bool NeedsAttention { get; init; }

        public FeedbackInternalActionItemDto InternalAction { get; init; }
            = null!;
    }
}
