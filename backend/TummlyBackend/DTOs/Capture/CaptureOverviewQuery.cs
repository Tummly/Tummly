namespace TummlyBackend.DTOs.Capture
{
    public sealed class CaptureOverviewQuery
    {
        public required int RestaurantId { get; init; }

        public required IReadOnlyList<int> ScopedLocationIds { get; init; }

        public DateTime? From { get; init; }

        public DateTime? To { get; init; }
    }
}
