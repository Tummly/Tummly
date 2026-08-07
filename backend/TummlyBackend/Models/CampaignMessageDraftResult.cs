namespace TummlyBackend.Models
{
    /// <summary>
    /// Inputs for Campaign message-draft AI.
    /// Must never include guest email/phone or other guest PII.
    /// </summary>
    public sealed record CampaignMessageDraftInput(
        string LocationName,
        string Channel,
        string GoalId,
        string AudienceKey,
        string OfferStance,
        string? CampaignName,
        string Tone,
        string? IncludeNotes,
        string Mode,
        string? CurrentBody,
        string? CurrentSubject
    );

    public abstract record CampaignMessageDraftProviderResult
    {
        private CampaignMessageDraftProviderResult()
        {
        }

        public sealed record Succeeded(
            string Body,
            string? Subject,
            string Channel
        ) : CampaignMessageDraftProviderResult;

        public sealed record Failed(bool Retryable = true)
            : CampaignMessageDraftProviderResult;
    }
}
