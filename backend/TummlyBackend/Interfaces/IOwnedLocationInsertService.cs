using TummlyBackend.DTOs.Locations;

namespace TummlyBackend.Interfaces
{
    public interface IOwnedLocationInsertService
    {
        Task<AddOwnedLocationResult> AddAsync(
            int restaurantId,
            AddOwnedLocationRequest request
        );
    }
}
