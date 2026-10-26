using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Paid plan started from React Email (<c>emails/emails/plan-started.tsx</c>).
    /// </summary>
    public static class PlanStartedEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/plan-started.html";

        public static string Subject(string planName) =>
            $"Your Tummly {planName} plan has started";

        public static string Generate(
            IWebHostEnvironment environment,
            string firstName,
            string planName,
            string restaurantName,
            string dashboardUrl,
            string privacyUrl,
            string companyDetailsUrl,
            string logoUrl
        )
        {
            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Plan started email template was not found. Run `npm run email:export` from the repo root."
            );

            return html
                .Replace(
                    "{{first_name}}",
                    WebUtility.HtmlEncode(firstName),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{plan_name}}",
                    WebUtility.HtmlEncode(planName),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{restaurant_name}}",
                    WebUtility.HtmlEncode(restaurantName),
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
