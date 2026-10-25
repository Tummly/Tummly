using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Stages and finds restaurant-scoped hashed contact suppressions (WS2a).
    /// Caller owns <c>SaveChanges</c>.
    /// </summary>
    public interface IGuestContactSuppressionService
    {
        Task StageAsync(
            int restaurantId,
            string contactKey,
            LocationGuestPermissionKind kind,
            DateTime withdrawnAtUtc,
            string source,
            CancellationToken ct = default
        );

        Task<IReadOnlyList<GuestContactSuppression>> FindAsync(
            int restaurantId,
            string contactKey,
            CancellationToken ct = default
        );
    }
}
