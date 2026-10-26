using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Public URLs for Twilio webhooks. Twilio signs the URL it called, so the
    /// signed URL is built from <c>PublicApi:BaseUrl</c>, not the request host.
    /// </summary>
    public static class TwilioWebhookUrl
    {
        public const string SmsStatusPath = "/api/webhooks/twilio/sms-status";
        public const string SmsInboundPath = "/api/webhooks/twilio/sms-inbound";

        /// <summary>
        /// <c>{PublicApi:BaseUrl}{path}</c> without a trailing slash on the base,
        /// or null when <c>PublicApi:BaseUrl</c> is empty.
        /// </summary>
        public static string? For(IConfiguration config, string path)
        {
            var baseUrl = config["PublicApi:BaseUrl"]?.Trim().TrimEnd('/');
            return string.IsNullOrWhiteSpace(baseUrl) ? null : $"{baseUrl}{path}";
        }

        /// <summary>
        /// URL to validate a Twilio signature against. Falls back to the request
        /// URL only when <c>PublicApi:BaseUrl</c> is empty.
        /// </summary>
        public static string ValidationUrl(
            IConfiguration config,
            string path,
            HttpRequest request
        ) =>
            For(config, path)
            ?? $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
    }
}
