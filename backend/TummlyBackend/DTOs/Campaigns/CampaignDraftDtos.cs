namespace TummlyBackend.DTOs.Campaigns
{
    public sealed class CreateCampaignDraftRequest
    {
        public int LocationId { get; init; }

        public string? Name { get; init; }

        public string? GoalId { get; init; }

        public string? TemplateId { get; init; }

        public int? TemplateVersion { get; init; }

        public string? AudienceKey { get; init; }

        public string? Channel { get; init; }

        public string? OfferStance { get; init; }

        /// <summary>Attached Offers catalog id; null when No offer.</summary>
        public int? OfferId { get; init; }

        public string? MessageSubject { get; init; }

        public string? MessageBody { get; init; }
    }

    /// <summary>
    /// Home recommendation click: create the Draft once, or return the open Draft
    /// for the same recommendation.
    /// </summary>
    public sealed class CreateRecommendationCampaignDraftRequest
    {
        /// <summary>Home campaign recommendation type (for example thank-recent-guests).</summary>
        public string RecommendationType { get; init; } = string.Empty;

        public CreateCampaignDraftRequest Draft { get; init; } = new();
    }

    public sealed record CampaignRecommendationDraftResult(CampaignDraftDto Campaign, bool Reused);

    public sealed class PatchCampaignDraftRequest
    {
        /// <summary>Base64 SQL rowversion from the last Draft read.</summary>
        public byte[] RowVersion { get; init; } = [];

        public string? Name { get; init; }

        public string? GoalId { get; init; }

        public string? TemplateId { get; init; }

        public int? TemplateVersion { get; init; }

        public string? AudienceKey { get; init; }

        public string? Channel { get; init; }

        public string? OfferStance { get; init; }

        /// <summary>Attached Offers catalog id; null when No offer / omitted.</summary>
        public int? OfferId { get; init; }

        public string? MessageSubject { get; init; }

        public string? MessageBody { get; init; }
    }

    public sealed class CampaignDraftDto
    {
        public int Id { get; init; }

        public int LocationId { get; init; }

        public string Status { get; init; } = "draft";

        public string Name { get; init; } = string.Empty;

        public string? GoalId { get; init; }

        public string? TemplateId { get; init; }

        public int? TemplateVersion { get; init; }

        public string? AudienceKey { get; init; }

        public string? Channel { get; init; }

        public string? OfferStance { get; init; }

        public int? OfferId { get; init; }

        /// <summary>Catalog Offer title when attached; null for No offer.</summary>
        public string? OfferTitle { get; init; }

        public string? MessageSubject { get; init; }

        public string? MessageBody { get; init; }

        /// <summary>Recent redeemers look-back in days; null when the audience does not use it.</summary>
        public int? AudienceLookbackDays { get; init; }

        /// <summary>IANA timezone of the look-back; null when the audience does not use it.</summary>
        public string? AudienceLookbackTimeZone { get; init; }

        /// <summary>Base64 SQL rowversion for optimistic concurrency.</summary>
        public byte[] RowVersion { get; init; } = [];

        public DateTime CreatedAt { get; init; }

        public DateTime UpdatedAt { get; init; }
    }

    public abstract class CampaignDraftWriteResult
    {
        private CampaignDraftWriteResult()
        {
        }

        public sealed class Ok : CampaignDraftWriteResult
        {
            public required CampaignDraftDto Campaign { get; init; }
        }

        public sealed class NotFound : CampaignDraftWriteResult
        {
        }

        public sealed class Conflict : CampaignDraftWriteResult
        {
        }

        /// <summary>
        /// Campaign exists but is not a Draft — distinct from concurrency Conflict.
        /// </summary>
        public sealed class NotDraft : CampaignDraftWriteResult
        {
        }
    }
}
