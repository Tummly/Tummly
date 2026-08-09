using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Default Billing Reserve — not live until Phase A ledger ships.
    /// Campaign schedule / send confirm hard-blocks while this is registered.
    /// </summary>
    public sealed class UnavailableCampaignBillingReserve : ICampaignBillingReserve
    {
        public bool IsLive => false;

        public Task<CampaignBillingReserveResult> ReserveAsync(
            CampaignBillingReserveRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<CampaignBillingReserveResult>(
                new CampaignBillingReserveResult.Failed
                {
                    Message =
                        "Billing Reserve is not available. Schedule and send stay blocked.",
                }
            );
        }

        public Task<CampaignBillingSettleResult> SettleAsync(
            CampaignBillingSettleRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<CampaignBillingSettleResult>(
                new CampaignBillingSettleResult.Failed
                {
                    Message = "Billing Settle is not available.",
                }
            );
        }

        public Task<CampaignBillingReleaseResult> ReleaseAsync(
            CampaignBillingReleaseRequest request,
            CancellationToken cancellationToken = default
        )
        {
            // Soft no-op while Reserve is unavailable — lifecycle release paths
            // still clear local reservation refs (ticket 30).
            return Task.FromResult<CampaignBillingReleaseResult>(
                new CampaignBillingReleaseResult.Ok()
            );
        }
    }
}
