namespace TummlyBackend.DTOs.Reports
{
    /// <summary>
    /// Row-grain Feedback export / PDF snapshot (REP-02 / REP-03).
    /// </summary>
    public sealed class ReportsExportFeedbackRowDto
    {
        public int FeedbackId { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public string Tags { get; init; } = string.Empty;

        public string Comment { get; init; } = string.Empty;

        public string Source { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public string FollowUp { get; init; } = string.Empty;

        public string Guest { get; init; } = string.Empty;
    }
}
