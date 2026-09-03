namespace TummlyBackend.DTOs.Reports
{
    public sealed class ReportsRateMetricDto
    {
        public double? Value { get; init; }

        public double? ValuePrevious { get; init; }
    }

    public sealed class ReportsOffersKpisDto
    {
        public ReportsMetricDto ActiveOffers { get; init; } = new();

        public ReportsMetricDto OfferClaims { get; init; } = new();

        public ReportsMetricDto Redemptions { get; init; } = new();

        public ReportsRateMetricDto RedemptionRate { get; init; } = new();

        public ReportsMetricDto ExpiredClaims { get; init; } = new();

        public ReportsMetricDto InvalidAttempts { get; init; } = new();
    }

    public sealed class ReportsOffersPerformanceRowDto
    {
        public int OfferId { get; init; }

        public string Offer { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public int Claims { get; init; }

        public int Redemptions { get; init; }

        public double? Rate { get; init; }

        public int Expired { get; init; }

        public int Invalid { get; init; }
    }

    public sealed class ReportsOffersRecentRedemptionDto
    {
        public int Id { get; init; }

        public DateTime DateTimeUtc { get; init; }

        public string OfferTitle { get; init; } = string.Empty;

        public string GuestName { get; init; } = string.Empty;

        public string LocationName { get; init; } = string.Empty;

        public string Outcome { get; init; } = "redeemed";
    }

    public sealed class ReportsOffersRepeatedInvalidSignalDto
    {
        public string Kind { get; init; } = "repeated-invalid";

        public int Count { get; init; }

        public string Target { get; init; } = "redemption-log";
    }

    public sealed class ReportsOffersLowRedemptionSignalDto
    {
        public string Kind { get; init; } = "low-redemption";

        public int OfferId { get; init; }

        public string OfferTitle { get; init; } = string.Empty;

        public int Claims { get; init; }

        public int Redemptions { get; init; }

        public double Rate { get; init; }

        public string Target { get; init; } = "offers";
    }

    public sealed class ReportsOffersDto
    {
        public bool LifetimeEmpty { get; init; }

        public ReportsOffersKpisDto? Kpis { get; init; }

        public IReadOnlyList<ReportsOffersPerformanceRowDto>? Performance
        {
            get;
            init;
        }

        public IReadOnlyList<ReportsOffersRecentRedemptionDto>? RecentRedemptions
        {
            get;
            init;
        }

        public IReadOnlyList<object>? ControlSignals { get; init; }
    }
}
