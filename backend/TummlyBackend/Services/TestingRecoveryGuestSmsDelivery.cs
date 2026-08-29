using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Accepts Recovery SMS sends in the Testing host without Twilio.
    /// </summary>
    public sealed class TestingRecoveryGuestSmsDelivery : IRecoveryGuestSmsDelivery
    {
        public Task<RecoveryGuestSmsDeliveryResult> SendAsync(
            string phoneNumber,
            string body,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<RecoveryGuestSmsDeliveryResult>(
                new RecoveryGuestSmsDeliveryResult.Accepted
                {
                    AcceptedSegments = Helpers.SmsSegmentEstimate.EstimateSegments(body),
                }
            );
        }
    }
}
