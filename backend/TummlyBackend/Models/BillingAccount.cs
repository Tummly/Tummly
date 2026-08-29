using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Required 1:1 billing child of <see cref="Restaurant"/>.
    /// Primary key is <see cref="RestaurantId"/>.
    /// </summary>
    public class BillingAccount
    {
        [Key]
        [ForeignKey(nameof(Restaurant))]
        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; } = null!;

        /// <summary>
        /// Opaque Revolut customer id. Empty until the Revolut map creates the customer.
        /// Unique among non-empty values.
        /// </summary>
        [MaxLength(128)]
        public string? RevolutCustomerId { get; set; }

        [MaxLength(32)]
        public string SubscriptionPlan { get; set; } = BillingSubscriptionPlans.Pilot;

        /// <summary>
        /// Empty on Pilot. Monthly or Annual when paid.
        /// </summary>
        [MaxLength(16)]
        public string? BillingCycle { get; set; }

        [MaxLength(32)]
        public string BillingStatus { get; set; } = BillingStatuses.Pilot;

        /// <summary>
        /// This account's contracted catalog id (<c>pricebook.id</c>).
        /// </summary>
        [MaxLength(64)]
        public string ContractedPricebookId { get; set; } = string.Empty;

        [MaxLength(320)]
        public string? BillingEmail { get; set; }

        public bool LowCreditAlertOwner { get; set; } = true;

        public bool LowCreditAlertAdmin { get; set; }

        public bool LowCreditAlertBillingContact { get; set; } = true;

        public bool PaymentFailureAlertOwner { get; set; } = true;

        public bool PaymentFailureAlertBillingContact { get; set; } = true;

        /// <summary>
        /// Lifetime starter-kit state: unused / used / pending dispatch.
        /// </summary>
        [MaxLength(32)]
        public string StarterKitState { get; set; } = StarterKitStates.Unused;

        /// <summary>
        /// When true, the account may buy the 5,000 SMS top-up pack without Group plan.
        /// </summary>
        public bool AllowSms5000TopUp { get; set; }

        /// <summary>
        /// Paid Additional Group Location quantity. Default 0. Meaningful on Group.
        /// </summary>
        public int PaidExtraLocationCount { get; set; }

        /// <summary>
        /// End of the current paid period. Scheduled change and Cancel plan apply here.
        /// Null on unpaid Pilot.
        /// </summary>
        public DateTime? RenewalDateUtc { get; set; }

        /// <summary>
        /// When true, a Scheduled change snapshot is pending for the Renewal date.
        /// Empty slot when false.
        /// </summary>
        public bool HasScheduledChange { get; set; }

        /// <summary>
        /// Target Subscription plan for the scheduled snapshot.
        /// </summary>
        [MaxLength(32)]
        public string? ScheduledTargetSubscriptionPlan { get; set; }

        /// <summary>
        /// Target Billing cycle for the scheduled snapshot.
        /// </summary>
        [MaxLength(16)]
        public string? ScheduledTargetBillingCycle { get; set; }

        /// <summary>
        /// Target PaidExtraLocationCount for the scheduled snapshot.
        /// </summary>
        public int? ScheduledTargetExtraLocationCount { get; set; }

        /// <summary>
        /// When true, cancel is exclusive at apply (lock 07).
        /// </summary>
        public bool ScheduledCancelPlan { get; set; }

        /// <summary>
        /// Unpaid Pilot clock. Copied from Activation period at Account activation
        /// and on Extend activation. Cleared on <c>ActivatePaidPlan</c>.
        /// </summary>
        public DateTime? PilotPeriodEnd { get; set; }

        /// <summary>
        /// Open paid dunning episode start. Null when no episode is open.
        /// </summary>
        public DateTime? DunningEpisodeStartedAt { get; set; }

        /// <summary>
        /// Which of 0 / 3 / 7 / 10 / 24 have fired this episode. Empty when none.
        /// </summary>
        [MaxLength(32)]
        public string? DunningFiredSteps { get; set; }

        public DateTime? SoftLockEnteredAt { get; set; }

        public DateTime? DormantEnteredAt { get; set; }

        public bool PilotSoftLockNotified { get; set; }

        public bool PilotDormantNotified { get; set; }

        public bool ChargebackRestricted { get; set; }

        public void ClearScheduledChangeSlot()
        {
            HasScheduledChange = false;
            ScheduledTargetSubscriptionPlan = null;
            ScheduledTargetBillingCycle = null;
            ScheduledTargetExtraLocationCount = null;
            ScheduledCancelPlan = false;
        }
    }

    public static class BillingSubscriptionPlans
    {
        public const string Pilot = "Pilot";

        public const string Starter = "Starter";

        public const string Growth = "Growth";

        public const string Group = "Group";
    }

    public static class BillingStatuses
    {
        public const string Pilot = "Pilot";

        public const string Active = "Active";

        public const string PastDue = "Past due";

        public const string SoftLock = "Soft lock";

        public const string Dormant = "Dormant";
    }

    public static class StarterKitStates
    {
        public const string Unused = "unused";

        public const string Used = "used";

        public const string PendingDispatch = "pending dispatch";
    }

    public static class BillingCycles
    {
        public const string Monthly = "Monthly";

        public const string Annual = "Annual";
    }
}
