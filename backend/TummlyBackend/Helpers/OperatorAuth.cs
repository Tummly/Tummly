using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace TummlyBackend.Helpers
{
    public static class OperatorAuth
    {
        public static IActionResult? TryRequireUserId(
            ClaimsPrincipal user,
            out int userId
        )
        {
            userId = 0;

            var userIdClaim =
                user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (
                string.IsNullOrEmpty(userIdClaim)
                || !int.TryParse(userIdClaim, out userId)
            )
            {
                return new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Invalid token."
                });
            }

            return null;
        }
    }
}
