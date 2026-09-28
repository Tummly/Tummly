using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Venue-branded Guest response / Campaign email HTML from React Email
    /// (<c>emails/emails/guest-response.tsx</c> →
    /// <c>Assets/emails/templates/guest-response.html</c>).
    /// Offer card HTML is injected via <see cref="GuestResponseEmailOfferHtml"/>.
    /// </summary>
    public static class GuestResponseEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/guest-response.html";

        public const string EmptyValue = "—";

        public const string PublicLogoPath = EmailAssets.PublicPoweredByLogoPath;
        public const string PublicTopDecorationPath =
            EmailAssets.PublicTopDecorationPath;
        public const string PublicBottomStripPath =
            EmailAssets.PublicBottomStripPath;
        public const string PublicBrandLogoPlaceholderPath =
            EmailAssets.PublicBrandLogoPlaceholderPath;

        public static string Generate(
            IWebHostEnvironment environment,
            string brandTitle,
            string? brandSubtitle,
            string? locationAddress,
            string? subject,
            string message,
            string frontendBaseUrl,
            string? brandLogoUrl,
            GuestResponseEmailOfferBlock? offer = null,
            string? unsubscribeHref = null,
            string? emailAssetsBaseUrl = null
        )
        {
            var title = string.IsNullOrWhiteSpace(brandTitle)
                ? EmptyValue
                : brandTitle.Trim();
            var safeTitle = WebUtility.HtmlEncode(title);
            var subtitle = string.IsNullOrWhiteSpace(brandSubtitle)
                ? string.Empty
                : brandSubtitle.Trim();
            var trimmedSubject = subject?.Trim() ?? string.Empty;
            var trimmedMessage = string.IsNullOrWhiteSpace(message)
                ? EmptyValue
                : message.Trim();
            var address = string.IsNullOrWhiteSpace(locationAddress)
                ? EmptyValue
                : locationAddress.Trim();
            var safeAddress = WebUtility.HtmlEncode(address);
            var baseUrl = frontendBaseUrl.Trim().TrimEnd('/');
            var assetsBase = string.IsNullOrWhiteSpace(emailAssetsBaseUrl)
                ? baseUrl
                : emailAssetsBaseUrl.Trim().TrimEnd('/');
            var unsubscribeUrl = string.IsNullOrWhiteSpace(unsubscribeHref)
                ? $"{baseUrl}/unsubscribe"
                : unsubscribeHref.Trim();

            var resolvedBrandLogoUrl = string.IsNullOrWhiteSpace(brandLogoUrl)
                ? $"{assetsBase}{PublicBrandLogoPlaceholderPath}"
                : brandLogoUrl.Trim();

            var subjectBlock = trimmedSubject.Length == 0
                ? string.Empty
                : $@"<p data-guest-response-subject='1' style='margin:0 0 30px 0;font-size:14px;font-weight:600;line-height:20px;color:#ffffff;font-family:Arial,Helvetica,sans-serif;'>{WebUtility.HtmlEncode(trimmedSubject)}</p>";

            var messageHtml = WebUtility.HtmlEncode(trimmedMessage)
                .Replace("\r\n", "\n")
                .Replace("\n", "<br />");

            var disclaimer =
                $"You&#39;re receiving this because you joined {safeTitle} customer club after visiting or giving feedback.";
            var addressLine = $"{safeTitle}, {safeAddress}";
            var preview = trimmedSubject.Length > 0
                ? trimmedSubject
                : trimmedMessage.Split('\n')[0];

            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Guest response email template was not found. Run `npm run email:export` from the repo root."
            );

            return html
                .Replace(
                    "{{preview}}",
                    WebUtility.HtmlEncode(preview),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{brand_title}}",
                    safeTitle,
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{brand_subtitle}}",
                    WebUtility.HtmlEncode(subtitle),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{brand_logo_url}}",
                    WebUtility.HtmlEncode(resolvedBrandLogoUrl),
                    StringComparison.Ordinal
                )
                .Replace("{{subject_block}}", subjectBlock, StringComparison.Ordinal)
                .Replace("{{message_html}}", messageHtml, StringComparison.Ordinal)
                .Replace(
                    "{{offer_block}}",
                    GuestResponseEmailOfferHtml.Render(offer),
                    StringComparison.Ordinal
                )
                .Replace("{{disclaimer}}", disclaimer, StringComparison.Ordinal)
                .Replace("{{address_line}}", addressLine, StringComparison.Ordinal)
                .Replace(
                    "{{unsubscribe_url}}",
                    WebUtility.HtmlEncode(unsubscribeUrl),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{terms_url}}",
                    WebUtility.HtmlEncode($"{baseUrl}/terms"),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{privacy_url}}",
                    WebUtility.HtmlEncode($"{baseUrl}/privacy"),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{cookie_url}}",
                    WebUtility.HtmlEncode($"{baseUrl}/cookie-policy"),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{top_decoration_url}}",
                    WebUtility.HtmlEncode($"{assetsBase}{PublicTopDecorationPath}"),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{powered_by_logo_url}}",
                    WebUtility.HtmlEncode($"{assetsBase}{PublicLogoPath}"),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{bottom_strip_url}}",
                    WebUtility.HtmlEncode($"{assetsBase}{PublicBottomStripPath}"),
                    StringComparison.Ordinal
                );
        }
    }
}
