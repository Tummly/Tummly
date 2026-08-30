using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Open first-paid-conversion Revolut subscription / setup-order
    /// correlation (ticket 14). Outside BillingAccount plan fields.
    /// </summary>
    public class RevolutPendingPaySession
    {
        [Key]
        public Guid Id { get; set; }

        public int RestaurantId { get; set; }

        [ForeignKey(nameof(RestaurantId))]
        public BillingAccount? BillingAccount { get; set; }

        [MaxLength(32)]
        public string TargetPlan { get; set; } = string.Empty;

        /// <summary>API cadence: <c>monthly</c> or <c>annual</c>.</summary>
        [MaxLength(16)]
        public string TargetCadence { get; set; } = string.Empty;

        [MaxLength(128)]
        public string RevolutSubscriptionId { get; set; } = string.Empty;

        [MaxLength(128)]
        public string SetupOrderId { get; set; } = string.Empty;

        [MaxLength(2048)]
        public string CheckoutUrl { get; set; } = string.Empty;

        [MaxLength(128)]
        public string IdempotencyKey { get; set; } = string.Empty;

        /// <summary>False after cancel / replace.</summary>
        public bool IsOpen { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; }
    }
}
