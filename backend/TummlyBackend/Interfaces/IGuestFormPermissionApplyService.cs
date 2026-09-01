using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IGuestFormPermissionApplyService
    {
        /// <summary>
        /// Appends grant/withdraw ledger events for Enabled restaurant
        /// permissions (or all three withdraw when consent is declined) and
        /// syncs the legacy marketing preference rollup.
        /// </summary>
        Task ApplyOnSubmitAsync(
            LocationGuest locationGuest,
            Restaurant restaurant,
            int restaurantLocationId,
            bool consentGranted,
            DateTime occurredAt,
            CancellationToken cancellationToken = default
        );
    }
}
