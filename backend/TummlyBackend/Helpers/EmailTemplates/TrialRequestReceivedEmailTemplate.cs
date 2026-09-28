using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Trial request received from React Email
    /// (<c>emails/emails/trial-request-received.tsx</c>).
    /// </summary>
    public static class TrialRequestReceivedEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/trial-request-received.html";

        public const string Subject = "We've received your Tummly trial request";

        public static string Generate(
            IWebHostEnvironment environment,
            string fullName,
            string businessName,
            string helpCentreUrl,
            string termsUrl,
            string privacyUrl,
            string cookiePolicyUrl,
            string logoUrl
        )
        {
            var firstName = ReactEmailHtmlFragments.ExtractFirstName(fullName);
            var restaurantName = string.IsNullOrWhiteSpace(businessName)
                ? "your restaurant"
                : businessName.Trim();
            var greetingLine = $"Hi {WebUtility.HtmlEncode(firstName)},";
            var introParagraph =
                $"Thanks for requesting a guided Tummly trial for {WebUtility.HtmlEncode(restaurantName)}.";

            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Trial request received email template was not found. Run `npm run email:export` from the repo root."
            );

            return html
                .Replace(
                    "{{greeting_line}}",
                    greetingLine,
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{intro_paragraph}}",
                    introParagraph,
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{help_centre_url}}",
                    WebUtility.HtmlEncode(helpCentreUrl),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{terms_url}}",
                    WebUtility.HtmlEncode(termsUrl),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{privacy_url}}",
                    WebUtility.HtmlEncode(privacyUrl),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{cookie_policy_url}}",
                    WebUtility.HtmlEncode(cookiePolicyUrl),
                    StringComparison.Ordinal
                )
                .Replace("{{logo_url}}", logoUrl, StringComparison.Ordinal);
        }
    }
}
