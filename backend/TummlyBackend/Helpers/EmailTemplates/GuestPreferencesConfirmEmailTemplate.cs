using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Guest preference centre confirm-email link. HTML is
    /// <c>Assets/emails/templates/guest-preferences-confirm.html</c>.
    /// </summary>
    public static class GuestPreferencesConfirmEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/guest-preferences-confirm.html";

        public const string Subject = "Confirm your email for Tummly";

        public static string Generate(
            IWebHostEnvironment environment,
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
