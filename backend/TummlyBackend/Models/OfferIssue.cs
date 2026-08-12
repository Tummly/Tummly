using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Guest pass created from an Offers catalog attach (Campaign Accepted or
    /// Guest form thank-you). Owns the unique Offer Claim code. Benefit fields
    /// are snapshots at issue time so later catalog edits do not rewrite passes.
    /// </summary>
    public class OfferIssue
    {
        public int Id { get; set; }

        public int CatalogOfferId { get; set; }

        public CatalogOffer? CatalogOffer { get; set; }

        public int LocationGuestId { get; set; }

        public LocationGuest? LocationGuest { get; set; }

        /// <summary>Unique redeemable Offer Claim code (TUM-######).</summary>
        [Required]
        [MaxLength(32)]
        public string ClaimCode { get; set; } = string.Empty;

        public DateTime IssuedAtUtc { get; set; }

        /// <summary>
        /// MVP Claim proxy: set once sticky (Campaign Accepted ≈ IssuedAt;
        /// thank-you paint target — submit sets ClaimedAt until open-tracking).
        /// </summary>
        public DateTime? ClaimedAtUtc { get; set; }

        /// <summary>
        /// Staff Redeem success time (ticket 38 Check / Mark as redeemed).
        /// </summary>
        public DateTime? RedeemedAtUtc { get; set; }

        /// <summary>
        /// Set when an approved void correction voids the redemption for KPI
        /// while RedeemedAtUtc remains for audit (ticket 39).
        /// </summary>
        public DateTime? RedemptionVoidedAtUtc { get; set; }

        /// <summary>
        /// Cancel / void time when the pass is no longer redeemable.
        /// </summary>
        public DateTime? CancelledAtUtc { get; set; }

        /// <summary>campaign | guest_form_thank_you</summary>
        [Required]
        [MaxLength(32)]
        public string Source { get; set; } = string.Empty;

        public int? CampaignId { get; set; }

        public Campaign? Campaign { get; set; }

        public int? FeedbackId { get; set; }

        public Feedback? Feedback { get; set; }

        public DateTime ExpiryAtUtc { get; set; }

        // --- Benefit snapshot (catalog definition at issue) ---

        public CatalogOfferType OfferType { get; set; }

        [Required]
        [MaxLength(60)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(240)]
        public string Description { get; set; } = string.Empty;

        public CatalogOfferValidity Validity { get; set; }

        public DateOnly? CustomExpiryDate { get; set; }

        public decimal? DiscountPercentage { get; set; }

        public decimal? DiscountAmount { get; set; }

        [MaxLength(200)]
        public string? FreeItemText { get; set; }

        public CatalogOfferPurchaseRequirement? PurchaseRequirement { get; set; }

        public decimal? MinimumSpend { get; set; }

        [MaxLength(500)]
        public string? AdditionalExclusions { get; set; }

        [MaxLength(200)]
        public string? ReplacementItemText { get; set; }

        [MaxLength(1000)]
        public string? StaffInstructions { get; set; }
    }

    public static class OfferIssueSources
    {
        public const string Campaign = "campaign";
        public const string GuestFormThankYou = "guest_form_thank_you";
        public const string Recovery = "recovery";
    }
}
