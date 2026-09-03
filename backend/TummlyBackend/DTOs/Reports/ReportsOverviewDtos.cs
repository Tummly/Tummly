namespace TummlyBackend.DTOs.Reports
{
    public sealed class ReportsMetricDto
    {
        public int Value { get; init; }

        public int ValuePrevious { get; init; }
    }

    public sealed class ReportsOverviewFunnelDto
    {
        public ReportsMetricDto QrScans { get; init; } = new();

        public ReportsMetricDto FeedbackReceived { get; init; } = new();

        public ReportsMetricDto MarketingOptIns { get; init; } = new();

        public ReportsMetricDto OfferRedemptions { get; init; } = new();

        public ReportsMetricDto CampaignsSent { get; init; } = new();
    }

    public sealed class ReportsOverviewPrivateFeedbackDto
    {
        public ReportsMetricDto FeedbackMessages { get; init; } = new();

        public ReportsMetricDto MarketingOptIns { get; init; } = new();

        public ReportsMetricDto FollowUpNeeded { get; init; } = new();

        public ReportsMetricDto FollowedUp { get; init; } = new();
    }

    public sealed class ReportsOverviewOffersCampaignsDto
    {
        public ReportsMetricDto ActiveOffers { get; init; } = new();

        public ReportsMetricDto OfferClaims { get; init; } = new();

        public ReportsMetricDto OfferRedemptions { get; init; } = new();

        public ReportsMetricDto CampaignsSent { get; init; } = new();

        public ReportsMetricDto Unsubscribes { get; init; } = new();
    }

    public sealed class ReportsOverviewCaptureSourceDto
    {
        public int QrCodeId { get; init; }

        public string Source { get; init; } = string.Empty;

        public int Scans { get; init; }

        public int Feedback { get; init; }

        public int MarketingOptIns { get; init; }
    }

    public sealed class ReportsOverviewDto
    {
        public bool LifetimeEmpty { get; init; }

        public ReportsOverviewFunnelDto? Funnel { get; init; }

        public ReportsOverviewPrivateFeedbackDto? PrivateFeedback { get; init; }

        public ReportsOverviewOffersCampaignsDto? OffersAndCampaigns { get; init; }

        public IReadOnlyList<ReportsOverviewCaptureSourceDto>? TopCaptureSources
        {
            get;
            init;
        }
    }
}
