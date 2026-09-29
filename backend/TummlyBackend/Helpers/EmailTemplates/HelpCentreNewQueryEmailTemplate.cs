using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Help Centre new query (internal) from React Email
    /// (<c>emails/emails/help-centre-new-query.tsx</c>).
    /// </summary>
    public static class HelpCentreNewQueryEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/help-centre-new-query.html";

        public static string Subject => "New Help Centre query";

        public static string Generate(
            IWebHostEnvironment environment,
            string topicLabel,
            string submitterName,
            string submitterEmail,
            string businessName,
            string? locationLabel,
            string messagePreview,
            int attachmentCount,
            string supportDashboardUrl,
            string helpCentreUrl,
            string logoUrl
        )
        {
            var locationRow = string.IsNullOrWhiteSpace(locationLabel)
                ? string.Empty
                : ReactEmailHtmlFragments.DetailRow(
                    "Location",
                    WebUtility.HtmlEncode(locationLabel)
                );
            var attachmentRow = attachmentCount > 0
                ? ReactEmailHtmlFragments.DetailRow(
                    "Attachments",
                    $"{attachmentCount} included — view in Support dashboard"
                )
                : string.Empty;

            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Help Centre new query email template was not found. Run `npm run email:export` from the repo root."
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
                    "{{location_row}}",
                    locationRow,
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{attachment_row}}",
                    attachmentRow,
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{message_preview}}",
                    WebUtility.HtmlEncode(messagePreview),
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
