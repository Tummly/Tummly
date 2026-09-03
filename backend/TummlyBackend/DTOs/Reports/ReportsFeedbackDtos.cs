namespace TummlyBackend.DTOs.Reports
{
    public sealed class ReportsFeedbackNeedsAttentionDto
    {
        public int FeedbackId { get; init; }

        public DateTime SubmittedAt { get; init; }

        public string GuestName { get; init; } = string.Empty;

        public string Source { get; init; } = string.Empty;

        public string CommentPreview { get; init; } = string.Empty;

        public string WorkflowStatus { get; init; } = string.Empty;
    }

    public sealed class ReportsFeedbackBySourceDto
    {
        public int QrCodeId { get; init; }

        public string Source { get; init; } = string.Empty;

        public int Feedback { get; init; }

        public int MarketingOptIns { get; init; }

        public int FollowUpNeeded { get; init; }
    }

    public sealed class ReportsFeedbackKpisDto
    {
        public ReportsMetricDto FeedbackReceived { get; init; } = new();

        public ReportsMetricDto MarketingOptIns { get; init; } = new();

        public ReportsMetricDto FollowUpNeeded { get; init; } = new();

        public ReportsMetricDto Resolved { get; init; } = new();
    }

    public sealed class ReportsFeedbackStatusStripDto
    {
        public ReportsMetricDto New { get; init; } = new();

        public ReportsMetricDto InProgress { get; init; } = new();

        public ReportsMetricDto FollowUpNeeded { get; init; } = new();

        public ReportsMetricDto Resolved { get; init; } = new();
    }

    public sealed class ReportsFeedbackDto
    {
        public bool LifetimeEmpty { get; init; }

        public ReportsFeedbackKpisDto? Kpis { get; init; }

        public ReportsFeedbackStatusStripDto? Status { get; init; }

        public IReadOnlyList<ReportsFeedbackNeedsAttentionDto>? NeedsAttention
        {
            get;
            init;
        }

        public IReadOnlyList<ReportsFeedbackBySourceDto>? BySource { get; init; }
    }
}
