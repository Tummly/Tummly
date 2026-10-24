using System.Text.Json;
using System.Text.RegularExpressions;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Middleware
{
    /// <summary>
    /// Public product-demo sandbox (ENG07): writes are default-deny. Tour writes
    /// that would contaminate the shared seed are diverted into a session
    /// overlay (§15); only the explicit allow-list reaches a real handler.
    /// </summary>
    public sealed class DemoSessionSideEffectMiddleware
    {
        public const string DisabledMessage =
            "This action is disabled in the public product demo.";

        /// <summary>
        /// Writes that run their real handler for a demo session. Each one
        /// stores nothing shared, or is guarded further in its controller
        /// (Assistant turns accept only the tour prompt). AI providers are
        /// routed to deterministic twins for demo sessions.
        /// </summary>
        private static readonly Regex[] AllowedWrites =
        [
            Route("POST", "/api/auth/refresh"),
            Route("POST", "/api/auth/logout"),
            Route("POST", "/api/assistant/turns"),
            Route("POST", "/api/feedback/{id}/recovery-draft"),
            Route("POST", "/api/home/recommendation"),
            Route("POST", "/api/campaigns/recommendation"),
            Route("POST", "/api/campaigns/message-draft"),
            Route("POST", "/api/offers/{id}/recommendation"),
        ];

        private readonly RequestDelegate _next;

        public DemoSessionSideEffectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            DemoSessionOverlayService overlay
        )
        {
            var isMutating =
                HttpMethods.IsPost(context.Request.Method)
                || HttpMethods.IsPut(context.Request.Method)
                || HttpMethods.IsPatch(context.Request.Method)
                || HttpMethods.IsDelete(context.Request.Method);

            if (!isMutating || !DemoSessionClaims.IsDemoSession(context.User))
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path;

            var sessionId = DemoSessionClaims.TryGetSessionId(context.User);
            if (sessionId is Guid sid && IsContaminatingMutation(path))
            {
                var entityType = ResolveEntityType(path);
                var key = $"{context.Request.Method}:{path.Value}";
                await overlay.UpsertAsync(
                    sid,
                    entityType,
                    key,
                    new
                    {
                        path = path.Value,
                        method = context.Request.Method,
                        at = DateTime.UtcNow,
                    }
                );

                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(
                        new
                        {
                            success = true,
                            message =
                                "Saved in your demo session only. Other visitors will not see this change.",
                            demoSessionIsolated = true,
                        }
                    )
                );
                return;
            }

            if (IsAllowed(context.Request.Method, path))
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { message = DisabledMessage })
            );
        }

        private static Regex Route(string method, string template)
        {
            var pattern = Regex.Escape(template).Replace(@"\{id}", "[0-9]+");
            return new Regex(
                $"^{method} {pattern}/?$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
        }

        private static bool IsAllowed(string method, PathString path)
        {
            var request = $"{method.ToUpperInvariant()} {path.Value}";
            foreach (var route in AllowedWrites)
            {
                if (route.IsMatch(request))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsContaminatingMutation(PathString path)
        {
            var value = (path.Value ?? string.Empty).ToLowerInvariant();

            if (value.Contains("/redeem/check", StringComparison.Ordinal))
            {
                return false;
            }

            if (value.StartsWith("/api/notifications/", StringComparison.Ordinal)
                && value.Contains("/read", StringComparison.Ordinal))
            {
                return true;
            }

            if (value == "/api/home/weekly-brief/mark-reviewed")
            {
                return true;
            }

            return value.Contains("/workflow-status", StringComparison.Ordinal)
                || value.Contains("/guest-responses", StringComparison.Ordinal)
                || value.Contains("/recovery-completions", StringComparison.Ordinal)
                || value.Contains("/internal-actions", StringComparison.Ordinal)
                || (value.Contains("/redeem", StringComparison.Ordinal)
                    && !value.Contains("/redeem/check", StringComparison.Ordinal))
                || value.Contains("/fire", StringComparison.Ordinal)
                || value.Contains("/schedule-commit", StringComparison.Ordinal)
                || value.EndsWith("/send", StringComparison.Ordinal)
                || value.Contains("/cancel", StringComparison.Ordinal)
                    && value.StartsWith("/api/campaigns", StringComparison.Ordinal);
        }

        private static string ResolveEntityType(string path)
        {
            var value = path.ToLowerInvariant();
            if (value.Contains("workflow-status", StringComparison.Ordinal))
            {
                return DemoSessionOverlayEntityTypes.FeedbackWorkflow;
            }

            if (value.Contains("redeem", StringComparison.Ordinal))
            {
                return DemoSessionOverlayEntityTypes.OfferRedemption;
            }

            if (value.Contains("guest-response", StringComparison.Ordinal)
                || value.Contains("recovery", StringComparison.Ordinal))
            {
                return DemoSessionOverlayEntityTypes.RecoveryDraft;
            }

            if (value.Contains("/campaigns", StringComparison.Ordinal))
            {
                return DemoSessionOverlayEntityTypes.CampaignDraft;
            }

            return DemoSessionOverlayEntityTypes.SimulatedSend;
        }
    }
}
