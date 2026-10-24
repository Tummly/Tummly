namespace TummlyBackend.Middleware
{
    /// <summary>
    /// Baseline security headers on every API response. HSTS is always sent:
    /// ingress ends TLS, so the app sees HTTP, and browsers ignore HSTS on HTTP.
    /// </summary>
    public sealed class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                headers["Strict-Transport-Security"] =
                    "max-age=31536000; includeSubDomains";

                return Task.CompletedTask;
            });

            return _next(context);
        }
    }
}
