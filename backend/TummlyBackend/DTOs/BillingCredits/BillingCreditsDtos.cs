namespace TummlyBackend.DTOs.BillingCredits
{
    public sealed class BillingCreditsPageDto
    {
        public bool Success { get; set; } = true;

        public string ActorPermissionRole { get; set; } = string.Empty;

        public bool ActorCanManage { get; set; }

        public bool ActorCanPersistBillingContacts { get; set; }

        public PlanSubscriptionSnapshotDto PlanSubscription { get; set; } = new();

        public PaymentMethodSnapshotDto? PaymentMethod { get; set; }

        public List<InvoiceRowDto> Invoices { get; set; } = [];

        public BillingContactsSnapshotDto BillingContacts { get; set; } = new();
    }

    public sealed class PaymentMethodSnapshotDto
    {
        public string Kind { get; set; } = "card";

        public string? Brand { get; set; }

        public string? Last4 { get; set; }

        public string? ExpiryLabel { get; set; }

        public string? WalletName { get; set; }
    }

    public sealed class InvoiceRowDto
    {
        public string InvoiceNo { get; set; } = string.Empty;

        public string InvoiceDateLabel { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string AmountLabel { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public bool ShowActions { get; set; }
    }

    public sealed class PaymentMethodUpdateSessionDto
    {
        public bool Success { get; set; } = true;

        public string RedirectUrl { get; set; } = string.Empty;
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

        public bool AllowSms5000TopUp { get; set; }
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

    public sealed class BillingContactsSnapshotDto
    {
        public int BillingContactUserId { get; set; }

        public string? BillingEmail { get; set; }

        public List<BillingContactPickerItemDto> EligibleMembers { get; set; } = [];

        public BillingAlertRoleFlagsDto LowCreditAlerts { get; set; } = new();

        public BillingPaymentFailureAlertFlagsDto PaymentFailureAlerts { get; set; } = new();
    }

    public sealed class BillingContactPickerItemDto
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }

    public sealed class BillingAlertRoleFlagsDto
    {
        public bool Owner { get; set; }

        public bool Admin { get; set; }

        public bool BillingContact { get; set; }
    }

    public sealed class BillingPaymentFailureAlertFlagsDto
    {
        public bool Owner { get; set; }

        public bool BillingContact { get; set; }
    }

    public sealed class UpdateBillingContactsRequest
    {
        public int BillingContactUserId { get; set; }

        public string? BillingEmail { get; set; }

        public BillingAlertRoleFlagsDto LowCreditAlerts { get; set; } = new();

        public BillingPaymentFailureAlertFlagsDto PaymentFailureAlerts { get; set; } = new();
    }

    public sealed class UpdateBillingContactsResponseDto
    {
        public bool Success { get; set; } = true;

        public BillingContactsSnapshotDto BillingContacts { get; set; } = new();
    }
}
