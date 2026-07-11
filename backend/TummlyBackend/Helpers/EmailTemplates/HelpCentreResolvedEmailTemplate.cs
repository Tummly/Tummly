using System.Net;
using System.Text;

namespace TummlyBackend.Helpers.EmailTemplates
{
    public static class HelpCentreResolvedEmailTemplate
    {
        public const string Subject = "Your query has been resolved";

        public static string Generate(
            string submitterName,
            string topicLabel,
            IReadOnlyList<(string AuthorLabel, string Body)> excerptMessages,
            string? myQueriesUrl,
            string frontendBaseUrl,
            string logoDataUri
        )
        {
            var excerptHtml = BuildExcerptHtml(excerptMessages);

            var myQueriesSection = string.IsNullOrWhiteSpace(myQueriesUrl)
                ? string.Empty
                : $@"
                    <p style='margin:24px 0 0 0;font-size:16px;line-height:24px;color:#374151;'>
                        You can view the full conversation in
                        <a href='{myQueriesUrl}' style='color:#C2410C;text-decoration:underline;'>My queries</a>.
                    </p>";

            var bodyHtml = $@"
                <h1 style='margin:0 0 16px 0;font-size:24px;font-weight:700;color:#111827;'>
                    Your query has been resolved
                </h1>
                <p style='margin:0 0 16px 0;font-size:16px;line-height:24px;color:#374151;'>
                    Hi {WebUtility.HtmlEncode(submitterName)},
                </p>
                <p style='margin:0 0 16px 0;font-size:16px;line-height:24px;color:#374151;'>
                    We’ve marked your query as resolved.
                </p>
                <p style='margin:0 0 8px 0;font-size:14px;line-height:20px;color:#6B7280;'>
                    Re: {WebUtility.HtmlEncode(topicLabel)}
                </p>
                {excerptHtml}
                {myQueriesSection}
                <p style='margin:24px 0 0 0;font-size:16px;line-height:24px;color:#374151;'>
                    If you need more help, reply to this email or visit our
                    <a href='{frontendBaseUrl.TrimEnd('/')}/help-center' style='color:#C2410C;text-decoration:underline;'>Help Centre</a>.
                </p>";

            return BaseEmailTemplate.Generate(
                Subject,
                bodyHtml,
                frontendBaseUrl,
                logoDataUri,
                EmailFooterVariant.Transactional
            );
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
                        <p style='margin:0 0 8px 0;font-size:12px;line-height:16px;font-weight:600;letter-spacing:0.04em;text-transform:uppercase;color:#6B7280;'>
                            {WebUtility.HtmlEncode(authorLabel)}
                        </p>
                        <p style='margin:0;font-size:16px;line-height:24px;color:#111827;white-space:pre-wrap;'>
                            {WebUtility.HtmlEncode(body)}
                        </p>
                    </div>");
            }

            builder.Append("</div>");
            return builder.ToString();
        }
    }
}
