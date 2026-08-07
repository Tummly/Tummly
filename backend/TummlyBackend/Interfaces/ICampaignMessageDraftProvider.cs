using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface ICampaignMessageDraftProvider
    {
        Task<CampaignMessageDraftProviderResult> DraftAsync(
            CampaignMessageDraftInput input,
            CancellationToken cancellationToken = default
        );
    }
}
