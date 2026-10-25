using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Verify.V2.Service;
using TummlyBackend.Configurations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public class TwilioVerifySmsService : ISmsService
    {
        private readonly TwilioSettings _settings;
        private readonly ILogger<TwilioVerifySmsService> _logger;
        private readonly DemoSideEffectGuard _demoSideEffects;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TwilioVerifySmsService(
            IOptions<TwilioSettings> settings,
            ILogger<TwilioVerifySmsService> logger,
            DemoSideEffectGuard demoSideEffects,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _settings = settings.Value;
            _logger = logger;
            _demoSideEffects = demoSideEffects;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task SendOtpSmsAsync(string phoneNumber)
        {
            if (
                DemoSessionClaims.IsDemoSession(_httpContextAccessor.HttpContext?.User)
                || _demoSideEffects.ShouldSuppressDestination(phoneNumber)
            )
            {
                _logger.LogInformation(
                    "Demo side-effect suppression: skipped Twilio Verify OTP to {MaskedPhone}",
                    LogContactMask.Phone(phoneNumber)
                );
                return;
            }

            EnsureConfigured();

            var e164 = ToE164(phoneNumber);

            try
            {
                var verification = await VerificationResource.CreateAsync(
                    to: e164,
                    channel: "sms",
                    pathServiceSid: _settings.VerifyServiceSid
                );

                if (
                    !string.Equals(
                        verification.Status,
                        "pending",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    throw new InvalidOperationException(
                        "Unable to send SMS verification code."
                    );
                }
            }
            catch (ApiException ex)
            {
                _logger.LogError(
                    "Twilio Verify send failed for {MaskedPhone} ExceptionType={ExceptionType} Code={Code} Message={ScrubbedMessage}",
                    LogContactMask.Phone(e164),
                    ex.GetType().Name,
                    ex.Code,
                    LogContactMask.Scrub(ex.Message)
                );

                throw new InvalidOperationException(
                    MapTwilioError(ex),
                    ex
                );
            }
        }

        public async Task<bool> VerifyOtpSmsAsync(
            string phoneNumber,
            string otp
        )
        {
            EnsureConfigured();

            var e164 = ToE164(phoneNumber);
            var code = otp.Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            try
            {
                var check = await VerificationCheckResource.CreateAsync(
                    to: e164,
                    code: code,
                    pathServiceSid: _settings.VerifyServiceSid
                );

                return string.Equals(
                    check.Status,
                    "approved",
                    StringComparison.OrdinalIgnoreCase
                );
            }
            catch (ApiException ex) when (ex.Status == 404)
            {
                return false;
            }
            catch (ApiException ex)
            {
                _logger.LogWarning(
                    "Twilio Verify check failed for {MaskedPhone} ExceptionType={ExceptionType} Code={Code} Message={ScrubbedMessage}",
                    LogContactMask.Phone(e164),
                    ex.GetType().Name,
                    ex.Code,
                    LogContactMask.Scrub(ex.Message)
                );

                throw new InvalidOperationException(
                    MapTwilioError(ex),
                    ex
                );
            }
        }

        private void EnsureConfigured()
        {
            if (_settings.IsConfigured)
            {
                TwilioClient.Init(
                    _settings.AccountSid,
                    _settings.AuthToken
                );
                return;
            }

            throw new InvalidOperationException(
                "SMS verification is not configured. Set TwilioSettings__AccountSid, "
                + "TwilioSettings__AuthToken, and TwilioSettings__VerifyServiceSid."
            );
        }

        private string ToE164(string phoneNumber)
        {
            return PhoneNumberHelper.NormalizeToE164(
                phoneNumber,
                _settings.ResolvedDefaultRegion
            );
        }

        private static string MapTwilioError(ApiException ex)
        {
            return ex.Code switch
            {
                60200 =>
                    "The phone number on file is not valid for SMS verification.",
                60202 =>
                    "Too many verification attempts. Try again later.",
                60203 =>
                    "Too many verification codes sent. Try again later.",
                60212 =>
                    "Too many verification attempts. Try again later.",
                20429 =>
                    "Too many requests. Try again shortly.",
                _ =>
                    "Unable to complete SMS verification. Try again shortly.",
            };
        }
    }
}
