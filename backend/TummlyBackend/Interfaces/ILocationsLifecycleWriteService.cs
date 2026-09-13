using TummlyBackend.DTOs.Locations;

namespace TummlyBackend.Interfaces
{
    public interface ILocationsLifecycleWriteService
    {
        Task<LocationLifecycleWriteResult> ActivateDraftAsync(
            int restaurantId,
            int locationId,
            int actorUserId
        );

        Task<LocationLifecycleWriteResult> DeleteDraftAsync(
            int restaurantId,
            int locationId,
            int actorUserId
        );

        Task<LocationLifecycleWriteResult> EditDetailsAsync(
            int restaurantId,
            int locationId,
            int actorUserId,
            AddOwnedLocationRequest request
        );
    }
}
