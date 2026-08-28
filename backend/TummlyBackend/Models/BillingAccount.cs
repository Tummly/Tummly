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
