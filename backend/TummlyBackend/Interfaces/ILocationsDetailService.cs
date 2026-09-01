using TummlyBackend.DTOs.Locations;

namespace TummlyBackend.Interfaces
{
    public interface ILocationsDetailService
    {
        /// <summary>
        /// Returns detail for a scoped Owned location, or null when the id does not
        /// exist for the restaurant.
        /// </summary>
        Task<LocationDetailResponseDto?> GetDetailAsync(LocationDetailQuery query);
    }
}
