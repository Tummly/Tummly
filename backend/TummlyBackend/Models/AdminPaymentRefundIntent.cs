using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Staff Admin–initiated Revolut payment refund intent (ticket 25 / lock 09).
    /// Distinguishes expected refunds from unexpected Business-UI refunds.
    /// </summary>
    public class AdminPaymentRefundIntent
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(128)]
        public string IdempotencyKey { get; set; } = string.Empty;

        public int RestaurantId { get; set; }

        [ForeignKey(nameof(RestaurantId))]
        public BillingAccount? BillingAccount { get; set; }

        /// <summary>Original payment order UUID (<c>SourcePaymentRef</c>).</summary>
        [MaxLength(128)]
        public string SourcePaymentOrderId { get; set; } = string.Empty;

        /// <summary>Refund order UUID returned by Merchant; null until call succeeds.</summary>
        [MaxLength(128)]
        public string? RefundOrderId { get; set; }

        /// <summary>Null means full refund (amount omitted on Merchant call).</summary>
        public int? AmountMinor { get; set; }

        public int ActorStaffUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
