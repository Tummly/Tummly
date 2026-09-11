using System.Globalization;
using System.Text;
using TummlyBackend.Models;

namespace TummlyBackend.PrintReadyQrMaterials
{
    /// <summary>
    /// Hand-rolled PDF page sized to the Dev SVG viewBox (mm→pt) with QR PNG
    /// pixels and optional offer headline at pack millimetre slots.
    /// </summary>
    public static class PrintReadyQrPdfComposer
    {
        public const string ContentType = "application/pdf";

        private const double MmToPt = 72.0 / 25.4;

        public static byte[] Compose(
            PrintTemplatePackSnapshot pack,
            QrType qrType,
            QrRasterImage qrImage,
            string? offerHeadline
        )
        {
            return qrType switch
            {
                QrType.TableTent => ComposeTentOrSticker(
                    pack.TentStickerEmptySvgPath,
                    pack.TableTentQr,
                    qrImage
                ),
                QrType.WindowSticker => ComposeTentOrSticker(
                    pack.TentStickerEmptySvgPath,
                    pack.WindowStickerQr,
                    qrImage
                ),
                QrType.OfferCard => ComposeOfferCard(pack, qrImage, offerHeadline),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(qrType),
                    qrType,
                    "Unsupported QR type for print materials."
                ),
            };
        }

        private static byte[] ComposeTentOrSticker(
            string svgPath,
            PrintSlotQrSpec qrSlot,
            QrRasterImage qrImage
        )
        {
            var (widthMm, heightMm) = PrintTemplatePack.ReadSvgViewBoxMm(svgPath);
            var (xMm, yMm) = ResolveQrTopLeft(widthMm, heightMm, qrSlot);
            return BuildSinglePagePdf(
                widthMm * MmToPt,
                heightMm * MmToPt,
                qrImage,
                xMm * MmToPt,
                yMm * MmToPt,
                qrSlot.WidthMm * MmToPt,
                qrSlot.HeightMm * MmToPt,
                headline: null,
                headlineBox: null
            );
        }

        private static byte[] ComposeOfferCard(
            PrintTemplatePackSnapshot pack,
            QrRasterImage qrImage,
            string? offerHeadline
        )
        {
            var (widthMm, heightMm) = PrintTemplatePack.ReadSvgViewBoxMm(pack.CardSvgPath);
            var qr = pack.OfferCardQr;
            var headline = pack.OfferCardHeadline;
            return BuildSinglePagePdf(
                widthMm * MmToPt,
                heightMm * MmToPt,
                qrImage,
                (qr.XMm ?? 0) * MmToPt,
                (qr.YMm ?? 0) * MmToPt,
                qr.WidthMm * MmToPt,
                qr.HeightMm * MmToPt,
                offerHeadline ?? pack.DefaultOfferHeadline,
                headline
            );
        }

        public static (double XMm, double YMm) ResolveQrTopLeft(
            double canvasWidthMm,
            double canvasHeightMm,
            PrintSlotQrSpec slot
        )
        {
            if (slot.XMm is double x && slot.YMm is double y)
            {
                return (x, y);
            }

            if (!slot.CentreWhenBlank)
            {
                throw new InvalidOperationException(
                    "QR slot is missing X/Y and centreWhenBlank is false."
                );
            }

            return (
                (canvasWidthMm - slot.WidthMm) / 2.0,
                (canvasHeightMm - slot.HeightMm) / 2.0
            );
        }

        private static byte[] BuildSinglePagePdf(
            double pageWidthPt,
            double pageHeightPt,
            QrRasterImage qrImage,
            double qrXFromLeftPt,
            double qrYFromTopPt,
            double qrWidthPt,
            double qrHeightPt,
            string? headline,
            PrintSlotRectSpec? headlineBox
        )
        {
            // PDF origin is bottom-left; SVG slot Y is from top.
            var qrYPdf = pageHeightPt - qrYFromTopPt - qrHeightPt;

            var content = new StringBuilder(512);
            // White page fill
            content.Append("1 1 1 rg 0 0 ");
            content.Append(F(pageWidthPt));
            content.Append(' ');
            content.Append(F(pageHeightPt));
            content.Append(" re f\n");

            if (headline is not null && headlineBox is not null)
            {
                var textYPdf =
                    pageHeightPt
                    - (headlineBox.YMm * MmToPt)
                    - (headlineBox.HeightMm * MmToPt / 2.0);
                var textX = headlineBox.XMm * MmToPt;
                var fontSize = Math.Min(11.0, headlineBox.HeightMm * MmToPt * 0.45);
                content.Append("0 0 0 rg\n");
                content.Append("BT /F1 ");
                content.Append(F(fontSize));
                content.Append(" Tf ");
                content.Append(F(textX));
                content.Append(' ');
                content.Append(F(textYPdf));
                content.Append(" Td (");
                content.Append(EscapePdfText(SanitizeAscii(headline)));
                content.Append(") Tj ET\n");
            }

            // Black-on-white QR image
            content.Append("q ");
            content.Append(F(qrWidthPt));
            content.Append(" 0 0 ");
            content.Append(F(qrHeightPt));
            content.Append(' ');
            content.Append(F(qrXFromLeftPt));
            content.Append(' ');
            content.Append(F(qrYPdf));
            content.Append(" cm /Im1 Do Q\n");

            var contentBytes = Encoding.ASCII.GetBytes(content.ToString());
            var imageBytes = qrImage.RgbBytes;

            var objects = new List<byte[]>
            {
                Encoding.ASCII.GetBytes(
                    "1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj\n"
                ),
                Encoding.ASCII.GetBytes(
                    "2 0 obj<< /Type /Pages /Kids [3 0 R] /Count 1 >>endobj\n"
                ),
                Encoding.ASCII.GetBytes(
                    "3 0 obj<< /Type /Page /Parent 2 0 R "
                        + $"/MediaBox [0 0 {F(pageWidthPt)} {F(pageHeightPt)}] "
                        + "/Contents 4 0 R "
                        + "/Resources<< /Font<< /F1 5 0 R >> "
                        + "/XObject<< /Im1 6 0 R >> >> >>endobj\n"
                ),
                Concat(
                    Encoding.ASCII.GetBytes(
                        $"4 0 obj<< /Length {contentBytes.Length} >>stream\n"
                    ),
                    contentBytes,
                    Encoding.ASCII.GetBytes("\nendstream\nendobj\n")
                ),
                Encoding.ASCII.GetBytes(
                    "5 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>endobj\n"
                ),
                Concat(
                    Encoding.ASCII.GetBytes(
                        "6 0 obj<< /Type /XObject /Subtype /Image "
                            + $"/Width {qrImage.Width} /Height {qrImage.Height} "
                            + "/ColorSpace /DeviceRGB /BitsPerComponent 8 "
                            + $"/Length {imageBytes.Length} >>stream\n"
                    ),
                    imageBytes,
                    Encoding.ASCII.GetBytes("\nendstream\nendobj\n")
                ),
            };

            using var output = new MemoryStream();
            output.Write(Encoding.ASCII.GetBytes("%PDF-1.4\n"));
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

        private static string F(double value) =>
            value.ToString("0.##", CultureInfo.InvariantCulture);

        private static string SanitizeAscii(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (ch >= 32 && ch <= 126)
                {
                    sb.Append(ch);
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

        private static string EscapePdfText(string value) =>
            value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);

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
