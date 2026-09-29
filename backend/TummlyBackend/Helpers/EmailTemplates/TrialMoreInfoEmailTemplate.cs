using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Trial more-info request from React Email
    /// (<c>emails/emails/trial-more-info.tsx</c>).
    /// </summary>
    public static class TrialMoreInfoEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/trial-more-info.html";

        public const string Subject = "Action required: Tummly trial request";

        public static string Generate(
            IWebHostEnvironment environment,
            string fullName,
            string? moreInfoMessage,
            string helpCentreUrl,
            string termsUrl,
            string privacyUrl,
            string cookiePolicyUrl,
            string logoUrl
        )
        {
            var firstName = ReactEmailHtmlFragments.ExtractFirstName(fullName);
            var greetingLine = $"Hi {WebUtility.HtmlEncode(firstName)},";
            var feedbackBlock = string.IsNullOrWhiteSpace(moreInfoMessage)
                ? @"<p style='margin:0 0 14px;font-size:14px;line-height:20px;color:#141414;'>Reply to this email with your restaurant&apos;s physical address or business registration details.</p>"
                : ReactEmailHtmlFragments.AdminFeedbackBlock(
                    "What we need from you",
                    moreInfoMessage
                );

            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Trial more-info email template was not found. Run `npm run email:export` from the repo root."
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
