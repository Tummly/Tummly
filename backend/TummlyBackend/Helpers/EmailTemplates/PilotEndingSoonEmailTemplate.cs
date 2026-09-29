using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Pilot ending soon from React Email
    /// (<c>emails/emails/pilot-ending-soon.tsx</c>).
    /// </summary>
    public static class PilotEndingSoonEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/pilot-ending-soon.html";

        public static string Subject(int daysRemaining) =>
            $"Your Tummly Pilot ends in {daysRemaining} days";

        public static string Generate(
            IWebHostEnvironment environment,
            string daysRemaining,
            string firstName,
            string restaurantName,
            string pilotEndDate,
            string plansUrl,
            string privacyUrl,
            string companyDetailsUrl,
            string logoUrl
        )
        {
            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Pilot ending soon email template was not found. Run `npm run email:export` from the repo root."
            );

            return html
                .Replace(
                    "{{days_remaining}}",
                    WebUtility.HtmlEncode(daysRemaining),
                    StringComparison.Ordinal
                )
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
