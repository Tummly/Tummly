using Microsoft.Extensions.Configuration;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Builds preference centre links for email footers, and RFC 8058 one-click URLs for the
    /// <c>List-Unsubscribe</c> header.
    /// </summary>
    public static class UnsubscribeLink
    {
        /// <summary>
        /// Signed preference centre link for one Location Guest (<c>/preferences?t=</c>).
        /// </summary>
        public static string ForLocationGuest(
            string frontendBaseUrl,
            int locationGuestId,
            int restaurantId,
            string secret,
            DateTime? issuedAtUtc = null
        )
        {
            var baseUrl = NormalizeBaseUrl(frontendBaseUrl);
            var issued = issuedAtUtc ?? DateTime.UtcNow;
            var token = UnsubscribeToken.Create(
                locationGuestId,
                restaurantId,
                issued,
                secret
            );
            return $"{baseUrl}/preferences?t={token}";
        }

        /// <summary>
        /// Preference centre entry page, used when no signed token can be minted. The page asks
        /// for the guest's contact, so no restaurant id is needed in the URL.
        /// </summary>
        public static string ForRestaurant(string frontendBaseUrl)
        {
            var baseUrl = NormalizeBaseUrl(frontendBaseUrl);
            return $"{baseUrl}/preferences";
        }

        /// <summary>
        /// RFC 8058 one-click URL on the API origin. Mailbox providers POST to it.
        /// </summary>
        public static string OneClickUrl(string apiBaseUrl, string token)
        {
            var baseUrl = NormalizeBaseUrl(apiBaseUrl);
            return $"{baseUrl}/api/public/preferences/one-click?t={Uri.EscapeDataString(token)}";
        }

        /// <summary>
        /// One-click URL for a campaign or guest-response email, or null when it cannot be
        /// built. It needs <c>Api:PublicBaseUrl</c> with an <c>https://</c> origin (unset in local
        /// dev, so no headers are sent), a signing secret, and a Location Guest to withdraw.
        /// </summary>
        public static string? OneClickUrlFor(
            IConfiguration configuration,
            int restaurantId,
            int? locationGuestId
        )
        {
            var apiBaseUrl = configuration["Api:PublicBaseUrl"]?.Trim();
            var secret = ResolveSigningSecret(configuration);
            if (
                string.IsNullOrWhiteSpace(apiBaseUrl)
                || !apiBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(secret)
                || locationGuestId is not int lg
                || lg <= 0
                || restaurantId <= 0
            )
            {
                return null;
            }

            var token = UnsubscribeToken.Create(lg, restaurantId, DateTime.UtcNow, secret);
            return OneClickUrl(apiBaseUrl, token);
        }

        /// <summary>
        /// Prefers the signed <c>?t=</c> link when a secret and Location Guest are present.
        /// Otherwise returns the entry page when <paramref name="restaurantId"/> is positive.
        /// Returns null only when neither applies.
        /// </summary>
        public static string? PreferSignedOrRestaurant(
            string frontendBaseUrl,
            int restaurantId,
            int? locationGuestId,
            string? secret,
            DateTime? issuedAtUtc = null
        )
        {
            if (
                locationGuestId is int lg
                && lg > 0
                && restaurantId > 0
                && !string.IsNullOrEmpty(secret)
            )
            {
                return ForLocationGuest(
                    frontendBaseUrl,
                    lg,
                    restaurantId,
                    secret,
                    issuedAtUtc
                );
            }

            if (restaurantId > 0)
            {
                return ForRestaurant(frontendBaseUrl);
            }

            return null;
        }

        /// <summary>
        /// Prefer <c>Unsubscribe:SigningSecret</c>; fall back to JWT secret so
        /// existing deploys keep working until a dedicated secret is set.
        /// </summary>
        public static string? ResolveSigningSecret(IConfiguration configuration)
        {
            var dedicated = configuration["Unsubscribe:SigningSecret"];
            if (!string.IsNullOrWhiteSpace(dedicated))
            {
                return dedicated.Trim();
            }

            var jwt = configuration["JwtSettings:Secret"];
            return string.IsNullOrWhiteSpace(jwt) ? null : jwt.Trim();
        }

        private static string NormalizeBaseUrl(string frontendBaseUrl) =>
            frontendBaseUrl.Trim().TrimEnd('/');
    }
}
