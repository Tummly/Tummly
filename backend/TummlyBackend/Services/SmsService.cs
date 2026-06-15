using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Development stub for Sign-in SMS OTP. Replace with a real provider (e.g. Twilio)
    /// when production credentials are available.
    /// </summary>
    public class SmsService : ISmsService
    {
        private readonly ILogger<SmsService> _logger;

        public SmsService(ILogger<SmsService> logger)
        {
            _logger = logger;
        }

        public Task SendOtpSmsAsync(
            string phoneNumber,
            string otp
        )
        {
            _logger.LogInformation(
                "[SMS STUB] Sign-in OTP {Otp} would be sent to {Phone}. " +
                "Wire a real SMS provider before production.",
                otp,
                phoneNumber
            );

            return Task.CompletedTask;
        }
    }
}
