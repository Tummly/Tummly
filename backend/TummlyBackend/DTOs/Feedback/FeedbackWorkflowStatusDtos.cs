namespace TummlyBackend.DTOs.Feedback
{
    public sealed class SetFeedbackWorkflowStatusDto
    {
        public string WorkflowStatus { get; set; }
            = string.Empty;
    }

    public sealed class FeedbackWorkflowStatusChangeItemDto
    {
        public int Id { get; init; }

        public string FromWorkflowStatus { get; init; }
            = string.Empty;

        public string ToWorkflowStatus { get; init; }
            = string.Empty;

        public string AuthorDisplayName { get; init; }
            = string.Empty;

        public DateTime CreatedAt { get; init; }
    }
}
