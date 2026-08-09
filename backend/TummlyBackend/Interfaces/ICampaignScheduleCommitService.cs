using TummlyBackend.DTOs.Campaigns;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Atomic Campaign schedule / send commit: freeze + Billing Reserve + status.
    /// </summary>
    public interface ICampaignScheduleCommitService
    {
        Task<CampaignScheduleCommitResult> CommitAsync(
            int campaignId,
            CommitCampaignScheduleRequest request,
            CancellationToken cancellationToken = default
        );
    }
}
