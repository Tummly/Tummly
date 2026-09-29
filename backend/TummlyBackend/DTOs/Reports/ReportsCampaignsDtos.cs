namespace TummlyBackend.DTOs.Reports
{
    public sealed class ReportsCampaignsPerformanceRowDto
    {
        public int CampaignId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Goal { get; init; }

        public string? Channel { get; init; }

        public int Sent { get; init; }

        public int Claims { get; init; }

        public int Redemptions { get; init; }

        public int Unsubscribes { get; init; }

        public string Status { get; init; } = string.Empty;
    }

    public sealed class ReportsCampaignsAttentionRowDto
    {
        public int CampaignId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;
    }

    public sealed class ReportsCampaignsDto
    {
        public bool LifetimeEmpty { get; init; }

        public ReportsMetricDto? CampaignsSent { get; init; }

        public ReportsMetricDto? GuestsMessaged { get; init; }

        public ReportsMetricDto? FailedSends { get; init; }

        public ReportsMetricDto? OfferClaims { get; init; }

        public ReportsMetricDto? OfferRedemptions { get; init; }

        public ReportsMetricDto? Unsubscribes { get; init; }

        public IReadOnlyList<ReportsCampaignsPerformanceRowDto>? Performance
        {
            get;
            init;
        }

        public IReadOnlyList<ReportsCampaignsAttentionRowDto>? NeedsAttention
        {
            get;
            init;
        }
    }
}
