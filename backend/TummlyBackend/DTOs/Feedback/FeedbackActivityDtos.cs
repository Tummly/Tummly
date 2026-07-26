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
    }
}
