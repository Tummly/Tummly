using Microsoft.Extensions.Configuration;
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
        private readonly DemoSideEffectGuard _demoSideEffects;
        private readonly IConfiguration _configuration;

        public TwilioRecoveryGuestSmsDelivery(
            IOptions<TwilioSettings> settings,
            ILogger<TwilioRecoveryGuestSmsDelivery> logger,
            DemoSideEffectGuard demoSideEffects,
            IConfiguration configuration
        )
        {
            _settings = settings.Value;
            _logger = logger;
            _demoSideEffects = demoSideEffects;
            _configuration = configuration;
        }

        public async Task<RecoveryGuestSmsDeliveryResult> SendAsync(
            string phoneNumber,
            string body,
            CancellationToken cancellationToken = default
        )
        {
            if (_demoSideEffects.ShouldSuppressDestination(phoneNumber))
            {
                _logger.LogInformation(
                    "Demo side-effect suppression: skipped Recovery SMS to {MaskedPhone}",
                    LogContactMask.Phone(phoneNumber)
                );
                // Nothing was sent, so there is no SID and no status callback.
                return new RecoveryGuestSmsDeliveryResult.Accepted
                {
                    AcceptedSegments = 1,
                    ProviderMessageId = null,
                    StatusCallbackSet = false,
                };
            }

            if (!_settings.IsRecoverySmsConfigured)
            {
                return new RecoveryGuestSmsDeliveryResult.Failed
                {
                    Message = "Recovery SMS is not configured.",
                };
            }

            // Defence in depth: the reserved test range never reaches Twilio
            // unless GuestContact:AllowReservedTestNumbers is on.
            if (
                PhoneNumberHelper.IsReservedTestRange(phoneNumber)
                && !ReservedTestNumberPolicy.AllowReservedTestNumbers
            )
            {
                _logger.LogWarning(
                    "Refused Recovery SMS to reserved test range {MaskedPhone}",
                    LogContactMask.Phone(phoneNumber)
                );
                return new RecoveryGuestSmsDeliveryResult.Failed
                {
                    Message = "Unable to send Recovery SMS.",
                };
            }

            TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
            var e164 = PhoneNumberHelper.NormalizeToE164(
                phoneNumber,
                _settings.ResolvedDefaultRegion
            );

            var statusCallback = TwilioWebhookUrl.For(
                _configuration,
                TwilioWebhookUrl.SmsStatusPath
            );

            try
            {
                var message = await MessageResource.CreateAsync(
                    to: new Twilio.Types.PhoneNumber(e164),
                    from: new Twilio.Types.PhoneNumber(_settings.RecoveryFromNumber),
                    body: body,
                    statusCallback: statusCallback == null ? null : new Uri(statusCallback)
                );

                var segments = message.NumSegments ?? "1";
                if (!int.TryParse(segments, out var accepted) || accepted <= 0)
                {
                    accepted = 1;
                }

                return new RecoveryGuestSmsDeliveryResult.Accepted
                {
                    AcceptedSegments = accepted,
                    ProviderMessageId = message.Sid,
                    StatusCallbackSet = statusCallback != null,
                };
            }
            catch (ApiException ex)
            {
                _logger.LogProviderSendFailure(
                    ex,
                    "Twilio Recovery SMS send failed for {MaskedPhone}",
                    LogContactMask.Phone(e164)
                );

                return new RecoveryGuestSmsDeliveryResult.Failed
                {
                    Message = "Unable to send Recovery SMS.",
                };
            }
        }
    }
}
