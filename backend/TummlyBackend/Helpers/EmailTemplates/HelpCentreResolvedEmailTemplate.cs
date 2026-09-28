using System.Net;
using System.Text;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Help Centre resolved from React Email
    /// (<c>emails/emails/help-centre-resolved.tsx</c>).
    /// </summary>
    public static class HelpCentreResolvedEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/help-centre-resolved.html";

        public const string Subject = "Your query has been resolved";

        public static string Generate(
            IWebHostEnvironment environment,
            string submitterName,
            string topicLabel,
            IReadOnlyList<(string AuthorLabel, string Body)> excerptMessages,
            string? myQueriesUrl,
            string helpCentreUrl,
            string logoUrl
        )
        {
            var myQueriesBlock = string.IsNullOrWhiteSpace(myQueriesUrl)
                ? string.Empty
                : $@"
                    <p style='margin:24px 0 0 0;font-size:16px;line-height:24px;color:#141414;'>
                        You can view the full conversation in
                        <a href='{WebUtility.HtmlEncode(myQueriesUrl)}' style='color:#141414;text-decoration:underline;'>My queries</a>.
                    </p>";

            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Help Centre resolved email template was not found. Run `npm run email:export` from the repo root."
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
                    "{{excerpt_block}}",
                    BuildExcerptHtml(excerptMessages),
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

        private static string BuildExcerptHtml(
            IReadOnlyList<(string AuthorLabel, string Body)> excerptMessages
        )
        {
            if (excerptMessages.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.Append("<div style='margin:16px 0 0 0;'>");

            foreach (var (authorLabel, body) in excerptMessages)
            {
                builder.Append($@"
                    <div style='margin:0 0 12px 0;padding:16px;background:#F9FAFB;border-radius:8px;border:1px solid #E5E7EB;'>
                        <p style='margin:0 0 8px 0;font-size:12px;line-height:16px;font-weight:600;letter-spacing:0.04em;text-transform:uppercase;color:#7d7d7d;'>
                            {WebUtility.HtmlEncode(authorLabel)}
                        </p>
                        <p style='margin:0;font-size:16px;line-height:24px;color:#141414;white-space:pre-wrap;'>
                            {WebUtility.HtmlEncode(body)}
                        </p>
                    </div>");
            }

            builder.Append("</div>");
            return builder.ToString();
        }
    }
}
