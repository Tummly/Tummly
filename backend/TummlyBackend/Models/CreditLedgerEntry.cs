using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class CreditLedgerEntry
    {
        [Key]
        public Guid Id { get; set; }

        public int RestaurantId { get; set; }

        public BillingAccount BillingAccount { get; set; } = null!;

        [MaxLength(16)]
        public string Channel { get; set; } = string.Empty;

        [MaxLength(32)]
        public string EntryType { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public Guid? AllocationId { get; set; }

        public CreditLedgerEntry? Allocation { get; set; }

        [MaxLength(128)]
        public string? ReservationRef { get; set; }

        public int? LocationId { get; set; }

        public RestaurantLocation? Location { get; set; }

        [MaxLength(64)]
        public string? PricebookVersion { get; set; }

        public DateTime? ExpiresAtUtc { get; set; }

        public DateTime? PeriodStartUtc { get; set; }

        public Guid? ReversedEntryId { get; set; }

        public CreditLedgerEntry? ReversedEntry { get; set; }

        [MaxLength(128)]
        public string? SourcePaymentRef { get; set; }

        [MaxLength(32)]
        public string? CorrectionSource { get; set; }

        public int? ActorStaffUserId { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        public int? HelpCentreQueryId { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public static class CreditChannels
    {
        public const string Ai = "ai";

        public const string Email = "email";

        public const string Sms = "sms";

        public static readonly IReadOnlyList<string> All =
        [
            Email,
            Sms,
            Ai,
        ];
    }

    public static class CreditLedgerEntryTypes
    {
        public const string IncludedAllocation = "included_allocation";

        public const string PilotAllocation = "pilot_allocation";

        public const string TopupAllocation = "topup_allocation";

        public const string Reservation = "reservation";

        public const string Consumption = "consumption";

        public const string Release = "release";

        public const string Expiry = "expiry";

        public const string Refund = "refund";

        public const string ManualAdjustment = "manual_adjustment";

        public const string Reversal = "reversal";

        public const string PlanMigration = "plan_migration";
    }
}
