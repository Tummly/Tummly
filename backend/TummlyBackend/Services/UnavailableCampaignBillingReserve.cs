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
    }
}
