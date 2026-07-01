using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;

namespace TummlyBackend.Middleware
{
    public class ActivationGateMiddleware
    {
        private static readonly string[] AllowedPathPrefixes =
        [
            "/api/auth/me",
            "/api/auth/activate",
            "/api/auth/workspaces",
            "/api/auth/select-workspace",
        ];

        private readonly RequestDelegate _next;

        public ActivationGateMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ApplicationDbContext db
        )
        {
            if (
                context.User.Identity?.IsAuthenticated != true
                || string.Equals(
                    context.User.FindFirstValue(ClaimTypes.Role),
                    "Admin",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value ?? string.Empty;

            if (
                AllowedPathPrefixes.Any(prefix =>
                    path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                await _next(context);
                return;
            }

            var userIdClaim =
                context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (
                string.IsNullOrEmpty(userIdClaim)
                || !int.TryParse(userIdClaim, out var userId)
            )
            {
                await _next(context);
                return;
            }

            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null || !ActivationCodeHelper.IsOperatorApiAccessBlocked(user))
            {
                await _next(context);
                return;
            }

            if (ActivationCodeHelper.IsActivationExpired(user))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    activationExpired = true,
                    message = ActivationCodeHelper.ActivationExpiredMessage,
                });
                return;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                activationRequired = true,
                message =
                    "Account activation is required before accessing this resource.",
            });
        }
    }
}
