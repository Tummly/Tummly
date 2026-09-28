using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Shop order dispatched from React Email
    /// (<c>emails/emails/shop-order-dispatched.tsx</c>).
    /// Figma Guest-Loop-MVP node 6852:42010.
    /// </summary>
    public static class ShopOrderDispatchedEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/shop-order-dispatched.html";

        public const string Subject = "Your Tummly order is on its way";

        public static string Generate(
            IWebHostEnvironment environment,
            string firstName,
            string locationName,
            string orderNumber,
            string deliveryEstimate,
            string trackingDetails,
            string orderUrl,
            string privacyUrl,
            string companyDetailsUrl,
            string logoUrl
        )
        {
            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Shop order dispatched email template was not found. Run `npm run email:export` from the repo root."
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
                    "{{delivery_estimate}}",
                    WebUtility.HtmlEncode(deliveryEstimate),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{tracking_details}}",
                    WebUtility.HtmlEncode(trackingDetails),
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
