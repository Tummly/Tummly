using TummlyBackend.DTOs.BillingCredits;

namespace TummlyBackend.Interfaces
{
    public interface IPlanEntitlementsSnapshot
    {
        Task<PlanEntitlementsAccountSnapshotDto> GetAccountAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        );

        Task<PlanEntitlementsLocationSnapshotDto?> GetLocationAsync(
            int restaurantId,
            int locationId,
            CancellationToken cancellationToken = default
        );

        Task<PlanEntitlementsSnapshotDto> GetAsync(
            int restaurantId,
            int? locationId = null,
            CancellationToken cancellationToken = default
        );
    }
}
