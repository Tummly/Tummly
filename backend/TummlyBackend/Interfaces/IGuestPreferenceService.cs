using TummlyBackend.DTOs.GuestPreferences;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Guest preference centre read and write rules. Owns every consent write
    /// the page makes. Restaurant-side writes go through
    /// <see cref="ILocationGuestPermissionLedgerService"/>.
    /// </summary>
    public interface IGuestPreferenceService
    {
        Task<GuestPreferencesOverviewDto> GetOverviewAsync(
            int tummlyGuestId,
            CancellationToken ct
        );

        /// <exception cref="ArgumentException">Trimmed name is empty or longer than 100 characters.</exception>
        /// <exception cref="KeyNotFoundException">No Tummly Guest with that id.</exception>
        Task SetFirstNameAsync(int tummlyGuestId, string firstName, CancellationToken ct);

        /// <summary>Applies only the non-null keys. A null key writes nothing.</summary>
        Task SetChannelsAsync(
            int tummlyGuestId,
            GuestPreferenceChannelsRequest req,
            CancellationToken ct
        );

        /// <summary>Stop all: withdraws Email and SMS everywhere and on the Tummly stream.</summary>
        Task StopAllAsync(int tummlyGuestId, CancellationToken ct);

        Task<IReadOnlyList<GuestPreferenceRestaurantDto>> ListRestaurantsAsync(
            int tummlyGuestId,
            string? q,
            CancellationToken ct
        );

        /// <exception cref="KeyNotFoundException">Location is not on this guest's list, or no Tummly Guest.</exception>
        Task SetLocationMuteAsync(
            int tummlyGuestId,
            int locationId,
            bool muted,
            CancellationToken ct
        );

        /// <exception cref="KeyNotFoundException">Restaurant is not on this guest's list, or no Tummly Guest.</exception>
        Task SetRestaurantMuteAsync(
            int tummlyGuestId,
            int restaurantId,
            bool muted,
            CancellationToken ct
        );
    }
}
