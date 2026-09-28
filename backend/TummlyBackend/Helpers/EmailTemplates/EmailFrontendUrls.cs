using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Shared absolute URLs for React Email footers
    /// (<c>{Frontend:BaseUrl}/…</c>).
    /// </summary>
    public static class EmailFrontendUrls
    {
        public static string HelpCentre(string frontendBaseUrl) =>
            $"{Trim(frontendBaseUrl)}/help-center";

        public static string Terms(string frontendBaseUrl) =>
            $"{Trim(frontendBaseUrl)}/terms";

        public static string Privacy(string frontendBaseUrl) =>
            $"{Trim(frontendBaseUrl)}/privacy";

        public static string CookiePolicy(string frontendBaseUrl) =>
            $"{Trim(frontendBaseUrl)}/cookie-policy";

        /// <summary>
        /// Marketing "Company details" link (terms page until a dedicated
        /// company page ships).
        /// </summary>
        public static string CompanyDetails(string frontendBaseUrl) =>
            Terms(frontendBaseUrl);

        public static string ForgotPassword(string frontendBaseUrl) =>
            $"{Trim(frontendBaseUrl)}/forgot-password";

        /// <summary>Operator app home (post-sign-in landing).</summary>
        public static string AppHome(string frontendBaseUrl) => Trim(frontendBaseUrl);

        /// <summary>
        /// Shop order detail in Operator Shop Orders view.
        /// Single: <c>/single-dashboard/shop?location=&amp;view=orders&amp;shopOrderId=</c>.
        /// Multi: <c>/multi-dashboard/shop?…</c>.
        /// </summary>
        public static string ShopOrder(
            string accountType,
            int locationId,
            Guid shopOrderId
        )
        {
            var root = string.Equals(accountType, "Multi", StringComparison.Ordinal)
                ? "/multi-dashboard"
                : "/single-dashboard";
            return $"{root}/shop?location={locationId}&view=orders&shopOrderId={shopOrderId:D}";
        }

        /// <summary>
        /// Prefix relative CTA paths from <c>BillingAlertCtaResolver</c>
        /// with <paramref name="frontendBaseUrl"/>. Absolute http(s) URLs pass through.
        /// </summary>
        public static string Absolute(string frontendBaseUrl, string? relativeOrAbsolute)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsolute))
            {
                return AppHome(frontendBaseUrl);
            }

            var value = relativeOrAbsolute.Trim();
            if (
                value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            )
            {
                return value;
            }

            if (!value.StartsWith('/'))
            {
                value = "/" + value;
            }

            return $"{Trim(frontendBaseUrl)}{value}";
        }

        private static string Trim(string frontendBaseUrl) =>
            frontendBaseUrl.Trim().TrimEnd('/');
    }
}
