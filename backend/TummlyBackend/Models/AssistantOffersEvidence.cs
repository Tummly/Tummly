namespace TummlyBackend.Models
{
    public sealed record AssistantOffersEvidence(
        int CatalogTotalCount,
        int CatalogSampleCount,
        int ActiveOffers,
        int OffersIssued,
        int Claims,
        int Redemptions,
        double? ClaimToRedemptionRate,
        IReadOnlyList<AssistantOfferCatalogRow> Catalog,
        IReadOnlyList<AssistantOfferMetricsRow> PerOfferMetrics,
        IReadOnlyList<AssistantOfferLinkedCampaignRow> LinkedCampaigns,
        IReadOnlyList<AssistantOfferLogRow> ClaimLogs,
        IReadOnlyList<AssistantOfferLogRow> RedemptionLogs
    )
    {
        public static AssistantOffersEvidence Empty { get; } =
            new(0, 0, 0, 0, 0, 0, null, [], [], [], [], []);

        public bool IsEmpty =>
            CatalogTotalCount == 0
            && OffersIssued == 0
            && Claims == 0
            && Redemptions == 0;

        public bool HasCatalogFacts => CatalogTotalCount > 0;

        public bool HasPerformanceFacts =>
            OffersIssued > 0 || Claims > 0 || Redemptions > 0;

        public bool DisclosesSample => CatalogTotalCount > CatalogSampleCount;
    }

    public sealed record AssistantOfferCatalogRow(
        int Id,
        string Title,
        string Status,
        DateTime CreatedAt
    );

    public sealed record AssistantOfferMetricsRow(
        int OfferId,
        string Title,
        int Claims,
        int Redemptions,
        double? RedemptionRate,
        int ExpiredUnused,
        int FailedAttempts
    );

    public sealed record AssistantOfferLinkedCampaignRow(
        int OfferId,
        int CampaignId,
        string CampaignName,
        string Status
    );

    public sealed record AssistantOfferLogRow(
        int OfferId,
        string Title,
        DateTime AtUtc,
        string ClaimCode
    );
}
