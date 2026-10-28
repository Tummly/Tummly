namespace TummlyBackend.DTOs.Admin
{
    public sealed class AdminOverviewDto
    {
        public DateTime GeneratedAtUtc { get; set; }

        public AdminOverviewMetricDto<IReadOnlyList<AdminOverviewBillingStatusCountDto>> WorkspacesByBillingStatus { get; set; } = new();

        public AdminOverviewMetricDto<int?> ShopOrdersPaidNotDispatched { get; set; } = new();

        public AdminOverviewMetricDto<int?> ShopOrdersFlaggedForReview { get; set; } = new();

        public AdminOverviewMetricDto<int?> PrintAssetsFailed { get; set; } = new();

        public AdminOverviewMetricDto<int?> PilotsEndingWithin7Days { get; set; } = new();

        public AdminOverviewMetricDto<int?> OpenHelpCentreQueries { get; set; } = new();

        public AdminOverviewMetricDto<int?> FailedAuditEventsLast7Days { get; set; } = new();
    }

    /// <summary>
    /// One overview count. When <see cref="Available"/> is false the query
    /// failed and <see cref="Value"/> is null. A failed query is never shown as zero.
    /// </summary>
    public sealed class AdminOverviewMetricDto<T>
    {
        public bool Available { get; set; }

        public T? Value { get; set; }
    }

    public sealed class AdminOverviewBillingStatusCountDto
    {
        public string BillingStatus { get; set; } = string.Empty;

        public int Count { get; set; }
    }
}
