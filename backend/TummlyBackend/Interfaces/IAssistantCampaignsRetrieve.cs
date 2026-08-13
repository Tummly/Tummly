using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IAssistantCampaignsRetrieve
    {
        Task<AssistantCampaignsRetrieveResult> RetrieveAsync(
            int ownedLocationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );
    }

    public abstract record AssistantCampaignsRetrieveResult
    {
        public sealed record Ok(AssistantCampaignsEvidence Evidence)
            : AssistantCampaignsRetrieveResult;

        public sealed record Failed() : AssistantCampaignsRetrieveResult;
    }
}
