using System.Globalization;
using System.Text;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// A4 dark-theme UK tax-invoice PDF for Tummly VAT invoices / credit notes
    /// (Figma invoice frame). No NuGet dependency — emits a valid
    /// <c>%PDF</c> byte stream with Helvetica / Helvetica-Bold.
    /// </summary>
    public static class TummlyVatInvoicePdfWriter
    {
        private const float PageWidth = 595f;
        private const float PageHeight = 842f;
        private const float MarginLeft = 40f;
        private const float MarginRight = 40f;
        private const float ContentRight = PageWidth - MarginRight;
        private const float ContentWidth = ContentRight - MarginLeft;

        // #171717 / #666666 / #202020 / #14a74a (op-action-primary)
        private static readonly (float R, float G, float B) ColorBg = (
            23f / 255f,
            23f / 255f,
            23f / 255f
        );
        private static readonly (float R, float G, float B) ColorMuted = (
            102f / 255f,
            102f / 255f,
            102f / 255f
        );
        private static readonly (float R, float G, float B) ColorPrimary = (
            1f,
            1f,
            1f
        );
        private static readonly (float R, float G, float B) ColorHeaderBand = (
            32f / 255f,
            32f / 255f,
            32f / 255f
        );

        private const float LogoDisplayWidth = 90f;
        private const float LogoDisplayHeight =
            LogoDisplayWidth
            * TummlyInvoiceLogoAsset.PixelHeight
            / TummlyInvoiceLogoAsset.PixelWidth;

        public static byte[] Render(TummlyVatInvoice invoice)
        {
            var content = BuildContentStream(invoice);
            return BuildPdf(content, TummlyInvoiceLogoAsset.GetJpegBytes());
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
            var title = isCreditNote ? "Credit note" : "Invoice";
            var vatRatePercent = (invoice.VatRateBps / 100m).ToString(
                "0.##",
                CultureInfo.GetCultureInfo("en-GB")
            );
            var invoiceDate = LondonDateFormat.DMmmYyyy(invoice.InvoiceDateUtc);
            var dueDate = invoiceDate;
            var amountDue = FormatPoundsFromPence(invoice.GrossPence);
            var net = FormatPoundsFromPence(invoice.NetPence);
            var vat = FormatPoundsFromPence(invoice.VatPence);
            var gross = FormatPoundsFromPence(invoice.GrossPence);

            var lineItems = ResolveLineItems(invoice);
            var hasDeliverTo = !string.IsNullOrWhiteSpace(invoice.DeliverToSnapshot);

            var sb = new StringBuilder(8192);

            // Page background
            SetFill(sb, ColorBg);
            FillRect(sb, 0, 0, PageWidth, PageHeight);

            var y = PageHeight - 40f;

            // Brand wordmark (embedded JPEG — PDF cannot draw SVG)
            DrawLogo(sb, MarginLeft, y - LogoDisplayHeight);
            y -= LogoDisplayHeight + 20f;

            // Title + meta
            SetFill(sb, ColorPrimary);
            TextAt(sb, "F2", 22, MarginLeft, y, title);
            y -= 20f;
            SetFill(sb, ColorMuted);
            TextAt(
                sb,
                "F1",
                10,
                MarginLeft,
                y,
                $"Invoice number: {invoice.DocumentNumber}"
            );
            y -= 14f;
            TextAt(sb, "F1", 10, MarginLeft, y, $"Date of issue: {invoiceDate}");
            y -= 14f;
            TextAt(sb, "F1", 10, MarginLeft, y, $"Due date: {dueDate}");
            y -= 14f;
            TextAt(sb, "F1", 10, MarginLeft, y, $"Currency: {invoice.Currency}");
            y -= 18f;

            StrokeLineMuted(sb, MarginLeft, y, ContentRight, y);
            y -= 20f;

            // Party columns
            var colCount = hasDeliverTo ? 3 : 2;
            var colGap = 16f;
            var colWidth = (ContentWidth - (colGap * (colCount - 1))) / colCount;
            var fromX = MarginLeft;
            var billX = MarginLeft + colWidth + colGap;
            var deliverX = billX + colWidth + colGap;

            var fromLines = BuildPartyLines(
                invoice.SellerLegalName,
                invoice.SellerRegisteredAddress,
                invoice.SellerBillingEmail,
                vatLine: null
            );
            var billLines = BuildPartyLines(
                invoice.CustomerBusinessName,
                invoice.CustomerAddress,
                invoice.CustomerBillingEmail,
                vatLine: null
            );
            var deliverLines = hasDeliverTo
                ? SplitSnapshotLines(invoice.DeliverToSnapshot!)
                : [];

            SetFill(sb, ColorPrimary);
            TextAt(sb, "F2", 10, fromX, y, "From");
            TextAt(sb, "F2", 10, billX, y, "Bill to");
            if (hasDeliverTo)
            {
                TextAt(sb, "F2", 10, deliverX, y, "Deliver to");
            }

            y -= 14f;
            var partyBottom = y;
            partyBottom = Math.Min(
                partyBottom,
                DrawPartyBody(sb, fromX, y, fromLines, colWidth)
            );
            partyBottom = Math.Min(
                partyBottom,
                DrawPartyBody(sb, billX, y, billLines, colWidth)
            );
            if (hasDeliverTo)
            {
                partyBottom = Math.Min(
                    partyBottom,
                    DrawPartyBody(sb, deliverX, y, deliverLines, colWidth)
                );
            }

            y = partyBottom - 24f;

            // Amount due headline
            SetFill(sb, ColorPrimary);
            TextAt(sb, "F2", 18, MarginLeft, y, $"{amountDue} due {dueDate}");
            y -= 16f;
            SetFill(sb, ColorMuted);
            var paymentLine = string.IsNullOrWhiteSpace(invoice.PaymentMethodSummary)
                ? (
                    string.Equals(
                        invoice.PaymentStatus,
                        TummlyVatInvoice.PaymentStatusPaid,
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? "Paid"
                        : invoice.PaymentStatus
                )
                : invoice.PaymentMethodSummary.Trim();
            TextAt(sb, "F1", 10, MarginLeft, y, paymentLine);
            y -= 18f;

            StrokeLineMuted(sb, MarginLeft, y, ContentRight, y);
            y -= 8f;

            // Line-item table
            var colQtyW = 36f;
            var colUnitW = 72f;
            var colTaxW = 36f;
            var colAmtW = 72f;
            var colDescW =
                ContentWidth - colQtyW - colUnitW - colTaxW - colAmtW - (16f * 4);
            var xDesc = MarginLeft;
            var xQty = xDesc + colDescW + 16f;
            var xUnit = xQty + colQtyW + 16f;
            var xTax = xUnit + colUnitW + 16f;
            var xAmt = xTax + colTaxW + 16f;

            var headerTop = y;
            var headerBottom = y - 22f;
            SetFill(sb, ColorHeaderBand);
            FillRect(sb, MarginLeft, headerBottom, ContentWidth, headerTop - headerBottom);
            SetFill(sb, ColorMuted);
            TextAt(sb, "F2", 8, xDesc, headerTop - 14f, "Description");
            TextAt(sb, "F2", 8, xQty, headerTop - 14f, "Qty");
            TextAt(sb, "F2", 8, xUnit + 8f, headerTop - 14f, "Unit price");
            TextAt(sb, "F2", 8, xTax, headerTop - 14f, "Tax");
            TextAt(sb, "F2", 8, xAmt + 12f, headerTop - 14f, "Amount");
            y = headerBottom;
            StrokeLineMuted(sb, MarginLeft, y, ContentRight, y);

            foreach (var line in lineItems)
            {
                y -= 14f;
                var taxLabel =
                    $"{(line.VatRateBps / 100m).ToString("0.##", CultureInfo.GetCultureInfo("en-GB"))}%";
                SetFill(sb, ColorPrimary);
                TextAt(sb, "F2", 9, xDesc, y, Truncate(line.Title, 48));
                SetFill(sb, ColorMuted);
                TextAt(
                    sb,
                    "F1",
                    9,
                    xQty,
                    y,
                    line.Quantity.ToString(CultureInfo.InvariantCulture)
                );
                TextAt(
                    sb,
                    "F1",
                    9,
                    xUnit,
                    y,
                    FormatPoundsFromPence(line.UnitNetPence)
                );
                TextAt(sb, "F1", 9, xTax, y, taxLabel);
                SetFill(sb, ColorPrimary);
                TextAt(
                    sb,
                    "F2",
                    9,
                    xAmt,
                    y,
                    FormatPoundsFromPence(line.AmountNetPence)
                );

                if (!string.IsNullOrWhiteSpace(line.Subtitle))
                {
                    y -= 12f;
                    SetFill(sb, ColorMuted);
                    TextAt(sb, "F1", 9, xDesc, y, Truncate(line.Subtitle!, 64));
                }

                y -= 10f;
                StrokeLineMuted(sb, MarginLeft, y, ContentRight, y);
            }

            y -= 20f;

            // Totals
            void TotalRow(string label, string value, bool boldValue)
            {
                SetFill(sb, ColorMuted);
                TextAt(sb, "F1", 9, MarginLeft, y, label);
                SetFill(sb, ColorPrimary);
                TextAt(sb, boldValue ? "F2" : "F1", 9, xAmt, y, value);
                y -= 14f;
            }

            TotalRow("Subtotal", net, boldValue: false);
            TotalRow("Total excluding tax", net, boldValue: false);
            TotalRow($"Tax ({vatRatePercent}% on {net})", vat, boldValue: false);
            TotalRow("Total", gross, boldValue: false);
            TotalRow("Amount due", amountDue, boldValue: true);

            // Footer
            SetFill(sb, ColorMuted);
            TextAt(sb, "F1", 8, MarginLeft, 48f, "Tax = VAT");
            TextAt(sb, "F1", 8, ContentRight - 52f, 48f, "Page 1 of 1");

            // Keep supplier VAT discoverable for sign-off tests / compliance
            if (!string.IsNullOrWhiteSpace(invoice.SellerVatRegistrationNumber))
            {
                TextAt(
                    sb,
                    "F1",
                    7,
                    MarginLeft,
                    36f,
                    $"Supplier VAT number: {invoice.SellerVatRegistrationNumber}"
                );
            }

            return sb.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
        }

        private static IReadOnlyList<TummlyVatInvoiceLineItemDto> ResolveLineItems(
            TummlyVatInvoice invoice
        )
        {
            var parsed = TummlyVatInvoiceLineItems.ParseOrEmpty(invoice.LineItemsJson);
            if (parsed.Count > 0)
            {
                return parsed;
            }

            return
            [
                new TummlyVatInvoiceLineItemDto(
                    Title: invoice.LineDescription,
                    Subtitle: null,
                    Quantity: invoice.Quantity,
                    UnitNetPence: invoice.NetPence,
                    VatRateBps: invoice.VatRateBps,
                    AmountNetPence: invoice.NetPence
                ),
            ];
        }

        private static float DrawPartyBody(
            StringBuilder sb,
            float x,
            float startY,
            IReadOnlyList<string> lines,
            float maxWidthCharsHint
        )
        {
            var y = startY;
            SetFill(sb, ColorMuted);
            var maxChars = Math.Max(18, (int)(maxWidthCharsHint / 5.2f));
            foreach (var raw in lines)
            {
                foreach (var wrapped in WrapText(raw, maxChars))
                {
                    TextAt(sb, "F1", 8, x, y, wrapped);
                    y -= 11f;
                }
            }

            return y;
        }

        private static IReadOnlyList<string> BuildPartyLines(
            string name,
            string address,
            string? email,
            string? vatLine
        )
        {
            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(name))
            {
                lines.Add(name.Trim());
            }

            if (!string.IsNullOrWhiteSpace(address))
            {
                foreach (
                    var part in address.Split(
                        [',', '\n', '\r'],
                        StringSplitOptions.RemoveEmptyEntries
                            | StringSplitOptions.TrimEntries
                    )
                )
                {
                    if (part.Length > 0)
                    {
                        lines.Add(part);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                lines.Add(email.Trim());
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

        private static IReadOnlyList<string> SplitSnapshotLines(string snapshot)
        {
            return snapshot
                .Split(
                    ['\n', '\r'],
                    StringSplitOptions.RemoveEmptyEntries
                        | StringSplitOptions.TrimEntries
                )
                .Where(line => line.Length > 0)
                .ToList();
        }

        private static void DrawLogo(StringBuilder sb, float x, float y)
        {
            sb.Append("q\n");
            sb.Append(LogoDisplayWidth.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(" 0 0 ");
            sb.Append(LogoDisplayHeight.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(x.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(y.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(" cm\n/Im1 Do\nQ\n");
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

        private static string Truncate(string value, int maxChars)
        {
            var trimmed = value.Trim();
            if (trimmed.Length <= maxChars)
            {
                return trimmed;
            }

            return trimmed[..(maxChars - 1)] + "…";
        }

        private static void SetFill(
            StringBuilder sb,
            (float R, float G, float B) color
        )
        {
            sb.Append(color.R.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(color.G.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(color.B.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append(" rg\n");
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

        private static void StrokeLineMuted(
            StringBuilder sb,
            float x1,
            float y1,
            float x2,
            float y2
        )
        {
            SetFill(sb, ColorMuted);
            sb.Append(ColorMuted.R.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(ColorMuted.G.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(ColorMuted.B.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append(" RG\n");
            sb.Append(x1.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(y1.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(" m ");
            sb.Append(x2.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(y2.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(" l S\n");
        }

        private static void FillRect(
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
            sb.Append(" re f\n");
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
                else if (ch == '…')
                {
                    sb.Append("...");
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

        private static byte[] BuildPdf(string contentStream, byte[] jpegBytes)
        {
            var contentBytes = Encoding.ASCII.GetBytes(contentStream);
            var pageResources =
                "3 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] "
                + "/Contents 4 0 R /Resources<< /Font<< /F1 5 0 R /F2 6 0 R >> "
                + "/XObject<< /Im1 7 0 R >> >> >>endobj\n";
            var contentObj =
                $"4 0 obj<< /Length {contentBytes.Length} >>stream\n";
            var contentObjEnd = "endstream\nendobj\n";
            var imageHeader =
                "7 0 obj<< /Type /XObject /Subtype /Image "
                + $"/Width {TummlyInvoiceLogoAsset.PixelWidth} "
                + $"/Height {TummlyInvoiceLogoAsset.PixelHeight} "
                + "/ColorSpace /DeviceRGB /BitsPerComponent 8 "
                + $"/Filter /DCTDecode /Length {jpegBytes.Length} >>stream\n";
            var imageFooter = "\nendstream\nendobj\n";

            var objects = new List<byte[]>
            {
                Encoding.ASCII.GetBytes(
                    "1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj\n"
                ),
                Encoding.ASCII.GetBytes(
                    "2 0 obj<< /Type /Pages /Kids [3 0 R] /Count 1 >>endobj\n"
                ),
                Encoding.ASCII.GetBytes(pageResources),
                Concat(
                    Encoding.ASCII.GetBytes(contentObj),
                    contentBytes,
                    Encoding.ASCII.GetBytes(contentObjEnd)
                ),
                Encoding.ASCII.GetBytes(
                    "5 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>endobj\n"
                ),
                Encoding.ASCII.GetBytes(
                    "6 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>endobj\n"
                ),
                Concat(
                    Encoding.ASCII.GetBytes(imageHeader),
                    jpegBytes,
                    Encoding.ASCII.GetBytes(imageFooter)
                ),
            };

            using var output = new MemoryStream();
            var header = Encoding.ASCII.GetBytes("%PDF-1.4\n");
            output.Write(header);
            var offsets = new List<int> { 0 };
            foreach (var obj in objects)
            {
                offsets.Add((int)output.Position);
                output.Write(obj);
            }

            var xrefPos = (int)output.Position;
            var xref = new StringBuilder();
            xref.Append($"xref\n0 {objects.Count + 1}\n");
            xref.Append("0000000000 65535 f \n");
            for (var i = 1; i < offsets.Count; i++)
            {
                xref.Append(offsets[i].ToString("D10", CultureInfo.InvariantCulture));
                xref.Append(" 00000 n \n");
            }

            xref.Append($"trailer<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
            xref.Append("startxref\n");
            xref.Append(xrefPos);
            xref.Append("\n%%EOF\n");
            output.Write(Encoding.ASCII.GetBytes(xref.ToString()));
            return output.ToArray();
        }

        private static byte[] Concat(params byte[][] parts)
        {
            var length = parts.Sum(part => part.Length);
            var buffer = new byte[length];
            var offset = 0;
            foreach (var part in parts)
            {
                Buffer.BlockCopy(part, 0, buffer, offset, part.Length);
                offset += part.Length;
            }

            return buffer;
        }
    }
}
