using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Help Centre operator reply (internal) from React Email
    /// (<c>emails/emails/help-centre-operator-reply.tsx</c>).
    /// </summary>
    public static class HelpCentreOperatorReplyEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/help-centre-operator-reply.html";

        public const string Subject = "Operator replied to Help Centre query";

        public static string Generate(
            IWebHostEnvironment environment,
            string topicLabel,
            string submitterName,
            string submitterEmail,
            string businessName,
            string replyBody,
            string supportDashboardUrl,
            string helpCentreUrl,
            string logoUrl
        )
        {
            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Help Centre operator reply email template was not found. Run `npm run email:export` from the repo root."
            );

            return html
                .Replace(
                    "{{topic_label}}",
                    WebUtility.HtmlEncode(topicLabel),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{submitter_name}}",
                    WebUtility.HtmlEncode(submitterName),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{submitter_email}}",
                    WebUtility.HtmlEncode(submitterEmail),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{business_name}}",
                    WebUtility.HtmlEncode(businessName),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{reply_body}}",
                    WebUtility.HtmlEncode(replyBody),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{support_dashboard_url}}",
                    WebUtility.HtmlEncode(supportDashboardUrl),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{help_centre_url}}",
                    WebUtility.HtmlEncode(helpCentreUrl),
                    StringComparison.Ordinal
                )
                .Replace("{{logo_url}}", logoUrl, StringComparison.Ordinal);
        }
    }
}
