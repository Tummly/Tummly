using Microsoft.Extensions.Configuration;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Builds unsubscribe URLs for non-transactional email footers.
    /// </summary>
    public static class UnsubscribeLink
    {
        /// <summary>
        /// Signed one-click URL when JWT secret is available.
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
            return $"{baseUrl}/unsubscribe?t={token}";
        }

        /// <summary>
        /// Form fallback when a signed token cannot be minted but restaurant
        /// context is known (<c>/unsubscribe?restaurantId=</c>).
        /// </summary>
        public static string ForRestaurant(
            string frontendBaseUrl,
            int restaurantId
        )
        {
            var baseUrl = NormalizeBaseUrl(frontendBaseUrl);
            return $"{baseUrl}/unsubscribe?restaurantId={restaurantId}";
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

        /// <summary>
        /// Prefer signed <c>?t=</c> when secret is present; otherwise
        /// restaurant-scoped form URL when <paramref name="restaurantId"/> is
        /// positive. Returns null only when neither path applies.
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
                return ForRestaurant(frontendBaseUrl, restaurantId);
            }

            return null;
        }

        private static string NormalizeBaseUrl(string frontendBaseUrl) =>
            frontendBaseUrl.Trim().TrimEnd('/');
    }
}
