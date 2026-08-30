namespace TummlyBackend.Interfaces
{
    public interface IRecoveryGuestSmsDelivery
    {
        Task<RecoveryGuestSmsDeliveryResult> SendAsync(
            string phoneNumber,
            string body,
            CancellationToken cancellationToken = default
        );
    }

    public abstract class RecoveryGuestSmsDeliveryResult
    {
        private RecoveryGuestSmsDeliveryResult()
        {
        }

        public sealed class Accepted : RecoveryGuestSmsDeliveryResult
        {
            public required int AcceptedSegments { get; init; }
        }

        public sealed class Failed : RecoveryGuestSmsDeliveryResult
        {
            public required string Message { get; init; }
        }
    }
}
