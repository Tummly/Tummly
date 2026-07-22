using TummlyBackend.Models;

namespace TummlyBackend.DTOs.Guests
{
    public enum GuestsEffectiveLocationStatus
    {
        Ok,
        Forbidden,
    }

    public sealed class GuestsEffectiveLocationResult
    {
        public required GuestsEffectiveLocationStatus Status { get; init; }

        public IReadOnlyList<int>? LocationIds { get; init; }

        public IReadOnlyDictionary<int, string>? LocationNamesById { get; init; }

        public string? ErrorMessage { get; init; }

        public static GuestsEffectiveLocationResult Ok(
            IReadOnlyList<int> locationIds,
            IReadOnlyDictionary<int, string> locationNamesById
        ) =>
            new()
            {
                Status = GuestsEffectiveLocationStatus.Ok,
                LocationIds = locationIds,
                LocationNamesById = locationNamesById,
            };

        public static GuestsEffectiveLocationResult Forbidden(string message) =>
            new()
            {
                Status = GuestsEffectiveLocationStatus.Forbidden,
                ErrorMessage = message,
            };
    }
}
