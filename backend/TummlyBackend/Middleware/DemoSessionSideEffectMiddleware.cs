using System.Text.Json;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Middleware
{
    /// <summary>
    /// Public product-demo sandbox: block payment/shipment, and divert mutating
    /// writes that would contaminate the shared seed into a session overlay (§15).
    /// </summary>
    public sealed class DemoSessionSideEffectMiddleware
    {
        private static readonly PathString[] BlockedPrefixes =
        [
            new("/api/billing-credits/payment-method"),
            new("/api/billing-credits/pending-payment"),
            new("/api/billing-credits/plan-change"),
            new("/api/billing-credits/scheduled-change"),
            new("/api/billing-credits/top-up"),
            new("/api/billing-credits/extra-location"),
            new("/api/billing-credits/cancel-plan"),
            new("/api/shop/orders"),
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

            if (IsBlocked(path))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(
                        new
                        {
                            message =
                                "This action is disabled in the public product demo.",
                        }
                    )
                );
                return;
            }

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

            await _next(context);
        }

        private static bool IsBlocked(PathString path)
        {
            foreach (var prefix in BlockedPrefixes)
            {
                if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
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
