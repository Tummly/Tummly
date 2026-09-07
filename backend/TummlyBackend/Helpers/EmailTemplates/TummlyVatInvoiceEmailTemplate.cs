using TummlyBackend.Helpers;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Non-transactional invoice notice — guest chrome with Tummly brand,
    /// PDF attached separately by <c>EmailService</c>.
    /// </summary>
    public static class TummlyVatInvoiceEmailTemplate
    {
        public const string BrandTitle = "Tummly";

        public static string Subject(string documentNumber) =>
            $"Your Tummly invoice {documentNumber.Trim()}";

        public static string GenerateMessage(
            string documentNumber,
            string lineDescription,
            int grossPence
        )
        {
            var amount = TummlyVatInvoicePdfWriter.FormatAmountLabel(grossPence);
            var safeDoc = documentNumber.Trim();
            var safeLine = string.IsNullOrWhiteSpace(lineDescription)
                ? "your purchase"
                : lineDescription.Trim();

            return $"Your invoice {safeDoc} for {safeLine} is attached as a PDF. Amount paid: {amount}.";
        }

        public static string GenerateHtml(
            string documentNumber,
            string lineDescription,
            int grossPence,
            string frontendBaseUrl
        )
        {
            var subject = Subject(documentNumber);
            var message = GenerateMessage(
                documentNumber,
                lineDescription,
                grossPence
            );
            var baseUrl = frontendBaseUrl.Trim().TrimEnd('/');
            var logoUrl = $"{baseUrl}{BaseNonTransactionalEmailTemplate.PublicLogoPath}";

            return BaseNonTransactionalEmailTemplate.Generate(
                BrandTitle,
                brandSubtitle: null,
                locationAddress: null,
                subject,
                message,
                baseUrl,
                logoUrl
            );
        }
    }
}
