namespace TummlyBackend.DTOs.PrivacyConsent
{
    public sealed class PermissionRecordsListQuery
    {
        public int RestaurantId { get; init; }

        public IReadOnlyList<int> LocationIds { get; init; } = [];

        public string? Q { get; init; }

        public string[] Permissions { get; init; } = [];

        public string[] CurrentStates { get; init; } = [];

        public string[] Locations { get; init; } = [];

        public string? DatePreset { get; init; }

        public DateTime? DateFrom { get; init; }

        public DateTime? DateTo { get; init; }

        public string Sort { get; init; } = "recent-activity";

        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 25;

        public int UtcOffsetMinutes { get; init; }
    }
}
