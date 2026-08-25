using Microsoft.AspNetCore.Mvc;

namespace TummlyBackend.DTOs.Guests
{
    public enum GuestsEffectiveLocationStatus
    {
        Ok,
        Forbidden,
        NotFound,
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

        public static GuestsEffectiveLocationResult NotFound(string message) =>
            new()
            {
                Status = GuestsEffectiveLocationStatus.NotFound,
                ErrorMessage = message,
            };

        public IActionResult? ToHttpResult()
        {
            return Status switch
            {
                GuestsEffectiveLocationStatus.Ok => null,
                GuestsEffectiveLocationStatus.Forbidden => new ObjectResult(new
                {
                    success = false,
                    message = ErrorMessage,
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                },
                GuestsEffectiveLocationStatus.NotFound => new NotFoundObjectResult(new
                {
                    success = false,
                    message = ErrorMessage,
                }),
                _ => new ObjectResult(new
                {
                    success = false,
                    message = ErrorMessage,
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                },
            };
        }
    }
}
