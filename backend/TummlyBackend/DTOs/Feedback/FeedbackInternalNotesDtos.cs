namespace TummlyBackend.DTOs.Feedback
{
    public sealed class CreateFeedbackInternalNoteRequest
    {
        public string Body { get; set; }
            = string.Empty;
    }

    public sealed class FeedbackInternalNoteItemDto
    {
        public int Id { get; init; }

        public string Body { get; init; }
            = string.Empty;

        public string AuthorDisplayName { get; init; }
            = string.Empty;

        public DateTime CreatedAt { get; init; }
    }
}
