using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Guest preference centre confirm-email link. Two copies share one HTML layout,
    /// <c>Assets/emails/templates/guest-preferences-confirm.html</c>: the entry link (sign in to
    /// the preference centre) and the add-contact link (add a new address to a guest's profile).
    /// The add-contact copy warns the recipient, who may not have asked for it.
    /// </summary>
    public static class GuestPreferencesConfirmEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/guest-preferences-confirm.html";

        public const string Subject = "Confirm your email for Tummly";

        public const string AddContactSubject = "Confirm this email for Tummly";

        private const string EntryIntro =
            "Open this link to confirm your email address and manage your Tummly marketing preferences. The link expires in 10 minutes.";

        private const string AddContactIntro =
            "Someone asked to add this email address to their Tummly marketing preferences. If this was you, open the link to confirm. If not, ignore this email. The link expires in 10 minutes.";

        public static string Generate(
            IWebHostEnvironment environment,
            string confirmUrl,
            string helpCentreUrl,
            string logoUrl
        ) => Render(environment, Subject, EntryIntro, confirmUrl, helpCentreUrl, logoUrl);

        public static string GenerateAddContact(
            IWebHostEnvironment environment,
            string confirmUrl,
            string helpCentreUrl,
            string logoUrl
        ) =>
            Render(
                environment,
                AddContactSubject,
                AddContactIntro,
                confirmUrl,
                helpCentreUrl,
                logoUrl
            );

        private static string Render(
            IWebHostEnvironment environment,
            string subject,
            string intro,
            string confirmUrl,
            string helpCentreUrl,
            string logoUrl
        )
        {
            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Guest preferences confirm email template was not found. Run `npm run email:export` from the repo root."
            );

            var ctaBlock = ReactEmailHtmlFragments.CtaButton("Confirm email", confirmUrl);

            return html
                .Replace("{{subject}}", WebUtility.HtmlEncode(subject), StringComparison.Ordinal)
                .Replace("{{intro}}", WebUtility.HtmlEncode(intro), StringComparison.Ordinal)
                .Replace("{{cta_block}}", ctaBlock, StringComparison.Ordinal)
                .Replace(
                    "{{help_centre_url}}",
                    WebUtility.HtmlEncode(helpCentreUrl),
                    StringComparison.Ordinal
                )
                .Replace("{{logo_url}}", logoUrl, StringComparison.Ordinal);
        }
    }
}
