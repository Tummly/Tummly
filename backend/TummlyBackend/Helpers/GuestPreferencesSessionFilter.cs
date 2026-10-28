using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
    /// <see cref="HttpContext.Items"/> under <see cref="TummlyGuestIdItemKey"/> and writes a renewed
    /// token to the response header (30-minute sliding session).
    /// </summary>
    public sealed class GuestPreferencesSessionFilter : IAsyncActionFilter
    {
        public const string TummlyGuestIdItemKey = "TummlyGuestId";

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
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var token = http.Request.Headers[GuestPreferencesSessionToken.HeaderName].ToString();

            if (
                string.IsNullOrEmpty(secret)
                || !GuestPreferencesSessionToken.TryVerify(
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
            http.Response.Headers[GuestPreferencesSessionToken.HeaderName] =
                GuestPreferencesSessionToken.Create(tummlyGuestId, contactId, nowUtc, secret);

            await next();
        }
    }
}
