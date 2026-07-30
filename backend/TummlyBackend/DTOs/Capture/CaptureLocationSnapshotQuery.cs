namespace TummlyBackend.DTOs.Capture
{
    public sealed class CaptureLocationSnapshotQuery
    {
        public required int LocationId { get; init; }

        public DateTime? From { get; init; }

        public DateTime? To { get; init; }
    }
}
