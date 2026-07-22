namespace TummlyBackend.DTOs.Guests
{
    public sealed class GuestsExportQuery
    {
        public required IReadOnlyList<int> LocationIds { get; init; }

        public required IReadOnlyDictionary<int, string> LocationNamesById { get; init; }

        public required int ShellLocationId { get; init; }

        public required int RestaurantId { get; init; }

        public required int OwnerUserId { get; init; }

        /// <summary>
        /// Null = full-list mode. Non-null = Selected mode (must be non-empty).
        /// </summary>
        public IReadOnlyList<int>? GuestIds { get; init; }

        public string SmartGroup { get; init; } = "all-guests";

        public string? Q { get; init; }

        public string Sort { get; init; } = "recent-activity";

        public IReadOnlyList<string> Marketing { get; init; } = Array.Empty<string>();

        public IReadOnlyList<string> Contact { get; init; } = Array.Empty<string>();

        public IReadOnlyList<string> Sentiment { get; init; } = Array.Empty<string>();

        public IReadOnlyList<int> TagIds { get; init; } = Array.Empty<int>();

        public string? DateAxis { get; init; }

        public string? DatePreset { get; init; }

        public DateTime? DateFrom { get; init; }

        public DateTime? DateTo { get; init; }

        /// <summary>
        /// Minutes east of UTC for resolving <see cref="DatePreset"/> in the
        /// operator's local calendar. Same sign as JavaScript
        /// <c>-Date#getTimezoneOffset()</c>.
        /// </summary>
        public int UtcOffsetMinutes { get; init; }

        /// <summary>
        /// Filename scope token from list Location override: <c>all</c>, <c>multi</c>,
        /// or a single location id. Ignored for Selected (derived from row venues).
        /// </summary>
        public required string LocationScopeToken { get; init; }
    }

    public sealed class GuestsExportResult
    {
        public required string FileName { get; init; }

        public required string ContentType { get; init; }

        public required byte[] Content { get; init; }
    }
}
