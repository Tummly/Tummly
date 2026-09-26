namespace TummlyBackend.DTOs.Campaigns
{
    /// <summary>
    /// Confirmed catalog Offer facts for Campaign message-draft AI.
    /// Mirrors recovery offer payload shape; no live redemption codes.
    /// </summary>
    public sealed class CampaignMessageDraftOfferPayloadDto
    {
        public string OfferType { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Validity { get; set; } = string.Empty;

        /// <summary>ISO date (yyyy-MM-dd) when validity is choose_expiry_date.</summary>
        public string? ExpiryDate { get; set; }

        public decimal? DiscountPercentage { get; set; }

        public decimal? DiscountAmount { get; set; }

        public string? FreeItemText { get; set; }

        public string? PurchaseRequirement { get; set; }

        public decimal? MinimumSpend { get; set; }

        public string? AdditionalExclusions { get; set; }

        public string? ReplacementItemText { get; set; }
    }

    public sealed class PrepareCampaignMessageDraftRequest
    {
        public int LocationId { get; init; }

        public string Channel { get; init; } = string.Empty;

        public string GoalId { get; init; } = string.Empty;

        public string AudienceKey { get; init; } = string.Empty;

        public string OfferStance { get; init; } = string.Empty;

        public string? CampaignName { get; init; }

        public string Tone { get; init; } = string.Empty;

        public string? IncludeNotes { get; init; }

        public string Mode { get; init; } = "prepare";

        public string? CurrentBody { get; init; }

        public string? CurrentSubject { get; init; }

        public CampaignMessageDraftOfferPayloadDto? ConfirmedOffer { get; init; }
    }

    public abstract record CampaignMessageDraftServiceResult
    {
        private CampaignMessageDraftServiceResult()
        {
        }

        public sealed record Ok(
            string Body,
            string? Subject,
            string Channel
        ) : CampaignMessageDraftServiceResult;

        public sealed record Failed(string Message, bool Retryable)
            : CampaignMessageDraftServiceResult;
    }
}
