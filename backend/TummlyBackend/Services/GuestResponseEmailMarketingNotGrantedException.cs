namespace TummlyBackend.Services
{
    /// <summary>
    /// Guest response email was claimed but must not send — guest has not
    /// granted email-marketing for the Email channel.
    /// </summary>
    public sealed class GuestResponseEmailMarketingNotGrantedException
        : InvalidOperationException
    {
        public GuestResponseEmailMarketingNotGrantedException(int guestResponseId)
            : base(
                $"Guest response {guestResponseId} skipped — email marketing not granted."
            )
        {
            GuestResponseId = guestResponseId;
        }

        public int GuestResponseId { get; }
    }
}
