namespace TummlyBackend.Models
{
    /// <summary>
    /// Durable delivery state for <see cref="FeedbackGuestResponse"/> email sends
    /// (ADR-0026). Not an operator-facing status.
    /// </summary>
    public enum GuestResponseEmailDeliveryStatus
    {
        /// <summary>SMS or non-email channel — no Resend work.</summary>
        NotApplicable = 0,

        /// <summary>Email fact saved; Resend not yet accepted.</summary>
        Pending = 1,

        /// <summary>Resend accepted the mail.</summary>
        Accepted = 2,
    }
}
