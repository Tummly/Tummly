namespace TummlyBackend.DTOs.Feedback
{
    public sealed class RespondAndRecordInternalActionRequest
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

        public string Category { get; set; }
            = string.Empty;

        public string Note { get; set; }
            = string.Empty;
    }

    public sealed class RespondAndRecordInternalActionResultDto
    {
        public string WorkflowStatus { get; init; }
            = string.Empty;

        public bool NeedsAttention { get; init; }

        public FeedbackGuestResponseItemDto GuestResponse { get; init; }
            = null!;

        public FeedbackInternalActionItemDto InternalAction { get; init; }
            = null!;
    }
}
