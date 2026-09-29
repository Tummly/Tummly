using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Pilot started from React Email (<c>emails/emails/pilot-started.tsx</c>).
    /// </summary>
    public static class PilotStartedEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/pilot-started.html";

        public const string Subject = "Your 30-day Tummly Pilot has started";

        public static string Generate(
            IWebHostEnvironment environment,
            string firstName,
            string restaurantName,
            string pilotEndDate,
            string dashboardUrl,
            string privacyUrl,
            string companyDetailsUrl,
            string logoUrl
        )
        {
            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Pilot started email template was not found. Run `npm run email:export` from the repo root."
            );

            return html
                .Replace(
                    "{{first_name}}",
                    WebUtility.HtmlEncode(firstName),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{restaurant_name}}",
                    WebUtility.HtmlEncode(restaurantName),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{pilot_end_date}}",
                    WebUtility.HtmlEncode(pilotEndDate),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{dashboard_url}}",
                    WebUtility.HtmlEncode(dashboardUrl),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{privacy_url}}",
                    WebUtility.HtmlEncode(privacyUrl),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{company_details_url}}",
                    WebUtility.HtmlEncode(companyDetailsUrl),
                    StringComparison.Ordinal
                )
                .Replace("{{logo_url}}", logoUrl, StringComparison.Ordinal);
        }
    }
}
