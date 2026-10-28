namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// One provider send for the guest preference centre. <c>ContactType</c> is
    /// the <c>TummlyGuestContactTypes</c> value. <c>ConfirmUrl</c> is set for email only.
    /// </summary>
    public sealed record GuestPreferencesSendItem(
        string ContactType,
        string Destination,
        string? ConfirmUrl
    );

    /// <summary>
    /// Hands a provider send to a background worker, so the request never waits on the provider.
    /// </summary>
    public interface IGuestPreferencesSendQueue
    {
        /// <summary>Returns false when the queue is full and the item was dropped.</summary>
        bool TryEnqueue(GuestPreferencesSendItem item);
    }
}
