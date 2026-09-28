using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// OTP HTML from React Email
    /// (<c>emails/emails/otp.tsx</c> → <c>Assets/emails/templates/otp.html</c>).
    /// Re-export with <c>npm run email:export</c> after template edits.
    /// </summary>
    public static class OtpEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/otp.html";

        private static string? _templateHtml;

        /// <summary>
        /// Subject when a restaurant / workspace name is known.
        /// </summary>
        public static string Subject(string? restaurantName)
        {
            return BuildHeadline(restaurantName);
        }

        public static string BuildHeadline(string? restaurantName)
        {
            if (string.IsNullOrWhiteSpace(restaurantName))
            {
                return "Your Tummly verification code";
            }

            return $"Verify your email to join {restaurantName.Trim()} on Tummly";
        }

        /// <summary>
        /// Full HTML document (not a body fragment).
        /// </summary>
        public static string Generate(
            IWebHostEnvironment environment,
            string otp,
            string? restaurantName,
            string helpCentreUrl,
            string logoUrl
        )
        {
            var html = LoadTemplate(environment);
            var headline = BuildHeadline(restaurantName);

            return html
                .Replace(
                    "{{headline}}",
                    WebUtility.HtmlEncode(headline),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{otp}}",
                    WebUtility.HtmlEncode(otp),
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
                    "OTP email template was not found. Run `npm run email:export` from the repo root.",
                    path
                );
            }

            _templateHtml = File.ReadAllText(path);
            return _templateHtml;
        }
    }
}
