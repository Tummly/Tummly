using TummlyBackend.DTOs.BillingCredits;

namespace TummlyBackend.DTOs.Offers
{
    /// <summary>
    /// Main Offers Performance KPIs for half-open [From, To).
    /// ClaimToRedemptionRate is Redemptions ÷ Claims (0–1), or null when Claims = 0.
    /// </summary>
    public sealed class OffersPerformanceDto
    {
        public int ActiveOffers { get; init; }

        public int OffersIssued { get; init; }

        public int Claims { get; init; }

        public int Redemptions { get; init; }

        public double? ClaimToRedemptionRate { get; init; }

        public PlanEntitlementsAccountSnapshotDto Entitlements { get; init; } = new();
    }

    /// <summary>
    /// Details Overview KPIs for one catalog offer in half-open [From, To).
    /// RedemptionRate is Redemptions ÷ Claims (0–1), or null when Claims = 0.
    /// </summary>
    public sealed class OfferMetricsDto
    {
        public int Claims { get; init; }

        public int Redemptions { get; init; }

        public double? RedemptionRate { get; init; }

        public int ExpiredUnused { get; init; }

        public int FailedAttempts { get; init; }
    }
}
