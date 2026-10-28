namespace TummlyBackend.DTOs.Admin
{
    public sealed class AdminAccountListRowDto
    {
        public int RestaurantId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string SubscriptionPlan { get; set; } = string.Empty;

        public string BillingStatus { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// Pilot end on a Pilot account; current period end (renewal) otherwise.
        /// Null when the server has no stored date.
        /// </summary>
        public DateTime? PeriodEndUtc { get; set; }

        public int ActiveLocationCount { get; set; }

        public string? OwnerDisplayName { get; set; }
    }

    public sealed class AdminAccountListResultDto
    {
        public IReadOnlyList<AdminAccountListRowDto> Items { get; set; } = [];

        public int TotalCount { get; set; }

        public int Skip { get; set; }

        public int Take { get; set; }
    }
}
