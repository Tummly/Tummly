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

        Task<LocationLifecycleWriteResult> SetManagerAsync(
            int restaurantId,
            int locationId,
            int actorUserId,
            int? managerUserId
        );
    }
}
