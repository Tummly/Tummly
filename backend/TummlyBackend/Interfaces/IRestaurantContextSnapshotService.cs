using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IRestaurantContextSnapshotService
    {
        Task<RestaurantContextSnapshot> BuildAsync(
            int ownerUserId,
            LocationScope scope,
            PeriodWindow? currentOverride,
            PeriodWindow? comparisonOverride,
            CancellationToken cancellationToken = default
        );
    }
}
