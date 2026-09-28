using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Reset password from React Email
    /// (<c>emails/emails/reset-password.tsx</c>).
    /// </summary>
    public static class ResetPasswordEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/reset-password.html";

        public const string Subject = "Reset your Tummly password";

        private static string? _templateHtml;

        public static string Generate(
            IWebHostEnvironment environment,
            string resetUrl,
            string helpCentreUrl,
            string termsUrl,
            string privacyUrl,
            string cookiePolicyUrl,
            string logoUrl
        )
        {
            var html = LoadTemplate(environment);

            return html
                .Replace(
                    "{{reset_url}}",
                    WebUtility.HtmlEncode(resetUrl),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{help_centre_url}}",
                    WebUtility.HtmlEncode(helpCentreUrl),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{terms_url}}",
                    WebUtility.HtmlEncode(termsUrl),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{privacy_url}}",
                    WebUtility.HtmlEncode(privacyUrl),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{cookie_policy_url}}",
                    WebUtility.HtmlEncode(cookiePolicyUrl),
                    StringComparison.Ordinal
                )
                .Replace("{{logo_url}}", logoUrl, StringComparison.Ordinal);
        }

        private static string LoadTemplate(IWebHostEnvironment environment)
        {
            if (_templateHtml != null)
            {
                return _templateHtml;
            }

            var path = Path.Combine(
                environment.ContentRootPath,
                TemplateRelativePath.Replace('/', Path.DirectorySeparatorChar)
            );

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "Reset password email template was not found. Run `npm run email:export` from the repo root.",
                    path
                );
            }

            _templateHtml = File.ReadAllText(path);
            return _templateHtml;
        }
    }
}
