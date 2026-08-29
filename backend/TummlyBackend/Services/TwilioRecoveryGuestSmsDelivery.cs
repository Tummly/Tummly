using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using TummlyBackend.Configurations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class TwilioRecoveryGuestSmsDelivery : IRecoveryGuestSmsDelivery
    {
        private readonly TwilioSettings _settings;
        private readonly ILogger<TwilioRecoveryGuestSmsDelivery> _logger;

        public TwilioRecoveryGuestSmsDelivery(
            IOptions<TwilioSettings> settings,
            ILogger<TwilioRecoveryGuestSmsDelivery> logger
        )
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<RecoveryGuestSmsDeliveryResult> SendAsync(
            string phoneNumber,
            string body,
            CancellationToken cancellationToken = default
        )
        {
            if (!_settings.IsRecoverySmsConfigured)
            {
                return new RecoveryGuestSmsDeliveryResult.Failed
                {
                    Message = "Recovery SMS is not configured.",
                };
            }

            TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
            var e164 = PhoneNumberHelper.NormalizeToE164(
                phoneNumber,
                _settings.ResolvedDefaultRegion
            );

            try
            {
                var message = await MessageResource.CreateAsync(
                    to: new Twilio.Types.PhoneNumber(e164),
                    from: new Twilio.Types.PhoneNumber(_settings.RecoveryFromNumber),
                    body: body
                );

                var segments = message.NumSegments ?? "1";
                if (!int.TryParse(segments, out var accepted) || accepted <= 0)
                {
                    accepted = 1;
                }

                return new RecoveryGuestSmsDeliveryResult.Accepted
                {
                    AcceptedSegments = accepted,
                };
            }
            catch (ApiException ex)
            {
                _logger.LogError(
                    ex,
                    "Twilio Recovery SMS send failed for {Phone}",
                    MaskForLogs(e164)
                );

                return new RecoveryGuestSmsDeliveryResult.Failed
                {
                    Message = "Unable to send Recovery SMS.",
                };
            }
        }

        private static string MaskForLogs(string e164)
        {
            if (e164.Length <= 4)
            {
                return "••••";
            }

            return $"••••{e164[^4..]}";
        }
    }
}
