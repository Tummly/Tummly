using System.Globalization;
using System.Text;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Minimal PDF writer for Tummly VAT invoices (sign-off §12 fields).
    /// No NuGet dependency — emits a valid <c>%PDF</c> byte stream.
    /// </summary>
    public static class TummlyVatInvoicePdfWriter
    {
        public static byte[] Render(TummlyVatInvoice invoice)
        {
            var lines = BuildLines(invoice);
            var content = BuildContentStream(lines);
            return BuildPdf(content);
        }

        private static IReadOnlyList<string> BuildLines(TummlyVatInvoice invoice)
        {
            var vatRatePercent = (invoice.VatRateBps / 100m).ToString(
                "0.##",
                CultureInfo.GetCultureInfo("en-GB")
            );
            return
            [
                "Tummly VAT invoice",
                $"Invoice number: {invoice.DocumentNumber}",
                $"Invoice date: {LondonDateFormat.DMmmYyyy(invoice.InvoiceDateUtc)}",
                $"Tax point: {LondonDateFormat.DMmmYyyy(invoice.TaxPointUtc)}",
                $"Supplier: {invoice.SellerLegalName}",
                $"Supplier address: {invoice.SellerRegisteredAddress}",
                $"VAT number: {invoice.SellerVatRegistrationNumber}",
                $"Customer: {invoice.CustomerBusinessName}",
                $"Customer address: {invoice.CustomerAddress}",
                $"Line: {invoice.LineDescription}",
                $"Quantity: {invoice.Quantity}",
                $"Net: {FormatPoundsFromPence(invoice.NetPence)}",
                $"VAT rate: {vatRatePercent}%",
                $"VAT: {FormatPoundsFromPence(invoice.VatPence)}",
                $"Gross: {FormatPoundsFromPence(invoice.GrossPence)}",
                $"Currency: {invoice.Currency}",
                $"Payment status: {invoice.PaymentStatus}",
            ];
        }

        internal static string FormatPoundsFromPence(int pence)
        {
            if (pence % 100 == 0)
            {
                return $"GBP {pence / 100}";
            }

            return $"GBP {(pence / 100m).ToString("0.00", CultureInfo.GetCultureInfo("en-GB"))}";
        }

        internal static string FormatAmountLabel(int pence)
        {
            if (pence % 100 == 0)
            {
                return $"£{pence / 100}";
            }

            return $"£{(pence / 100m).ToString("0.00", CultureInfo.GetCultureInfo("en-GB"))}";
        }

        private static string BuildContentStream(IReadOnlyList<string> lines)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BT");
            sb.AppendLine("/F1 10 Tf");
            sb.AppendLine("50 780 Td");
            sb.AppendLine("14 TL");
            for (var i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                {
                    sb.AppendLine("T*");
                }

                sb.Append('(');
                sb.Append(EscapePdfText(lines[i]));
                sb.AppendLine(") Tj");
            }

            sb.AppendLine("ET");
            return sb.ToString().Replace("\r\n", "\n");
        }

        private static string EscapePdfText(string value)
        {
            return value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);
        }

        private static byte[] BuildPdf(string contentStream)
        {
            var contentBytes = Encoding.ASCII.GetBytes(contentStream);
            var objects = new List<string>
            {
                "1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj\n",
                "2 0 obj<< /Type /Pages /Kids [3 0 R] /Count 1 >>endobj\n",
                "3 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources<< /Font<< /F1 5 0 R >> >> >>endobj\n",
                $"4 0 obj<< /Length {contentBytes.Length} >>stream\n{contentStream}endstream\nendobj\n",
                "5 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>endobj\n",
            };

            var sb = new StringBuilder();
            sb.Append("%PDF-1.4\n");
            var offsets = new List<int> { 0 };
            foreach (var obj in objects)
            {
                offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));
                sb.Append(obj);
            }

            var xrefPos = Encoding.ASCII.GetByteCount(sb.ToString());
            sb.Append($"xref\n0 {objects.Count + 1}\n");
            sb.Append("0000000000 65535 f \n");
            for (var i = 1; i < offsets.Count; i++)
            {
                sb.Append(offsets[i].ToString("D10", CultureInfo.InvariantCulture));
                sb.Append(" 00000 n \n");
            }

            sb.Append($"trailer<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
            sb.Append("startxref\n");
            sb.Append(xrefPos);
            sb.Append("\n%%EOF\n");
            return Encoding.ASCII.GetBytes(sb.ToString());
        }
    }
}
