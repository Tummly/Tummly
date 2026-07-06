using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Middleware
{
    public class ActivationGateMiddleware
    {
        private static readonly string[] AllowedPathPrefixes =
        [
            "/api/auth/me",
            "/api/auth/activate",
            "/api/help-centre",
        ];

        private static readonly string[] StaffRoles =
        [
            "Admin",
            "Support",
        ];

        private readonly RequestDelegate _next;

        public ActivationGateMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ApplicationDbContext db,
            IActivationGate activationGate
        )
        {
            if (
                context.User.Identity?.IsAuthenticated != true
                || StaffRoles.Any(role =>
                    string.Equals(
                        context.User.FindFirstValue(ClaimTypes.Role),
                        role,
                        StringComparison.OrdinalIgnoreCase
                    )
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

            if (user == null)
            {
                await _next(context);
                return;
            }

            var decision = activationGate.Decide(
                ActivationSubject.FromUser(user),
                ActivationIntent.ApiAccess
            );

            if (decision.Outcome == ActivationOutcome.Allow)
            {
                await _next(context);
                return;
            }

            // Map only known block reasons. Do not default unknown reasons to
            // Pending via exclusion — a new ActivationReason needs an explicit
            // branch. Unknown reasons fail closed as Expired (session clear).
            var (activationExpired, activationRequired) = decision.Reason switch
            {
                ActivationReason.Expired => (true, false),
                ActivationReason.Pending => (false, true),
                _ => (true, false),
            };

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                activationExpired,
                activationRequired,
                message = decision.Message,
            });
        }
    }
}
