using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    public static class HelpCentreSupportReplyEmailTemplate
    {
        public static string Generate(
            string submitterName,
            string topicLabel,
            string replyBody,
            string? myQueriesUrl,
            string frontendBaseUrl,
            string logoDataUri
        )
        {
            var myQueriesSection = string.IsNullOrWhiteSpace(myQueriesUrl)
                ? string.Empty
                : $@"
                    <p style='margin:24px 0 0 0;font-size:16px;line-height:24px;color:#374151;'>
                        You can view this conversation and reply in
                        <a href='{myQueriesUrl}' style='color:#C2410C;text-decoration:underline;'>My queries</a>.
                    </p>";

            var bodyHtml = $@"
                <h1 style='margin:0 0 16px 0;font-size:24px;font-weight:700;color:#111827;'>
                    Reply from Tummly Support
                </h1>
                <p style='margin:0 0 16px 0;font-size:16px;line-height:24px;color:#374151;'>
                    Hi {WebUtility.HtmlEncode(submitterName)},
                </p>
                <p style='margin:0 0 8px 0;font-size:14px;line-height:20px;color:#6B7280;'>
                    Re: {WebUtility.HtmlEncode(topicLabel)}
                </p>
                <div style='margin:16px 0;padding:16px;background:#F9FAFB;border-radius:8px;border:1px solid #E5E7EB;'>
                    <p style='margin:0;font-size:16px;line-height:24px;color:#111827;white-space:pre-wrap;'>
                        {WebUtility.HtmlEncode(replyBody)}
                    </p>
                </div>
                {myQueriesSection}
                <p style='margin:24px 0 0 0;font-size:16px;line-height:24px;color:#374151;'>
                    If you need more help, reply to this email or visit our
                    <a href='{frontendBaseUrl.TrimEnd('/')}/help-center' style='color:#C2410C;text-decoration:underline;'>Help Centre</a>.
                </p>";

            return BaseEmailTemplate.Generate(
                "Reply from Tummly Support",
                bodyHtml,
                frontendBaseUrl,
                logoDataUri,
                EmailFooterVariant.Transactional
            );
        }
    }
}
