using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Restore rule (D5). Decides whether a Tummly restore may grant one
    /// Location Guest and permission kind again. Pure: no I/O.
    /// </summary>
    public static class GuestPreferenceRestoreRule
    {
        /// <summary>
        /// True only when the guest granted this kind at this location before,
        /// and the latest ledger row is a withdraw made by the guest
        /// (<c>email-unsubscribe</c> or <c>preference-centre</c>). Bounce,
        /// complaint, operator, SMS STOP, suppression, and legacy withdraws
        /// never restore.
        /// </summary>
        /// <param name="history">The Location Guest's ledger rows for one kind, in any order.</param>
        public static bool CanRestore(IReadOnlyList<LocationGuestPermissionLedgerEntry> history)
        {
            ArgumentNullException.ThrowIfNull(history);

            if (
                !history.Any(e =>
                    e.EventKind == LocationGuestPermissionLedgerEventKinds.Grant
                )
            )
            {
                return false;
            }

            var latest = history
                .OrderByDescending(e => e.OccurredAt)
                .ThenByDescending(e => e.Id)
                .First();

            return latest.EventKind == LocationGuestPermissionLedgerEventKinds.Withdraw
                && (
                    latest.Source == LocationGuestPermissionLedgerSources.EmailUnsubscribe
                    || latest.Source == LocationGuestPermissionLedgerSources.PreferenceCentre
                );
        }
    }
}
