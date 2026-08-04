using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Append-only recovery-offer fact issued on successful Send and issue offer.
    /// Includes the generated unique single-use redemption code. Does not change
    /// workflow status.
    /// </summary>
    public class FeedbackRecoveryOffer
    {
        public int Id { get; set; }

        public int FeedbackId { get; set; }

        public Feedback? Feedback { get; set; }

        public int? GuestResponseId { get; set; }

        public FeedbackGuestResponse? GuestResponse { get; set; }

        public FeedbackRecoveryOfferType OfferType { get; set; }

        [Required]
        [MaxLength(60)]
        public string Title { get; set; }
            = string.Empty;

        [Required]
        [MaxLength(240)]
        public string Description { get; set; }
            = string.Empty;

        public FeedbackRecoveryOfferValidity Validity { get; set; }

        /// <summary>Computed expiry instant (UTC) at issue time.</summary>
        public DateTime ExpiryAt { get; set; }

        public decimal? DiscountPercentage { get; set; }

        public decimal? DiscountAmount { get; set; }

        [MaxLength(200)]
        public string? FreeItemText { get; set; }

        public FeedbackRecoveryOfferPurchaseRequirement? PurchaseRequirement
        {
            get;
            set;
        }

        public decimal? MinimumSpend { get; set; }

        [MaxLength(500)]
        public string? AdditionalExclusions { get; set; }

        [MaxLength(200)]
        public string? ReplacementItemText { get; set; }

        [Required]
        [MaxLength(32)]
        public string RedemptionCode { get; set; }
            = string.Empty;

        [MaxLength(1000)]
        public string? StaffInstructions { get; set; }

        public FeedbackRecoveryIntent Intent { get; set; }

        public int? AuthorUserId { get; set; }

        public User? AuthorUser { get; set; }

        [Required]
        [MaxLength(150)]
        public string AuthorDisplayName { get; set; }
            = string.Empty;

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}
