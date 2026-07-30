namespace TummlyBackend.DTOs.Capture
{
    public sealed class CaptureOverviewQuery
    {
        public required int OwnerUserId { get; init; }

        public DateTime? From { get; init; }

        public DateTime? To { get; init; }
    }
}
