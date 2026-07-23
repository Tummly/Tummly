using TummlyBackend.DTOs.Guests;

namespace TummlyBackend.Interfaces
{
    public interface IGuestActivityListService
    {
        /// <summary>
        /// Returns null when the Location Guest is missing for the Owned location.
        /// </summary>
        Task<GuestActivityListResponse?> ListAsync(
            int locationGuestId,
            int locationId,
            string locationName,
            string[]? types,
            string? datePreset,
            DateTime? dateFrom,
            DateTime? dateTo,
            string sort,
            int page,
            int pageSize,
            int utcOffsetMinutes,
            CancellationToken cancellationToken = default
        );
    }
}
