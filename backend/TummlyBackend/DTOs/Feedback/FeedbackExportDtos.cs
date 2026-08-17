namespace TummlyBackend.DTOs.Feedback
{
    public sealed class FeedbackExportQuery
    {
        public int LocationId { get; init; }

        public string LocationName { get; init; } = string.Empty;

        public DateTime FromUtc { get; init; }

        public DateTime ToUtc { get; init; }

        /// <summary>
        /// <c>current</c> = tab ∧ search ∧ filters ∧ sort ∧ header range;
        /// <c>all-in-period</c> = header range only.
        /// </summary>
        public string Scope { get; init; } = "current";

        /// <summary>
        /// <c>xlsx</c> (default) or <c>csv</c>.
        /// </summary>
        public string Format { get; init; } = "xlsx";

        public bool IncludeGuestContact { get; init; }

        public int? FeedbackId { get; init; }

        public string Tab { get; init; } = "all";

        public string? Q { get; init; }

        public string[]? Sentiment { get; init; }

        public string[]? DetectedTags { get; init; }

        public string[]? QrSource { get; init; }

        public string[]? Contact { get; init; }

        public string[]? WorkflowStatus { get; init; }

        public string? DatePreset { get; init; }

        public DateTime? DateFrom { get; init; }

        public DateTime? DateTo { get; init; }

        public string Sort { get; init; } = "newest-submitted";

        public int UtcOffsetMinutes { get; init; }
    }

    public sealed class FeedbackExportResult
    {
        public required string FileName { get; init; }

        public required string ContentType { get; init; }

        public required byte[] Content { get; init; }
    }
}
