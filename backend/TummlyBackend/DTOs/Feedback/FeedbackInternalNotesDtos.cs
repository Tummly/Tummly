namespace TummlyBackend.DTOs.Feedback
{
    public sealed class CreateFeedbackInternalNoteRequest
    {
        public string Body { get; set; }
            = string.Empty;
    }

    public sealed class UpdateFeedbackInternalNoteRequest
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

        /// <summary>Null when the note has never been edited.</summary>
        public DateTime? UpdatedAt { get; init; }
    }

    /// <summary>
    /// Facts used to derive Feedback activity history — includes soft-deleted notes.
    /// </summary>
    public sealed class FeedbackInternalNoteActivityFactDto
    {
        public int Id { get; init; }

        public string AuthorDisplayName { get; init; }
            = string.Empty;

        public DateTime CreatedAt { get; init; }

        public DateTime? DeletedAt { get; init; }

        public string? DeletedByDisplayName { get; init; }
    }

    public sealed class SoftDeleteFeedbackInternalNoteResultDto
    {
        public DateTime DeletedAt { get; init; }

        public string DeletedByDisplayName { get; init; }
            = string.Empty;
    }
}
