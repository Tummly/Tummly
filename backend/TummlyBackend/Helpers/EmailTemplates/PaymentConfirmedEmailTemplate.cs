using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Payment confirmed from React Email
    /// (<c>emails/emails/payment-confirmed.tsx</c>). Invoice PDF attaches separately.
    /// </summary>
    public static class PaymentConfirmedEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/payment-confirmed.html";

        public const string Subject = "Payment confirmed — Tummly";

        public static string Generate(
            IWebHostEnvironment environment,
            string firstName,
            string orderDescription,
            string amount,
            string paymentDate,
            string reference,
            string billingUrl,
            string privacyUrl,
            string companyDetailsUrl,
            string logoUrl
        )
        {
            var html = ReactEmailHtmlLoader.Load(
                environment,
                TemplateRelativePath,
                "Payment confirmed email template was not found. Run `npm run email:export` from the repo root."
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
                    "{{amount}}",
                    WebUtility.HtmlEncode(amount),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{payment_date}}",
                    WebUtility.HtmlEncode(paymentDate),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{reference}}",
                    WebUtility.HtmlEncode(reference),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{billing_url}}",
                    WebUtility.HtmlEncode(billingUrl),
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
