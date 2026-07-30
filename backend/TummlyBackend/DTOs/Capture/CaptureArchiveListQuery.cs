namespace TummlyBackend.DTOs.Capture
{
    /// <summary>
    /// Query for the Capture Archive list module (ADR-0024).
    /// </summary>
    public sealed class CaptureArchiveListQuery
    {
        public required int OwnerUserId { get; init; }

        public string? Q { get; init; }

        public int[]? LocationIds { get; init; }

        /// <summary>
        /// Wire QR type names (e.g. CounterCard). Empty/null = all types.
        /// </summary>
        public string[]? QrTypes { get; init; }

        /// <summary>
        /// any-time | today | last-7 | last-30 | this-month | previous-month | custom.
        /// Omit or any-time = no date filter.
        /// </summary>
        public string? DatePreset { get; init; }

        public DateTime? DateFrom { get; init; }

        public DateTime? DateTo { get; init; }

        /// <summary>
        /// Archived-by display-name filter. Empty/null = any archiver.
        /// </summary>
        public string[]? ArchivedBy { get; init; }

        public string Sort { get; init; } = "recently-archived";

        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 25;

        /// <summary>
        /// Minutes east of UTC for resolving datePreset (Guests-aligned).
        /// </summary>
        public int UtcOffsetMinutes { get; init; }
    }
}
