namespace TummlyBackend.Models
{
    /// <summary>
    /// Operator-visible delivery state for a <see cref="FeedbackGuestResponse"/>
    /// reply. Forward-only: Pending → Accepted → Delivered | Failed. Delivered and
    /// Failed are final. Unavailable means the provider gave no status callback.
    /// </summary>
    public enum GuestReplyDeliveryState
    {
        /// <summary>Reply saved; provider has not accepted it yet.</summary>
        Pending = 0,

        /// <summary>Provider accepted the reply for sending.</summary>
        Accepted = 1,

        /// <summary>Provider reports the reply was delivered.</summary>
        Delivered = 2,

        /// <summary>Reply was not confirmed as sent.</summary>
        Failed = 3,

        /// <summary>No delivery status is available from the provider.</summary>
        Unavailable = 4,
    }
}
