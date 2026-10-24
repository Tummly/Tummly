namespace TummlyBackend.Services
{
    /// <summary>
    /// Guest response email was claimed but must not send — guest has not
    /// granted feedback follow-up for a service reply on Email.
    /// </summary>
    public sealed class GuestResponseEmailNotEligibleException
        : InvalidOperationException
    {
        public GuestResponseEmailNotEligibleException(int guestResponseId)
            : base(
                $"Guest response {guestResponseId} skipped — service reply not eligible."
            )
        {
            GuestResponseId = guestResponseId;
        }

        public int GuestResponseId { get; }
    }
}
