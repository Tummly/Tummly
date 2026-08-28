namespace TummlyBackend.DTOs.BillingCredits
{
    public sealed class BillingCreditsPageDto
    {
        public bool Success { get; set; } = true;

        public string ActorPermissionRole { get; set; } = string.Empty;

        public bool ActorCanManage { get; set; }

        public PlanSubscriptionSnapshotDto PlanSubscription { get; set; } = new();
    }

    public sealed class PlanSubscriptionSnapshotDto
    {
        public string SubscriptionPlan { get; set; } = "Pilot";

        public string BillingStatus { get; set; } = "Pilot";

        public string? RenewalDateLabel { get; set; }

        public int EmailCreditsRemaining { get; set; }

        public int SmsCreditsRemaining { get; set; }

        public int AiCreditsRemaining { get; set; }

        public string? BillingCycle { get; set; }

        public string PlanPriceNet { get; set; } = "£0";

        public int IncludedLocations { get; set; } = 1;

        public int ActiveLocations { get; set; }

        public string IncludedEmailCreditsLabel { get; set; } = "500 once";

        public string IncludedSmsCreditsLabel { get; set; } = "20 once";

        public string IncludedAiCreditsLabel { get; set; } = "20 once";

        public string StarterKitState { get; set; } = "unused";

        public string PricebookId { get; set; } = string.Empty;

        public string? ScheduledChangeLine { get; set; }

        public bool IsPilot { get; set; } = true;
    }

    public sealed class CreditsUsageSnapshotDto
    {
        public bool Success { get; set; } = true;

        public string PeriodLabel { get; set; } = string.Empty;

        public string StarterKitState { get; set; } = "unused";

        public bool IsPilot { get; set; } = true;

        public List<CreditChannelUsageDto> Channels { get; set; } = [];
    }

    public sealed class CreditChannelUsageDto
    {
        public string Channel { get; set; } = string.Empty;

        public int CombinedRemaining { get; set; }

        public int UsedThisCycle { get; set; }

        public int IncludedThisPeriod { get; set; }

        public int PurchasedRemaining { get; set; }

        public string? PurchasedExpiryLabel { get; set; }
    }
}
