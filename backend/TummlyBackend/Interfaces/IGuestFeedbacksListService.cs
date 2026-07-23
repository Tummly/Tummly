using TummlyBackend.DTOs.Guests;

namespace TummlyBackend.Interfaces
{
    public interface IGuestFeedbacksListService
    {
        /// <summary>
        /// Returns null when the Location Guest is missing for the Owned location.
        /// </summary>
        Task<GuestFeedbacksListResponse?> ListAsync(
            int locationGuestId,
            int locationId,
            string locationName,
            string? q,
            string[]? sentiment,
            string[]? detectedTags,
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
