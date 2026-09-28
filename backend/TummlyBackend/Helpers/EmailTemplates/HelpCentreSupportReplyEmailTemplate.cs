using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Help Centre support reply from React Email
    /// (<c>emails/emails/help-centre-support-reply.tsx</c>).
    /// </summary>
    public static class HelpCentreSupportReplyEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/help-centre-support-reply.html";

        public const string Subject = "Reply from Tummly Support";

        public static string Generate(
            IWebHostEnvironment environment,
            string submitterName,
            string topicLabel,
            string replyBody,
            string? myQueriesUrl,
            string helpCentreUrl,
            string logoUrl
        )
        {
            var myQueriesBlock = string.IsNullOrWhiteSpace(myQueriesUrl)
                ? string.Empty
                : $@"
                    <p style='margin:24px 0 0 0;font-size:16px;line-height:24px;color:#141414;'>
                        You can view this conversation and reply in
                        <a href='{WebUtility.HtmlEncode(myQueriesUrl)}' style='color:#141414;text-decoration:underline;'>My queries</a>.
                    </p>";

            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Help Centre support reply email template was not found. Run `npm run email:export` from the repo root."
            );

            return html
                .Replace(
                    "{{submitter_name}}",
                    WebUtility.HtmlEncode(submitterName),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{topic_label}}",
                    WebUtility.HtmlEncode(topicLabel),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{reply_body}}",
                    WebUtility.HtmlEncode(replyBody),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{my_queries_block}}",
                    myQueriesBlock,
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
