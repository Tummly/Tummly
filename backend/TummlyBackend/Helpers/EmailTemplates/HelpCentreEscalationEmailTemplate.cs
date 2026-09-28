using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Help Centre escalation (internal) from React Email
    /// (<c>emails/emails/help-centre-escalation.tsx</c>).
    /// </summary>
    public static class HelpCentreEscalationEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/help-centre-escalation.html";

        public const string Subject = "Help Centre query escalated";

        public static string Generate(
            IWebHostEnvironment environment,
            string topicLabel,
            string submitterName,
            string submitterEmail,
            string businessName,
            string? locationLabel,
            string threadSummary,
            string? escalationNote,
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
            var noteBlock = string.IsNullOrWhiteSpace(escalationNote)
                ? string.Empty
                : $@"
                    <p style='margin:16px 0 8px 0;font-size:14px;font-weight:600;color:#141414;'>
                        Escalation note
                    </p>
                    <p style='margin:0;font-size:14px;line-height:22px;color:#141414;white-space:pre-wrap;'>
                        {WebUtility.HtmlEncode(escalationNote)}
                    </p>";

            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Help Centre escalation email template was not found. Run `npm run email:export` from the repo root."
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
                    "{{thread_summary}}",
                    WebUtility.HtmlEncode(threadSummary),
                    StringComparison.Ordinal
                )
                .Replace("{{note_block}}", noteBlock, StringComparison.Ordinal)
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
