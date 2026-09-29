using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Shop order confirmed from React Email
    /// (<c>emails/emails/shop-order-confirmed.tsx</c>).
    /// Figma Guest-Loop-MVP node 6852:41958.
    /// </summary>
    public static class ShopOrderConfirmedEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/shop-order-confirmed.html";

        public const string Subject = "Your Tummly order is confirmed";

        public static string Generate(
            IWebHostEnvironment environment,
            string firstName,
            string locationName,
            string orderNumber,
            string materialsLinesHtml,
            string deliveryAddressHtml,
            string orderUrl,
            string privacyUrl,
            string companyDetailsUrl,
            string logoUrl
        )
        {
            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Shop order confirmed email template was not found. Run `npm run email:export` from the repo root."
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
                    "{{order_number}}",
                    WebUtility.HtmlEncode(orderNumber),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{materials_lines_html}}",
                    materialsLinesHtml,
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{delivery_address_html}}",
                    deliveryAddressHtml,
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{order_url}}",
                    WebUtility.HtmlEncode(orderUrl),
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
    }
}
