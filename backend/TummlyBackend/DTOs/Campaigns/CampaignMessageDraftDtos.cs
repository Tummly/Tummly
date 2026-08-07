namespace TummlyBackend.DTOs.Campaigns
{
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
