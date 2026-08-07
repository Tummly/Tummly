using TummlyBackend.DTOs.Campaigns;

namespace TummlyBackend.Interfaces
{
    public interface ICampaignDraftService
    {
        Task<CampaignDraftDto> CreateAsync(
            CreateCampaignDraftRequest request,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Returns a Draft by id, or null when missing or not a Draft.
        /// </summary>
        Task<CampaignDraftDto?> GetByIdAsync(
            int campaignId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Location id for any Campaign status — ownership checks before PATCH.
        /// </summary>
        Task<int?> GetLocationIdAsync(
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
