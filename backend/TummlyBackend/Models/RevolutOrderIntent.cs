using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TummlyBackend.Models
{
    /// <summary>
    /// One-time Revolut order intent (ticket 20). Purpose routes webhook apply.
    /// </summary>
    public class RevolutOrderIntent
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(128)]
        public string OrderId { get; set; } = string.Empty;

        public int RestaurantId { get; set; }

        [ForeignKey(nameof(RestaurantId))]
        public BillingAccount? BillingAccount { get; set; }

        /// <summary>e.g. <c>plan_upgrade_proration</c>.</summary>
        [MaxLength(64)]
        public string Purpose { get; set; } = string.Empty;

        [MaxLength(32)]
        public string TargetPlan { get; set; } = string.Empty;

        /// <summary>API cadence: <c>monthly</c> or <c>annual</c>.</summary>
        [MaxLength(16)]
        public string TargetCadence { get; set; } = string.Empty;

        [MaxLength(128)]
        public string RevolutSubscriptionId { get; set; } = string.Empty;

        [MaxLength(2048)]
        public string CheckoutUrl { get; set; } = string.Empty;

        [MaxLength(128)]
        public string IdempotencyKey { get; set; } = string.Empty;

        /// <summary>False after cancel / replace / completed apply.</summary>
        public bool IsOpen { get; set; } = true;

        public int NetAmountMinor { get; set; }

        public int VatAmountMinor { get; set; }

        public int GrossAmountMinor { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public static class RevolutOrderIntentPurposes
    {
        public const string PlanUpgradeProration = "plan_upgrade_proration";
    }
}
