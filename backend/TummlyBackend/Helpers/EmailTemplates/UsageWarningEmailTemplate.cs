using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Usage warning from React Email (<c>emails/emails/usage-warning.tsx</c>).
    /// </summary>
    public static class UsageWarningEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/usage-warning.html";

        public static string Subject(string allowanceKind) =>
            $"You're nearing your {allowanceKind} allowance";

        public static string Generate(
            IWebHostEnvironment environment,
            string allowanceKind,
            string firstName,
            string percentUsed,
            string usedAmount,
            string remainingAmount,
            string resetDate,
            string usageUrl,
            string privacyUrl,
            string companyDetailsUrl,
            string logoUrl
        )
        {
            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Usage warning email template was not found. Run `npm run email:export` from the repo root."
            );

            return html
                .Replace(
                    "{{allowance_kind}}",
                    WebUtility.HtmlEncode(allowanceKind),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{first_name}}",
                    WebUtility.HtmlEncode(firstName),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{percent_used}}",
                    WebUtility.HtmlEncode(percentUsed),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{used_amount}}",
                    WebUtility.HtmlEncode(usedAmount),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{remaining_amount}}",
                    WebUtility.HtmlEncode(remainingAmount),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{reset_date}}",
                    WebUtility.HtmlEncode(resetDate),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{usage_url}}",
                    WebUtility.HtmlEncode(usageUrl),
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
