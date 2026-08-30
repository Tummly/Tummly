using System.Globalization;
using System.Text;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// A4 UK tax-invoice PDF for Tummly VAT invoices / credit notes
    /// (sign-off §12 fields). No NuGet dependency — emits a valid
    /// <c>%PDF</c> byte stream with Helvetica / Helvetica-Bold.
    /// </summary>
    public static class TummlyVatInvoicePdfWriter
    {
        private const float PageWidth = 595f;
        private const float PageHeight = 842f;
        private const float MarginLeft = 48f;
        private const float MarginRight = 48f;
        private const float ContentRight = PageWidth - MarginRight;

        public static byte[] Render(TummlyVatInvoice invoice)
        {
            var content = BuildContentStream(invoice);
            return BuildPdf(content);
        }

        internal static string FormatPoundsFromPence(int pence)
            => FormatMoneyFromPence(pence, "GBP ");

        internal static string FormatAmountLabel(int pence)
            => FormatMoneyFromPence(pence, "£");

        private static string FormatMoneyFromPence(int pence, string prefix)
        {
            if (pence % 100 == 0)
            {
                return $"{prefix}{pence / 100}";
            }

            return $"{prefix}{(pence / 100m).ToString("0.00", CultureInfo.GetCultureInfo("en-GB"))}";
        }

        private static string BuildContentStream(TummlyVatInvoice invoice)
        {
            var isCreditNote = string.Equals(
                invoice.DocumentPrefix,
                TummlyVatInvoice.PrefixTcn,
                StringComparison.Ordinal
            );
            var title = isCreditNote ? "CREDIT NOTE" : "TAX INVOICE";
            var vatRatePercent = (invoice.VatRateBps / 100m).ToString(
                "0.##",
                CultureInfo.GetCultureInfo("en-GB")
            );
            var invoiceDate = LondonDateFormat.DMmmYyyy(invoice.InvoiceDateUtc);
            var taxPoint = LondonDateFormat.DMmmYyyy(invoice.TaxPointUtc);
            var net = FormatPoundsFromPence(invoice.NetPence);
            var vat = FormatPoundsFromPence(invoice.VatPence);
            var gross = FormatPoundsFromPence(invoice.GrossPence);

            var sb = new StringBuilder(4096);

            // Header rule
            StrokeLine(sb, MarginLeft, 780, ContentRight, 780);

            // Brand + document title
            TextAt(sb, "F2", 22, MarginLeft, 800, "Tummly");
            TextAt(sb, "F2", 16, 360, 800, title);
            TextAt(sb, "F1", 10, 360, 782, invoice.DocumentNumber);
            TextAt(sb, "F1", 9, 360, 768, $"Invoice date: {invoiceDate}");
            TextAt(sb, "F1", 9, 360, 754, $"Tax point: {taxPoint}");
            TextAt(sb, "F1", 9, 360, 740, $"Currency: {invoice.Currency}");

            // Parties
            var partyTop = 710f;
            TextAt(sb, "F2", 10, MarginLeft, partyTop, "From");
            TextAt(sb, "F2", 10, 310, partyTop, "Bill to");

            var sellerLines = BuildPartyLines(
                invoice.SellerLegalName,
                invoice.SellerRegisteredAddress,
                string.IsNullOrWhiteSpace(invoice.SellerVatRegistrationNumber)
                    ? null
                    : $"VAT: {invoice.SellerVatRegistrationNumber}"
            );
            var customerLines = BuildPartyLines(
                invoice.CustomerBusinessName,
                invoice.CustomerAddress,
                null
            );

            var ySeller = partyTop - 16f;
            foreach (var line in sellerLines)
            {
                TextAt(sb, "F1", 9, MarginLeft, ySeller, line);
                ySeller -= 12f;
            }

            var yCustomer = partyTop - 16f;
            foreach (var line in customerLines)
            {
                TextAt(sb, "F1", 9, 310, yCustomer, line);
                yCustomer -= 12f;
            }

            var tableTop = Math.Min(ySeller, yCustomer) - 24f;
            if (tableTop > 560f)
            {
                tableTop = 560f;
            }

            // Line-item table
            var colDesc = MarginLeft;
            var colQty = 320f;
            var colNet = 370f;
            var colVat = 440f;
            var colGross = 510f;
            var rowHeight = 22f;
            var headerBottom = tableTop - rowHeight;
            var rowBottom = headerBottom - rowHeight;

            StrokeRect(sb, MarginLeft, rowBottom, ContentRight - MarginLeft, tableTop - rowBottom);
            StrokeLine(sb, MarginLeft, headerBottom, ContentRight, headerBottom);
            StrokeLine(sb, colQty, rowBottom, colQty, tableTop);
            StrokeLine(sb, colNet, rowBottom, colNet, tableTop);
            StrokeLine(sb, colVat, rowBottom, colVat, tableTop);
            StrokeLine(sb, colGross, rowBottom, colGross, tableTop);

            TextAt(sb, "F2", 9, colDesc + 6, tableTop - 14, "Description");
            TextAt(sb, "F2", 9, colQty + 6, tableTop - 14, "Qty");
            TextAt(sb, "F2", 9, colNet + 6, tableTop - 14, "Net");
            TextAt(sb, "F2", 9, colVat + 6, tableTop - 14, "VAT");
            TextAt(sb, "F2", 9, colGross + 6, tableTop - 14, "Gross");

            var descLines = WrapText(invoice.LineDescription, 38);
            var descY = headerBottom - 14f;
            foreach (var line in descLines.Take(2))
            {
                TextAt(sb, "F1", 9, colDesc + 6, descY, line);
                descY -= 11f;
            }

            TextAt(sb, "F1", 9, colQty + 6, headerBottom - 14, invoice.Quantity.ToString(CultureInfo.InvariantCulture));
            TextAt(sb, "F1", 9, colNet + 6, headerBottom - 14, net);
            TextAt(sb, "F1", 9, colVat + 6, headerBottom - 14, vat);
            TextAt(sb, "F1", 9, colGross + 6, headerBottom - 14, gross);

            // Totals
            var totalsTop = rowBottom - 36f;
            var labelX = 360f;
            var valueX = 460f;
            TextAt(sb, "F1", 10, labelX, totalsTop, "Subtotal (net)");
            TextAt(sb, "F1", 10, valueX, totalsTop, net);
            TextAt(sb, "F1", 10, labelX, totalsTop - 16, $"VAT ({vatRatePercent}%)");
            TextAt(sb, "F1", 10, valueX, totalsTop - 16, vat);
            StrokeLine(sb, labelX, totalsTop - 24, ContentRight, totalsTop - 24);
            TextAt(sb, "F2", 12, labelX, totalsTop - 42, "Total");
            TextAt(sb, "F2", 12, valueX, totalsTop - 42, gross);
            TextAt(
                sb,
                "F1",
                10,
                labelX,
                totalsTop - 60,
                $"Payment status: {invoice.PaymentStatus}"
            );

            // Footer
            StrokeLine(sb, MarginLeft, 72, ContentRight, 72);
            TextAt(
                sb,
                "F1",
                8,
                MarginLeft,
                56,
                $"Supplier VAT number: {invoice.SellerVatRegistrationNumber}"
            );
            TextAt(
                sb,
                "F1",
                8,
                MarginLeft,
                44,
                "Generated by Tummly. This document is a VAT invoice issued by the supplier named above."
            );

            return sb.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
        }

        private static IReadOnlyList<string> BuildPartyLines(
            string name,
            string address,
            string? vatLine
        )
        {
            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(name))
            {
                lines.AddRange(WrapText(name.Trim(), 36));
            }

            if (!string.IsNullOrWhiteSpace(address))
            {
                lines.AddRange(WrapText(address.Trim(), 36));
            }

            if (!string.IsNullOrWhiteSpace(vatLine))
            {
                lines.Add(vatLine.Trim());
            }

            if (lines.Count == 0)
            {
                lines.Add("-");
            }

            return lines;
        }

        private static IReadOnlyList<string> WrapText(string text, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return [];
            }

            var words = text.Split(
                [' ', '\t', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries
            );
            var lines = new List<string>();
            var current = new StringBuilder();
            foreach (var word in words)
            {
                if (current.Length == 0)
                {
                    current.Append(word);
                    continue;
                }

                if (current.Length + 1 + word.Length <= maxChars)
                {
                    current.Append(' ');
                    current.Append(word);
                }
                else
                {
                    lines.Add(current.ToString());
                    current.Clear();
                    current.Append(word);
                }
            }

            if (current.Length > 0)
            {
                lines.Add(current.ToString());
            }

            return lines;
        }

        private static void TextAt(
            StringBuilder sb,
            string font,
            float size,
            float x,
            float y,
            string text
        )
        {
            sb.Append("BT /");
            sb.Append(font);
            sb.Append(' ');
            sb.Append(size.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(" Tf ");
            sb.Append(x.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(y.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(" Td (");
            sb.Append(EscapePdfText(SanitizeAscii(text)));
            sb.Append(") Tj ET\n");
        }

        private static void StrokeLine(
            StringBuilder sb,
            float x1,
            float y1,
            float x2,
            float y2
        )
        {
            sb.Append(x1.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(y1.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(" m ");
            sb.Append(x2.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(y2.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(" l S\n");
        }

        private static void StrokeRect(
            StringBuilder sb,
            float x,
            float y,
            float width,
            float height
        )
        {
            sb.Append(x.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(y.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(width.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(height.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(" re S\n");
        }

        private static string SanitizeAscii(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (ch >= 32 && ch <= 126)
                {
                    sb.Append(ch);
                }
                else if (ch == '£')
                {
                    sb.Append("GBP ");
                }
                else if (char.IsWhiteSpace(ch))
                {
                    sb.Append(' ');
                }
                else
                {
                    sb.Append('?');
                }
            }

            return sb.ToString();
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
                "3 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources<< /Font<< /F1 5 0 R /F2 6 0 R >> >> >>endobj\n",
                $"4 0 obj<< /Length {contentBytes.Length} >>stream\n{contentStream}endstream\nendobj\n",
                "5 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>endobj\n",
                "6 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>endobj\n",
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
