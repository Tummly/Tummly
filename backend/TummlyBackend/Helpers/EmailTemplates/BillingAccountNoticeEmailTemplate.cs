using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Billing account notice from React Email
    /// (<c>emails/emails/billing-account-notice.tsx</c>).
    /// </summary>
    public static class BillingAccountNoticeEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/billing-account-notice.html";

        public static string Generate(
            IWebHostEnvironment environment,
            string firstName,
            string title,
            string body,
            string? ctaLabel,
            string? ctaHref,
            string frontendBaseUrl,
            string helpCentreUrl,
            string logoUrl
        )
        {
            var ctaBlock = string.Empty;
            if (
                !string.IsNullOrWhiteSpace(ctaLabel)
                && !string.IsNullOrWhiteSpace(ctaHref)
            )
            {
                var href = EmailFrontendUrls.Absolute(frontendBaseUrl, ctaHref);
                ctaBlock = ReactEmailHtmlFragments.CtaButton(ctaLabel, href);
            }

            var greetingLine = $"Hi {WebUtility.HtmlEncode(firstName)},";

            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Billing account notice email template was not found. Run `npm run email:export` from the repo root."
            );

            return html
                .Replace(
                    "{{greeting_line}}",
                    greetingLine,
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{headline}}",
                    WebUtility.HtmlEncode(title),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{body}}",
                    WebUtility.HtmlEncode(body),
                    StringComparison.Ordinal
                )
                .Replace("{{cta_block}}", ctaBlock, StringComparison.Ordinal)
                .Replace(
                    "{{help_centre_url}}",
                    WebUtility.HtmlEncode(helpCentreUrl),
                    StringComparison.Ordinal
                )
                .Replace("{{logo_url}}", logoUrl, StringComparison.Ordinal);
        }
    }
}
