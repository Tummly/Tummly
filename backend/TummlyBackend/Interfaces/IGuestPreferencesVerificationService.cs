namespace TummlyBackend.Interfaces
{
    public enum GuestVerifyOutcome
    {
        Ok,
        Invalid,
        Expired,
    }

    /// <summary>
    /// Proves a guest's email or UK mobile for the guest preference centre (spec §6).
    /// Entry starts (no session) send only to a contact a Master Guest holds. Add-contact
    /// starts (<c>linkToTummlyGuestId</c> set) send to any contact.
    /// </summary>
    public interface IGuestPreferencesVerificationService
    {
        /// <summary>
        /// Sends a confirm link (email) or a 6-digit code (UK mobile). Never throws for an
        /// unknown or invalid contact. Silently does nothing when the contact is invalid, when
        /// an entry start has no matching Master Guest, or after 5 starts in the last hour.
        /// </summary>
        Task StartAsync(string rawContact, int? linkToTummlyGuestId, CancellationToken ct);

        /// <summary>
        /// Checks an SMS code for a UK mobile, then finds or creates the Tummly Guest, or
        /// links the contact to <paramref name="linkToTummlyGuestId"/>.
        /// </summary>
        Task<(GuestVerifyOutcome Outcome, ProvenContact? Proven)> VerifyCodeAsync(
            string rawContact,
            string code,
            int? linkToTummlyGuestId,
            CancellationToken ct
        );

        /// <summary>
        /// Checks an emailed confirm link (<c>&lt;otpId&gt;.&lt;code&gt;</c>). The row decides the flow:
        /// entry finds or creates the guest, add-contact links the contact to the row's owner.
        /// </summary>
        Task<(GuestVerifyOutcome Outcome, ProvenContact? Proven)> VerifyEmailLinkAsync(
            string v,
            CancellationToken ct
        );
    }
}
