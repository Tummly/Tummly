using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Payment action required from React Email
    /// (<c>emails/emails/payment-action-required.tsx</c>).
    /// </summary>
    public static class PaymentActionRequiredEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/payment-action-required.html";

        public const string Subject = "Action required for your Tummly payment";

        public static string Generate(
            IWebHostEnvironment environment,
            string firstName,
            string orderDescription,
            string billingUrl,
            string ctaLabel,
            string privacyUrl,
            string companyDetailsUrl,
            string logoUrl
        )
        {
            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Payment action required email template was not found. Run `npm run email:export` from the repo root."
            );

            return html
                .Replace(
                    "{{first_name}}",
                    WebUtility.HtmlEncode(firstName),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{order_description}}",
                    WebUtility.HtmlEncode(orderDescription),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{billing_url}}",
                    WebUtility.HtmlEncode(billingUrl),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{cta_label}}",
                    WebUtility.HtmlEncode(ctaLabel),
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
