using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Contact enquiry confirmation from React Email
    /// (<c>emails/emails/contact-enquiry-received.tsx</c>).
    /// </summary>
    public static class ContactEnquiryReceivedEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/contact-enquiry-received.html";

        private static string? _templateHtml;

        public static string Subject(string reference) =>
            $"We've received your Tummly enquiry — {reference}";

        public static string FormatReference(int queryId)
        {
            var safeId = queryId > 0 ? queryId : 0;
            return $"TUM-{safeId.ToString().PadLeft(6, '0')}";
        }

        public static string Generate(
            IWebHostEnvironment environment,
            string topic,
            string reference,
            string privacyUrl,
            string companyDetailsUrl,
            string logoUrl
        )
        {
            var html = LoadTemplate(environment);

            return html
                .Replace(
                    "{{topic}}",
                    WebUtility.HtmlEncode(topic),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{reference}}",
                    WebUtility.HtmlEncode(reference),
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
                    "Contact enquiry email template was not found. Run `npm run email:export` from the repo root.",
                    path
                );
            }

            _templateHtml = File.ReadAllText(path);
            return _templateHtml;
        }
    }
}
