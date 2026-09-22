using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Routes Campaign fire outbound by channel — Email (Resend) and SMS (Twilio
    /// via <see cref="IRecoveryGuestSmsDelivery"/>).
    /// </summary>
    public sealed class CampaignOutboundChannelSender : ICampaignOutboundSender
    {
        private readonly ICampaignOutboundSender _email;
        private readonly IRecoveryGuestSmsDelivery _smsDelivery;

        public CampaignOutboundChannelSender(
            ICampaignOutboundSender email,
            IRecoveryGuestSmsDelivery smsDelivery
        )
        {
            _email = email;
            _smsDelivery = smsDelivery;
        }

        public async Task<CampaignOutboundSendResult> SendAsync(
            CampaignOutboundSendRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var channel = (request.Channel ?? string.Empty).Trim().ToLowerInvariant();
            if (channel == "email")
            {
                return await _email.SendAsync(request, cancellationToken);
            }

            if (channel == "sms")
            {
                return await SendSmsAsync(request, cancellationToken);
            }

            return new CampaignOutboundSendResult.Rejected
            {
                Message = $"Campaign outbound does not support channel '{channel}'.",
            };
        }

        private async Task<CampaignOutboundSendResult> SendSmsAsync(
            CampaignOutboundSendRequest request,
            CancellationToken cancellationToken
        )
        {
            var to = (request.ToAddress ?? string.Empty).Trim();
            if (to.Length == 0)
            {
                return new CampaignOutboundSendResult.Rejected
                {
                    Message = "SMS destination is required.",
                };
            }

            var delivery = await _smsDelivery.SendAsync(
                to,
                request.Body ?? string.Empty,
                cancellationToken
            );

            if (delivery is RecoveryGuestSmsDeliveryResult.Accepted accepted)
            {
                return new CampaignOutboundSendResult.Accepted
                {
                    AcceptedUnits = Math.Max(accepted.AcceptedSegments, 1),
                };
            }

            var message =
                delivery is RecoveryGuestSmsDeliveryResult.Failed failed
                    ? failed.Message
                    : "Unable to send Campaign SMS.";

            return new CampaignOutboundSendResult.Rejected
            {
                Message = message,
            };
        }
    }
}
