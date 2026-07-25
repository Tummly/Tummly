namespace TummlyBackend.DTOs.Guests
{
    public sealed class GuestsListQuery
    {
        public required IReadOnlyList<int> LocationIds { get; init; }

        public required IReadOnlyDictionary<int, string> LocationNamesById { get; init; }

        public required int ShellLocationId { get; init; }

        public required int RestaurantId { get; init; }

        public string SmartGroup { get; init; } = "all-guests";

        public string? Q { get; init; }

        public string Sort { get; init; } = "recent-activity";

        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 25;

        public IReadOnlyList<string> Marketing { get; init; } = Array.Empty<string>();

        public IReadOnlyList<string> Contact { get; init; } = Array.Empty<string>();

        public IReadOnlyList<string> Sentiment { get; init; } = Array.Empty<string>();

        public IReadOnlyList<int> TagIds { get; init; } = Array.Empty<int>();

        public string? DateAxis { get; init; }

        public string? DatePreset { get; init; }

        public DateTime? DateFrom { get; init; }

        public DateTime? DateTo { get; init; }

        public string? OverviewDatePreset { get; init; }

        public DateTime? OverviewDateFrom { get; init; }

        public DateTime? OverviewDateTo { get; init; }

        /// <summary>
        /// Minutes east of UTC for resolving <see cref="DatePreset"/> /
        /// <see cref="OverviewDatePreset"/> in the operator's local calendar.
        /// Same sign as JavaScript <c>-Date#getTimezoneOffset()</c>.
        /// </summary>
        public int UtcOffsetMinutes { get; init; }

        /// <summary>
        /// When false, skips overview KPIs and Smart Group counts (table-only
        /// refetch). Defaults to true.
        /// </summary>
        public bool IncludeAggregates { get; init; } = true;
    }
}
