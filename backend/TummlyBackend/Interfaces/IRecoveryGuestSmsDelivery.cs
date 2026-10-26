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

            /// <summary>Twilio message SID. Null when nothing was sent (demo suppression).</summary>
            public string? ProviderMessageId { get; init; }

            /// <summary>
            /// True when a status callback URL was sent with the message. Without one
            /// no delivery event can arrive, so a guest reply stays Unavailable.
            /// </summary>
            public bool StatusCallbackSet { get; init; }
        }

        public sealed class Failed : RecoveryGuestSmsDeliveryResult
        {
            public required string Message { get; init; }
        }
    }
}
