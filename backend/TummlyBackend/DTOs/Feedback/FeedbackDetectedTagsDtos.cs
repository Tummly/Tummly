namespace TummlyBackend.DTOs.Feedback
{
    public sealed class UpdateFeedbackDetectedTagsRequest
    {
        public List<string>? DetectedTags { get; set; }

        /// <summary>
        /// Required when Feedback ClassificationStatus is Failed.
        /// Must be omitted when Succeeded (tags-only replace).
        /// </summary>
        public string? Sentiment { get; set; }
    }

    public sealed class UpdateFeedbackDetectedTagsResultDto
    {
        public string ClassificationStatus { get; init; }
            = string.Empty;

        public string? Sentiment { get; init; }

        public IReadOnlyList<string>? DetectedTags { get; init; }

        public bool NeedsAttention { get; init; }

        public DateTime? ClassifiedAt { get; init; }

        public FeedbackActivityEventDto? ActivityEvent { get; init; }
    }
}
