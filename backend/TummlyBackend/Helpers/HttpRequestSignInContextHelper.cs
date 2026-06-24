using TummlyBackend.DTOs.Auth;

namespace TummlyBackend.Helpers
{
    public static class HttpRequestSignInContextHelper
    {
        public static SignInContext FromHttpContext(HttpContext httpContext)
        {
            return new SignInContext
            {
                SignedInAtUtc = DateTime.UtcNow,
                IpAddress = GetClientIpAddress(httpContext),
                UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            };
        }

        public static string? GetClientIpAddress(HttpContext httpContext)
        {
            if (
                httpContext.Request.Headers.TryGetValue(
                    "X-Forwarded-For",
                    out var forwarded
                )
            )
            {
                var firstHop = forwarded
                    .ToString()
                    .Split(',')[0]
                    .Trim();

                if (!string.IsNullOrWhiteSpace(firstHop))
                {
                    return firstHop;
                }
            }

            return httpContext.Connection.RemoteIpAddress?.ToString();
        }
    }
}
