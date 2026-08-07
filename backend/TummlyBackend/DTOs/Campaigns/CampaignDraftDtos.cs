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

        public string? MessageSubject { get; init; }

        public string? MessageBody { get; init; }
    }

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

        public string? MessageSubject { get; init; }

        public string? MessageBody { get; init; }

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
