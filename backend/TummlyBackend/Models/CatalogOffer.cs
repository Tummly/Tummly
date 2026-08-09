using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Reusable Offers catalog definition for Campaigns (ticket 22).
    /// Distinct from FeedbackRecoveryOffer (issued one-off on Feedback).
    /// </summary>
    public class CatalogOffer
    {
        public int Id { get; set; }

        public int RestaurantLocationId { get; set; }

        public RestaurantLocation? RestaurantLocation { get; set; }

        /// <summary>Always "active" on create in MVP.</summary>
        [Required]
        [MaxLength(32)]
        public string Status { get; set; } = "active";

        public CatalogOfferType OfferType { get; set; }

        [Required]
        [MaxLength(60)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(240)]
        public string Description { get; set; } = string.Empty;

        public CatalogOfferValidity Validity { get; set; }

        /// <summary>Set when Validity is ChooseExpiryDate.</summary>
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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
