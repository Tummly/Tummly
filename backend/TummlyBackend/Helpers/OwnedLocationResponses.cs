using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.OwnedLocation;

namespace TummlyBackend.Helpers
{
    public static class OwnedLocationResponses
    {
        public static IActionResult? FromResult(
            OwnedLocationResult result
        )
        {
            return result.Status switch
            {
                OwnedLocationResolveStatus.Found => null,
                OwnedLocationResolveStatus.NotFound =>
                    new NotFoundObjectResult(new
                    {
                        success = false,
                        message = "Location not found."
                    }),
                OwnedLocationResolveStatus.Forbidden =>
                    new ObjectResult(new
                    {
                        success = false,
                        message =
                            "You do not have access to this location."
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    },
                _ => throw new ArgumentOutOfRangeException(
                    nameof(result),
                    result.Status,
                    "Unexpected owned-location resolve status."
                )
            };
        }
    }
}
