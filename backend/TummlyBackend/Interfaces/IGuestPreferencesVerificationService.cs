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
        /// Checks an emailed entry link (<c>&lt;otpId&gt;.&lt;code&gt;</c>) and finds or creates the
        /// guest for that address. An add-contact row is Invalid here and is not consumed.
        /// </summary>
        Task<(GuestVerifyOutcome Outcome, ProvenContact? Proven)> VerifyEntryEmailLinkAsync(
            string v,
            CancellationToken ct
        );

        /// <summary>
        /// Checks an emailed add-contact link and links the address to the row's owner. Returns
        /// the ids only, with no session token. An entry row is Invalid here and is not consumed.
        /// </summary>
        Task<(GuestVerifyOutcome Outcome, ProvenContact? Proven)> VerifyAddContactEmailLinkAsync(
            string v,
            CancellationToken ct
        );
    }
}
