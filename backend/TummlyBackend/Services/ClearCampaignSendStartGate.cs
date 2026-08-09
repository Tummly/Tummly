using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Default fire gate — Soft-lock / hard-stop clear until Billing APIs exist.
    /// </summary>
    public sealed class ClearCampaignSendStartGate : ICampaignSendStartGate
    {
        public Task<CampaignSendStartGateResult> EvaluateAsync(
            int campaignId,
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<CampaignSendStartGateResult>(
                new CampaignSendStartGateResult.Clear()
            );
        }
    }
}
