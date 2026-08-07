using TummlyBackend.DTOs.Campaigns;

namespace TummlyBackend.Interfaces
{
    public interface ICampaignDraftService
    {
        Task<CampaignDraftDto> CreateAsync(
            CreateCampaignDraftRequest request,
            CancellationToken cancellationToken = default
        );

        Task<CampaignDraftDto?> GetByIdAsync(
            int campaignId,
            CancellationToken cancellationToken = default
        );

        Task<CampaignDraftWriteResult> PatchAsync(
            int campaignId,
            PatchCampaignDraftRequest request,
            CancellationToken cancellationToken = default
        );
    }
}
