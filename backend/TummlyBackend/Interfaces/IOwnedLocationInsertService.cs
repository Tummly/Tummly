using TummlyBackend.DTOs.Locations;

namespace TummlyBackend.Interfaces
{
    public interface IOwnedLocationInsertService
    {
        Task<AddOwnedLocationResult> AddAsync(
            int restaurantId,
            int actorUserId,
            AddOwnedLocationRequest request
        );

        Task<ImportOwnedLocationsResult> ImportAsync(
            int restaurantId,
            int actorUserId,
            ImportOwnedLocationsRequest request
        );
    }
}
