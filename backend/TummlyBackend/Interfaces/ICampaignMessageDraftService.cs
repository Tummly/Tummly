using TummlyBackend.DTOs.Campaigns;

namespace TummlyBackend.Interfaces
{
    public interface ICampaignMessageDraftService
    {
        Task<CampaignMessageDraftServiceResult> PrepareAsync(
            string locationName,
            PrepareCampaignMessageDraftRequest request,
            CancellationToken cancellationToken = default
        );
    }
}
