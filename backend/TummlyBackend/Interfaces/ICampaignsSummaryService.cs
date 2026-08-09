using TummlyBackend.DTOs.Campaigns;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Overview Campaign summary KPIs — in-flight (status only) + accepted
    /// messages in the overview window (ticket 29).
    /// </summary>
    public interface ICampaignsSummaryService
    {
        Task<CampaignsSummaryDto> GetSummaryAsync(
            CampaignsSummaryQuery query,
            CancellationToken cancellationToken = default
        );
    }
}
