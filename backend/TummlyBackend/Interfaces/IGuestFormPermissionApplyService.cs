using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IGuestFormPermissionApplyService
    {
        /// <summary>
        /// Appends grant/withdraw ledger events for the contact channel's
        /// marketing permission (when Enabled) and Feedback follow-up when
        /// Enabled. Caller owns SaveChanges.
        /// </summary>
        Task ApplyOnSubmitAsync(
            LocationGuest locationGuest,
            Restaurant restaurant,
            int restaurantLocationId,
            string locationName,
            bool marketingConsentGranted,
            ContactType contactType,
            DateTime occurredAt,
            CancellationToken cancellationToken = default
        );
    }
}
