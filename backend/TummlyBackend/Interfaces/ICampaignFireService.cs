using TummlyBackend.DTOs.Campaigns;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Fire / send execution: drop-only revalidate, settle accepted, cannot-start Failed
    /// (ticket 31).
    /// </summary>
    public interface ICampaignFireService
    {
        Task<CampaignFireResult> FireAsync(
            int campaignId,
            CancellationToken cancellationToken = default
        );
    }
}
