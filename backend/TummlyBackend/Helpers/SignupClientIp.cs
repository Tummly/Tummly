using System.Net;
using Microsoft.AspNetCore.Http;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Resolves the client IP that signup rate limits partition by. Behind the platform
    /// ingress <c>RemoteIpAddress</c> is the ingress itself, so the client comes from
    /// <c>X-Forwarded-For</c>. Only the entry <c>trustedProxyHops</c> from the right is
    /// used: entries further left are client-supplied and can be spoofed.
    /// </summary>
    public static class SignupClientIp
    {
        private const string ForwardedForHeader = "X-Forwarded-For";

        public const string Unknown = "unknown";

        public static string Resolve(HttpContext ctx, int trustedProxyHops)
        {
            ArgumentNullException.ThrowIfNull(ctx);

            var header = ctx.Request.Headers[ForwardedForHeader];
            if (header.Count > 0)
            {
                var entries = header.ToString()
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                var index = entries.Length - trustedProxyHops;
                if (trustedProxyHops >= 1 && index >= 0)
                {
                    return StripPort(entries[index]);
                }
            }

            var remote = ctx.Connection.RemoteIpAddress?.ToString();
            return string.IsNullOrEmpty(remote) ? Unknown : remote;
        }

        /// <summary>
        /// The platform ingress can append a source port (<c>1.2.3.4:5678</c>, <c>[::1]:5678</c>).
        /// The port changes per connection, so it must not be part of the partition key.
        /// A plain IPv6 address has no port and is returned unchanged.
        /// </summary>
        private static string StripPort(string raw) =>
            IPEndPoint.TryParse(raw, out var endpoint) ? endpoint.Address.ToString() : raw;
    }
}
