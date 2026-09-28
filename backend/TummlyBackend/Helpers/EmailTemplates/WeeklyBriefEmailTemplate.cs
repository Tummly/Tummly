using System.Globalization;
using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Weekly Brief ready from React Email
    /// (<c>emails/emails/weekly-brief.tsx</c> →
    /// <c>Assets/emails/templates/weekly-brief.html</c>).
    /// Re-export with <c>npm run email:export</c> after template edits.
    /// </summary>
    public static class WeeklyBriefEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/weekly-brief.html";

        public static string Subject(string locationName)
        {
            var name = string.IsNullOrWhiteSpace(locationName)
                ? "Location"
                : locationName.Trim();
            return $"Your Tummly Weekly Brief — {name}";
        }

        public static string Generate(
            IWebHostEnvironment environment,
            string firstName,
            string locationName,
            string periodLabel,
            int qrScans,
            int feedbackReceived,
            int guestsCaptured,
            int offerClaimed,
            int redemptions,
            int campaignEngagement,
            string whatChanged,
            string recommendedNextStep,
            string weeklyBriefUrl,
            string privacyUrl,
            string companyDetailsUrl,
            string logoUrl
        )
        {
            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Weekly Brief email template was not found. Run `npm run email:export` from the repo root."
            );

            return html
                .Replace(
                    "{{first_name}}",
                    WebUtility.HtmlEncode(firstName),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{location_name}}",
                    WebUtility.HtmlEncode(locationName),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{period_label}}",
                    WebUtility.HtmlEncode(periodLabel),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{qr_scans}}",
                    FormatCount(qrScans),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{feedback_received}}",
                    FormatCount(feedbackReceived),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{guests_captured}}",
                    FormatCount(guestsCaptured),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{offer_claimed}}",
                    FormatCount(offerClaimed),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{redemptions}}",
                    FormatCount(redemptions),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{campaign_engagement}}",
                    FormatCount(campaignEngagement),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{what_changed}}",
                    WebUtility.HtmlEncode(whatChanged),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{recommended_next_step}}",
                    WebUtility.HtmlEncode(recommendedNextStep),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{weekly_brief_url}}",
                    WebUtility.HtmlEncode(weeklyBriefUrl),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{privacy_url}}",
                    WebUtility.HtmlEncode(privacyUrl),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{company_details_url}}",
                    WebUtility.HtmlEncode(companyDetailsUrl),
                    StringComparison.Ordinal
                )
                .Replace("{{logo_url}}", logoUrl, StringComparison.Ordinal);
        }

        private static string FormatCount(int value) =>
            value.ToString(CultureInfo.InvariantCulture);
    }
}
