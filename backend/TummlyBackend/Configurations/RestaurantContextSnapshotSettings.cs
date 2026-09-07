namespace TummlyBackend.Configurations
{
    public sealed class RestaurantContextSnapshotSettings
    {
        public const string SectionName = "RestaurantContextSnapshot";

        public string SchemaVersion { get; set; } = "2026-09-05";

        public decimal CampaignUnderperformingThresholdPct { get; set; } = 50;

        public decimal LocationDivergenceThresholdPts { get; set; } = 15;

        public int MinDaysForTrendClaim { get; set; } = 14;

        public int MinDataPointsForMetric { get; set; } = 20;

        public decimal VipAtRiskFrequencyDropPct { get; set; } = 30;

        public int OfferExpiringWindowDays { get; set; } = 7;

        public int CacheTtlSeconds { get; set; } = 180;

        public int NewAccountHistoryDays { get; set; } = 30;

        public int FlaggedFeedbackCap { get; set; } = 5;
    }
}
