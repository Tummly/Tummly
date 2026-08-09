using TummlyBackend.DTOs.Campaigns;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Campaign list lifecycle actions — Unschedule, Pause, Cancel, Resume,
    /// Retry remaining, Duplicate (ticket 30).
    /// </summary>
    public interface ICampaignLifecycleService
    {
        Task<CampaignLifecycleResult> UnscheduleAsync(
            int campaignId,
            CampaignLifecycleActionRequest request,
            CancellationToken cancellationToken = default
        );

        Task<CampaignLifecycleResult> PauseAsync(
            int campaignId,
            CampaignLifecycleActionRequest request,
            CancellationToken cancellationToken = default
        );

        Task<CampaignLifecycleResult> CancelAsync(
            int campaignId,
            CampaignLifecycleActionRequest request,
            CancellationToken cancellationToken = default
        );

        Task<CampaignLifecycleResult> ResumeAsync(
            int campaignId,
            CampaignLifecycleActionRequest request,
            CancellationToken cancellationToken = default
        );

        Task<CampaignLifecycleResult> RetryRemainingAsync(
            int campaignId,
            CampaignLifecycleActionRequest request,
            CancellationToken cancellationToken = default
        );

        Task<CampaignLifecycleResult> DuplicateAsDraftAsync(
            int campaignId,
            CampaignLifecycleActionRequest request,
            CancellationToken cancellationToken = default
        );
    }
}
