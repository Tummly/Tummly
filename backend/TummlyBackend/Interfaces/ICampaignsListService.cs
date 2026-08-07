using TummlyBackend.DTOs.Campaigns;

namespace TummlyBackend.Interfaces
{
    public interface ICampaignsListService
    {
        Task<CampaignsListResponse> ListAsync(
            CampaignsListQuery query,
            CancellationToken cancellationToken = default
        );
    }
}
