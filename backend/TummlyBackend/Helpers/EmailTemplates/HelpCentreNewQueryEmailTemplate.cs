using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    public static class HelpCentreNewQueryEmailTemplate
    {
        public static string Subject => "New Help Centre query";

        public static string Generate(
            string topicLabel,
            string submitterName,
            string submitterEmail,
            string businessName,
            string? locationLabel,
            string messagePreview,
            int attachmentCount,
            string supportDashboardUrl,
            string frontendBaseUrl,
            string logoDataUri
        )
        {
            var attachmentLine = attachmentCount > 0
                ? $@"<tr>
                        <td style='padding:8px 0;font-size:14px;color:#6B7280;'>Attachments</td>
                        <td style='padding:8px 0;font-size:14px;color:#111827;'>{attachmentCount} included — view in Support dashboard</td>
                    </tr>"
                : string.Empty;

            var locationRow = string.IsNullOrWhiteSpace(locationLabel)
                ? string.Empty
                : $@"<tr>
                        <td style='padding:8px 0;font-size:14px;color:#6B7280;'>Location</td>
                        <td style='padding:8px 0;font-size:14px;color:#111827;'>{WebUtility.HtmlEncode(locationLabel)}</td>
                    </tr>";

            var bodyHtml = $@"
                <h1 style='margin:0 0 16px 0;font-size:24px;font-weight:700;color:#111827;'>
                    New Help Centre query
                </h1>
                <p style='margin:0 0 16px 0;font-size:16px;line-height:24px;color:#374151;'>
                    A new support request was submitted from Contact us.
                </p>
                <table style='width:100%;border-collapse:collapse;margin:16px 0;'>
                    <tr>
                        <td style='padding:8px 0;font-size:14px;color:#6B7280;width:140px;'>Topic</td>
                        <td style='padding:8px 0;font-size:14px;color:#111827;'>{WebUtility.HtmlEncode(topicLabel)}</td>
                    </tr>
                    <tr>
                        <td style='padding:8px 0;font-size:14px;color:#6B7280;'>Submitter</td>
                        <td style='padding:8px 0;font-size:14px;color:#111827;'>{WebUtility.HtmlEncode(submitterName)}</td>
                    </tr>
                    <tr>
                        <td style='padding:8px 0;font-size:14px;color:#6B7280;'>Email</td>
                        <td style='padding:8px 0;font-size:14px;color:#111827;'>{WebUtility.HtmlEncode(submitterEmail)}</td>
                    </tr>
                    <tr>
                        <td style='padding:8px 0;font-size:14px;color:#6B7280;'>Business</td>
                        <td style='padding:8px 0;font-size:14px;color:#111827;'>{WebUtility.HtmlEncode(businessName)}</td>
                    </tr>
                    {locationRow}
                    {attachmentLine}
                </table>
                <p style='margin:16px 0 8px 0;font-size:14px;font-weight:600;color:#111827;'>
                    Message
                </p>
                <p style='margin:0;font-size:14px;line-height:22px;color:#374151;white-space:pre-wrap;'>
                    {WebUtility.HtmlEncode(messagePreview)}
                </p>
                <p style='margin:24px 0 0 0;font-size:16px;line-height:24px;color:#374151;'>
                    Open the
                    <a href='{supportDashboardUrl}' style='color:#C2410C;text-decoration:underline;'>Support dashboard</a>
                    to respond.
                </p>";

            return BaseEmailTemplate.Generate(
                Subject,
                bodyHtml,
                frontendBaseUrl,
                logoDataUri,
                EmailFooterVariant.Transactional
            );
        }
    }
}
