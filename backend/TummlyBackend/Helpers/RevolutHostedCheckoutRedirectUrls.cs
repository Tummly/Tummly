using System.Net;
using Microsoft.Extensions.Configuration;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Builds post-payment return URLs for Revolut Hosted Checkout Page orders.
    /// Revolut rejects localhost and IP hosts for <c>redirect_url</c>.
    /// </summary>
    public static class RevolutHostedCheckoutRedirectUrls
    {
        public const string InvalidHostErrorCode = "revolut_redirect_host_invalid";

        public static string BuildBillingCreditsTabUrl(
            IConfiguration configuration,
            string restaurantAccountType,
            int locationId,
            string tabId,
            IReadOnlyDictionary<string, string>? extraQuery = null
        )
        {
            var baseUrl = ResolveRedirectBaseUrl(configuration);
            ValidateRevolutRedirectHost(baseUrl);

            var root = string.Equals(
                restaurantAccountType,
                "Multi",
                StringComparison.Ordinal
            )
                ? "/multi-dashboard"
                : "/single-dashboard";

            var query = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["location"] = locationId.ToString(),
                ["tab"] = tabId,
            };

            if (extraQuery != null)
            {
                foreach (var (key, value) in extraQuery)
                {
                    query[key] = value;
                }
            }

            var queryString = string.Join(
                "&",
                query.Select(pair =>
                    $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"
                )
            );

            return $"{baseUrl}{root}/settings/billing-credits?{queryString}";
        }

        public static string BuildShopOrdersTabUrl(
            IConfiguration configuration,
            string restaurantAccountType,
            int locationId,
            IReadOnlyDictionary<string, string>? extraQuery = null
        )
        {
            var baseUrl = ResolveRedirectBaseUrl(configuration);
            ValidateRevolutRedirectHost(baseUrl);

            var root = string.Equals(
                restaurantAccountType,
                "Multi",
                StringComparison.Ordinal
            )
                ? "/multi-dashboard"
                : "/single-dashboard";

            var query = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["location"] = locationId.ToString(),
                ["view"] = "orders",
            };

            if (extraQuery != null)
            {
                foreach (var (key, value) in extraQuery)
                {
                    query[key] = value;
                }
            }

            var queryString = string.Join(
                "&",
                query.Select(pair =>
                    $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"
                )
            );

            return $"{baseUrl}{root}/shop?{queryString}";
        }

        internal static string ResolveRedirectBaseUrl(IConfiguration configuration)
        {
            var overrideUrl = configuration["Frontend:RevolutRedirectBaseUrl"]
                ?.Trim()
                .TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(overrideUrl))
            {
                return overrideUrl;
            }

            var baseUrl = configuration["Frontend:BaseUrl"]?.Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException(
                    "Frontend:BaseUrl is not configured."
                );
            }

            return baseUrl;
        }

        internal static void ValidateRevolutRedirectHost(string baseUrl)
        {
            if (
                !Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
                || (
                    uri.Scheme != Uri.UriSchemeHttps
                    && uri.Scheme != Uri.UriSchemeHttp
                )
                || string.IsNullOrWhiteSpace(uri.Host)
            )
            {
                throw new InvalidOperationException(InvalidHostErrorCode);
            }

            if (
                uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || IPAddress.TryParse(uri.Host, out _)
            )
            {
                throw new InvalidOperationException(InvalidHostErrorCode);
            }
        }
    }
}
