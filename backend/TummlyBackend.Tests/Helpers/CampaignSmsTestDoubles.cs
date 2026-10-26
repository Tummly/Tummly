using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// SMS provider double. Each call runs the next scripted step. A step that
    /// throws makes the call fail with that exception. When the script is empty,
    /// the call is accepted.
    /// </summary>
    public sealed class ScriptedCampaignSmsDelivery : IRecoveryGuestSmsDelivery
    {
        private readonly Queue<Func<RecoveryGuestSmsDeliveryResult>> _script;
        private int _acceptedCount;

        public ScriptedCampaignSmsDelivery(
            params Func<RecoveryGuestSmsDeliveryResult>[] script
        )
        {
            _script = new Queue<Func<RecoveryGuestSmsDeliveryResult>>(script);
        }

        public List<string> Phones { get; } = [];

        public async Task<RecoveryGuestSmsDeliveryResult> SendAsync(
            string phoneNumber,
            string body,
            CancellationToken cancellationToken = default
        )
        {
            Phones.Add(phoneNumber);
            await Task.Yield();
            if (_script.Count > 0)
            {
                return _script.Dequeue()();
            }

            _acceptedCount++;
            return new RecoveryGuestSmsDeliveryResult.Accepted
            {
                AcceptedSegments = 1,
                ProviderMessageId = $"SMscripted-{_acceptedCount:D4}",
                StatusCallbackSet = true,
            };
        }
    }

    /// <summary>Billing reserve that is always live and settles and releases without error.</summary>
    public sealed class PassiveCampaignBillingReserve : ICampaignBillingReserve
    {
        public bool IsLive => true;

        public Task<CampaignBillingReserveResult> ReserveAsync(
            CampaignBillingReserveRequest request,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<CampaignBillingSettleResult> SettleAsync(
            CampaignBillingSettleRequest request,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<CampaignBillingSettleResult>(new CampaignBillingSettleResult.Ok());

        public Task<CampaignBillingReleaseResult> ReleaseAsync(
            CampaignBillingReleaseRequest request,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<CampaignBillingReleaseResult>(new CampaignBillingReleaseResult.Ok());
    }

    /// <summary>Send gate that lets every fire start.</summary>
    public sealed class ClearCampaignSendStartGateDouble : ICampaignSendStartGate
    {
        public Task<CampaignSendStartGateResult> EvaluateAsync(
            int campaignId,
            int locationId,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<CampaignSendStartGateResult>(new CampaignSendStartGateResult.Clear());
    }

    /// <summary>Email sender that must not run in SMS-only tests.</summary>
    public sealed class UnusedCampaignEmailOutbound : ICampaignOutboundSender
    {
        public Task<CampaignOutboundSendResult> SendAsync(
            CampaignOutboundSendRequest request,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException("Email is not used in SMS tests.");
    }
}
