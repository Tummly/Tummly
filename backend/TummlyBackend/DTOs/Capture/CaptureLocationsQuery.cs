namespace TummlyBackend.DTOs.Capture
{
    public sealed class CaptureLocationsQuery
    {
        public required int RestaurantId { get; init; }

        public required IReadOnlyList<int> ScopedLocationIds { get; init; }

        public DateTime? From { get; init; }

        public DateTime? To { get; init; }

        public string? Q { get; init; }

        public string[]? Status { get; init; }

        public int[]? LocationIds { get; init; }

        public string Sort { get; init; } = "highest-qr-scans";

        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 20;
    }
}
