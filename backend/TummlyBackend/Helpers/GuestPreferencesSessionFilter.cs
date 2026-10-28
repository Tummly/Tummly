using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Marks a guest preference action as session-gated. Applies <see cref="GuestPreferencesSessionFilter"/>.
    /// </summary>
    public sealed class GuestPreferencesSessionAttribute : TypeFilterAttribute
    {
        public GuestPreferencesSessionAttribute()
            : base(typeof(GuestPreferencesSessionFilter)) { }
    }

    /// <summary>
    /// Requires a valid <see cref="GuestPreferencesSessionToken"/> in the
    /// <see cref="GuestPreferencesSessionToken.HeaderName"/> request header. Missing, invalid or
    /// expired → 401 <c>{ error: "session" }</c>. On success, stores the session's Tummly Guest id in
    /// <see cref="HttpContext.Items"/> under <see cref="TummlyGuestIdItemKey"/>. The renewed token
    /// (30-minute sliding session) is written to the response header only when the action result
    /// is not a 401, so a 401 from the action itself never carries a session.
    /// A missing signing secret is a configuration error and throws.
    /// </summary>
    public sealed class GuestPreferencesSessionFilter : IAsyncActionFilter, IAsyncResultFilter
    {
        public const string TummlyGuestIdItemKey = "TummlyGuestId";

        private const string RenewedTokenItemKey = "GuestPreferencesSessionRenewedToken";

        private readonly IConfiguration _configuration;
        private readonly TimeProvider _timeProvider;

        public GuestPreferencesSessionFilter(IConfiguration configuration, TimeProvider timeProvider)
        {
            _configuration = configuration;
            _timeProvider = timeProvider;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next
        )
        {
            var http = context.HttpContext;
            var secret = GuestPreferencesSessionToken.ResolveSecret(_configuration);
            if (string.IsNullOrEmpty(secret))
            {
                throw new InvalidOperationException(
                    "Guest preferences session signing secret is not configured."
                );
            }

            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var token = http.Request.Headers[GuestPreferencesSessionToken.HeaderName].ToString();

            if (
                !GuestPreferencesSessionToken.TryVerify(
                    token,
                    secret,
                    nowUtc,
                    out var tummlyGuestId,
                    out var contactId
                )
            )
            {
                context.Result = new UnauthorizedObjectResult(new { error = "session" });
                return;
            }

            http.Items[TummlyGuestIdItemKey] = tummlyGuestId;
            http.Items[RenewedTokenItemKey] = GuestPreferencesSessionToken.Create(
                tummlyGuestId,
                contactId,
                nowUtc,
                secret
            );

            await next();
        }

        public async Task OnResultExecutionAsync(
            ResultExecutingContext context,
            ResultExecutionDelegate next
        )
        {
            var http = context.HttpContext;
            if (
                http.Items.TryGetValue(RenewedTokenItemKey, out var renewed)
                && context.Result is not IStatusCodeActionResult { StatusCode: 401 }
            )
            {
                http.Response.Headers[GuestPreferencesSessionToken.HeaderName] = (string)renewed!;
            }

            await next();
        }
    }
}
