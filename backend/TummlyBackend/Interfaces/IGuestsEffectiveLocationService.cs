using TummlyBackend.DTOs.Guests;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IGuestsEffectiveLocationService
    {
        Task<GuestsEffectiveLocationResult> ResolveAsync(
            int ownerUserId,
            RestaurantLocation shellLocation,
            string? locationScope,
            int[]? locationIds
        );

        string ResolveScopeToken(
            string? locationScope,
            int[]? locationIds,
            int shellLocationId
        );
    }
}
