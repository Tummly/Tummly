using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Pilot ended from React Email (<c>emails/emails/pilot-ended.tsx</c>).
    /// </summary>
    public static class PilotEndedEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/pilot-ended.html";

        public const string Subject = "Your Tummly Pilot has ended";

        public static string Generate(
            IWebHostEnvironment environment,
            string firstName,
            string restaurantName,
            string plansUrl,
            string privacyUrl,
            string companyDetailsUrl,
            string logoUrl
        )
        {
            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Pilot ended email template was not found. Run `npm run email:export` from the repo root."
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
                    "{{plans_url}}",
                    WebUtility.HtmlEncode(plansUrl),
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
