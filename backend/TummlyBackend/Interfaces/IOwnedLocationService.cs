using TummlyBackend.DTOs.OwnedLocation;

namespace TummlyBackend.Interfaces
{
    public interface IOwnedLocationService
    {
        Task<OwnedLocationResult> ResolveAsync(int userId, int locationId);

        /// <summary>
        /// Location ids for the restaurant that the operator owns.
        /// Empty when the restaurant has no matching owned locations.
        /// </summary>
        Task<IReadOnlyList<int>> ListOwnedLocationIdsAsync(
            int restaurantId,
            int userId,
            CancellationToken cancellationToken = default
        );
    }
}
