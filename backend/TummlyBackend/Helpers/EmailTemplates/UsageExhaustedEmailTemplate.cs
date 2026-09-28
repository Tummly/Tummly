using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Usage exhausted from React Email
    /// (<c>emails/emails/usage-exhausted.tsx</c>).
    /// </summary>
    public static class UsageExhaustedEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/usage-exhausted.html";

        public static string Subject(string allowanceKind) =>
            $"Your {allowanceKind} allowance has been used";

        public static string Generate(
            IWebHostEnvironment environment,
            string allowanceKind,
            string firstName,
            string ctaUrl,
            string ctaLabel,
            string privacyUrl,
            string companyDetailsUrl,
            string logoUrl
        )
        {
            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Usage exhausted email template was not found. Run `npm run email:export` from the repo root."
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
                    "{{cta_url}}",
                    WebUtility.HtmlEncode(ctaUrl),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{cta_label}}",
                    WebUtility.HtmlEncode(ctaLabel),
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
