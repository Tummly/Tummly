namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Soft-lock / channel hard-stop / dunning gate at fire (ticket 31).
    /// Default clears until Billing Soft-lock APIs exist.
    /// </summary>
    public interface ICampaignSendStartGate
    {
        Task<CampaignSendStartGateResult> EvaluateAsync(
            int campaignId,
            int locationId,
            CancellationToken cancellationToken = default
        );
    }

    public abstract class CampaignSendStartGateResult
    {
        private CampaignSendStartGateResult()
        {
        }

        public sealed class Clear : CampaignSendStartGateResult
        {
        }

        public sealed class SoftLocked : CampaignSendStartGateResult
        {
        }

        public sealed class ChannelHardStopped : CampaignSendStartGateResult
        {
        }

        public sealed class Blocked : CampaignSendStartGateResult
        {
            public required string Message { get; init; }
        }
    }
}
