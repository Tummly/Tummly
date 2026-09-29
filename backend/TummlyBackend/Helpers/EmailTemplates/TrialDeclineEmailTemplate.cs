using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Trial decline from React Email
    /// (<c>emails/emails/trial-decline.tsx</c>).
    /// </summary>
    public static class TrialDeclineEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/trial-decline.html";

        public const string Subject = "Update on your Tummly trial request";

        public static string Generate(
            IWebHostEnvironment environment,
            string fullName,
            string? declineReason,
            string helpCentreUrl,
            string termsUrl,
            string privacyUrl,
            string cookiePolicyUrl,
            string logoUrl
        )
        {
            var firstName = ReactEmailHtmlFragments.ExtractFirstName(fullName);
            var greetingLine = $"Hi {WebUtility.HtmlEncode(firstName)},";
            var feedbackBlock = string.IsNullOrWhiteSpace(declineReason)
                ? string.Empty
                : ReactEmailHtmlFragments.AdminFeedbackBlock(
                    "Reason",
                    declineReason
                );

            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Trial decline email template was not found. Run `npm run email:export` from the repo root."
            );

            return html
                .Replace(
                    "{{greeting_line}}",
                    greetingLine,
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{feedback_block}}",
                    feedbackBlock,
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
