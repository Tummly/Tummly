using TummlyBackend.DTOs.BillingCredits;

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

    public sealed class AdminAccountProfileDto
    {
        public AdminAccountIdentityDto Identity { get; set; } = new();

        public AdminAccountLifecycleDto Lifecycle { get; set; } = new();

        public AdminAccountLocationsDto Locations { get; set; } = new();

        public AdminAccountCapacityDto Capacity { get; set; } = new();

        public IReadOnlyList<AdminAccountCreditChannelDto> Credits { get; set; } = [];

        public IReadOnlyList<AdminAccountPaymentDto> Payments { get; set; } = [];

        public IReadOnlyList<AdminAccountInvoiceDto> Invoices { get; set; } = [];

        public IReadOnlyList<AdminAuditEventDto> RecentAudit { get; set; } = [];
    }

    public sealed class AdminAccountIdentityDto
    {
        public int RestaurantId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string? LegalName { get; set; }

        public string? OwnerName { get; set; }

        /// <summary>Owner is internal staff's customer contact, not guest data; shown in full.</summary>
        public string? OwnerEmail { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        /// <summary>The billing account primary key, which is the Workspace ID.</summary>
        public int BillingAccountId { get; set; }
    }

    public sealed class AdminAccountLifecycleDto
    {
        public string SubscriptionPlan { get; set; } = string.Empty;

        /// <summary>Term: Monthly or Annual. Null on Pilot.</summary>
        public string? BillingCycle { get; set; }

        public string BillingStatus { get; set; } = string.Empty;

        public DateTime? PilotStartedAtUtc { get; set; }

        public DateTime? PilotPeriodEndUtc { get; set; }

        /// <summary>Start of the current included-credit period, from the stored ledger window.</summary>
        public DateTime? CurrentPeriodStartUtc { get; set; }

        public DateTime? CurrentPeriodEndUtc { get; set; }

        public AdminAccountScheduledChangeDto ScheduledChange { get; set; } = new();

        public DateTime? SoftLockEnteredAtUtc { get; set; }

        public DateTime? DormantEnteredAtUtc { get; set; }
    }

    public sealed class AdminAccountScheduledChangeDto
    {
        public bool HasScheduledChange { get; set; }

        public string? TargetSubscriptionPlan { get; set; }

        public string? TargetBillingCycle { get; set; }

        public int? TargetExtraLocationCount { get; set; }

        public bool CancelScheduled { get; set; }

        /// <summary>Renewal date at which the scheduled change applies.</summary>
        public DateTime? EffectiveAtUtc { get; set; }

        /// <summary>Set only when cancellation is scheduled; the renewal date.</summary>
        public DateTime? CancelAtUtc { get; set; }
    }

    public sealed class AdminAccountLocationsDto
    {
        public AdminAccountLocationCountsDto Counts { get; set; } = new();

        public IReadOnlyList<AdminAccountLocationRowDto> Items { get; set; } = [];
    }

    public sealed class AdminAccountLocationCountsDto
    {
        public int Included { get; set; }

        public int PaidExtra { get; set; }

        public int Active { get; set; }

        public int Total { get; set; }
    }

    public sealed class AdminAccountLocationRowDto
    {
        public int LocationId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string LifecycleStatus { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public int AssignedManagerCount { get; set; }
    }

    public sealed class AdminAccountCapacityDto
    {
        public PlanEntitlementLimitDto Seats { get; set; } = new();

        public PlanEntitlementLimitDto QrPlacements { get; set; } = new();

        public PlanEntitlementLimitDto Offers { get; set; } = new();

        public IReadOnlyList<AdminAccountPendingInviteDto> PendingInvites { get; set; } = [];
    }

    public sealed class AdminAccountPendingInviteDto
    {
        public string MaskedEmail { get; set; } = string.Empty;

        public DateTime SentAtUtc { get; set; }

        public DateTime ExpiresAtUtc { get; set; }
    }

    public sealed class AdminAccountCreditChannelDto
    {
        /// <summary>ai, email or sms. Channels never pool.</summary>
        public string Channel { get; set; } = string.Empty;

        public int Available { get; set; }

        public int Reserved { get; set; }

        public int IncludedThisPeriod { get; set; }

        public int PrepaidRemaining { get; set; }

        public DateTime? NearestPrepaidExpiryUtc { get; set; }
    }

    public sealed class AdminAccountPaymentDto
    {
        public Guid Id { get; set; }

        public string Kind { get; set; } = string.Empty;

        public int AmountPence { get; set; }

        public string Currency { get; set; } = string.Empty;

        /// <summary>True until the intent is cancelled, replaced or completed.</summary>
        public bool IsOpen { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        /// <summary>Provider order reference, redacted to its last six characters.</summary>
        public string? ProviderReference { get; set; }
    }

    public sealed class AdminAccountInvoiceDto
    {
        public string DocumentNumber { get; set; } = string.Empty;

        /// <summary>Invoice or Credit note.</summary>
        public string Type { get; set; } = string.Empty;

        public DateTime InvoiceDateUtc { get; set; }

        public int GrossPence { get; set; }

        public string Currency { get; set; } = string.Empty;
    }
}
