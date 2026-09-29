using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// New device sign-in from React Email
    /// (<c>emails/emails/new-device-sign-in.tsx</c>).
    /// </summary>
    public static class NewDeviceSignInEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/new-device-sign-in.html";

        public const string Subject =
            "New sign-in to your Tummly account";

        private static string? _templateHtml;

        public static string Generate(
            IWebHostEnvironment environment,
            string firstName,
            string signInTime,
            string deviceSummary,
            string locationSummary,
            string resetPasswordUrl,
            string helpCentreUrl,
            string logoUrl
        )
        {
            var html = LoadTemplate(environment);

            return html
                .Replace(
                    "{{first_name}}",
                    WebUtility.HtmlEncode(firstName),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{sign_in_time}}",
                    WebUtility.HtmlEncode(signInTime),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{device_summary}}",
                    WebUtility.HtmlEncode(deviceSummary),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{location_summary}}",
                    WebUtility.HtmlEncode(locationSummary),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{reset_password_url}}",
                    WebUtility.HtmlEncode(resetPasswordUrl),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{help_centre_url}}",
                    WebUtility.HtmlEncode(helpCentreUrl),
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
                    "New device sign-in email template was not found. Run `npm run email:export` from the repo root.",
                    path
                );
            }

            _templateHtml = File.ReadAllText(path);
            return _templateHtml;
        }
    }
}
