namespace TummlyBackend.DTOs.Offers
{
    public sealed class CreateCatalogOfferRequest
    {
        public int LocationId { get; init; }

        public string OfferType { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string Validity { get; init; } = string.Empty;

        /// <summary>ISO date (yyyy-MM-dd) when validity is choose_expiry_date.</summary>
        public string? ExpiryDate { get; init; }

        public decimal? DiscountPercentage { get; init; }

        public decimal? DiscountAmount { get; init; }

        public string? FreeItemText { get; init; }

        public string? PurchaseRequirement { get; init; }

        public decimal? MinimumSpend { get; init; }

        public string? AdditionalExclusions { get; init; }

        public string? ReplacementItemText { get; init; }

        public string? StaffInstructions { get; init; }
    }

    public sealed class CatalogOfferDto
    {
        public int Id { get; init; }

        public int LocationId { get; init; }

        public string Status { get; init; } = "active";

        public string OfferType { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string Validity { get; init; } = string.Empty;

        public string? ExpiryDate { get; init; }

        public decimal? DiscountPercentage { get; init; }

        public decimal? DiscountAmount { get; init; }

        public string? FreeItemText { get; init; }

        public string? PurchaseRequirement { get; init; }

        public decimal? MinimumSpend { get; init; }

        public string? AdditionalExclusions { get; init; }

        public string? ReplacementItemText { get; init; }

        public string? StaffInstructions { get; init; }

        /// <summary>
        /// Count of OfferIssue rows for this catalog definition (soft-confirm gate).
        /// </summary>
        public int IssueCount { get; init; }

        public DateTime CreatedAt { get; init; }

        public DateTime UpdatedAt { get; init; }
    }
}
