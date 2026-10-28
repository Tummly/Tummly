using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Staff manual credit adjustment or reversal, keyed by the request's
    /// <c>Idempotency-Key</c> (ADM-07-07 / ADM-18-03). A replay with the same key and
    /// body returns this stored first result and writes no ledger row. The same key with
    /// a different body is a conflict. Written in the same transaction as the ledger rows.
    /// </summary>
    public class StaffCreditAdjustmentIntent
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(128)]
        public string IdempotencyKey { get; set; } = string.Empty;

        /// <summary>SHA-256 hex of the operation and normalised request body.</summary>
        [MaxLength(64)]
        public string RequestHash { get; set; } = string.Empty;

        /// <summary><see cref="StaffCreditAdjustmentOperations"/> value.</summary>
        [MaxLength(16)]
        public string Operation { get; set; } = string.Empty;

        /// <summary>Plain int, no foreign key: the intent outlives nothing and cascades nothing.</summary>
        public int RestaurantId { get; set; }

        public int ActorStaffUserId { get; set; }

        public int Quantity { get; set; }

        [MaxLength(500)]
        public string EvidenceReference { get; set; } = string.Empty;

        /// <summary>Combined pool remaining for the channel after the write.</summary>
        public int BalanceAfter { get; set; }

        /// <summary>Ledger row written by a reversal. Null for manual adjustments.</summary>
        public Guid? InsertedEntryId { get; set; }

        /// <summary>Allocation of the reversal row, as returned on replay.</summary>
        public Guid? InsertedAllocationId { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public static class StaffCreditAdjustmentOperations
    {
        public const string ManualAdjust = "manual_adjust";

        public const string Reversal = "reversal";
    }
}
