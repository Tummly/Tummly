using SkiaSharp;
using Svg.Skia;
using TummlyBackend.Models;

namespace TummlyBackend.PrintReadyQrMaterials
{
    /// <summary>
    /// Renders the Dev SVG artwork to a vector PDF, then places the dynamic
    /// QR and optional offer headline at the pack's millimetre slots.
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
            var (widthPt, heightPt) = PrintTemplatePack.ReadSvgViewBoxPoints(svgPath);
            var placement = ResolveQrPlacementPoints(
                widthPt,
                heightPt,
                qrSlot
            );
            return RenderTemplatePdf(
                svgPath,
                qrImage,
                placement.XPt,
                placement.YPt,
                placement.WidthPt,
                placement.HeightPt,
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
            var qr = pack.OfferCardQr;
            var headline = pack.OfferCardHeadline;
            var (widthPt, heightPt) = PrintTemplatePack.ReadSvgViewBoxPoints(
                pack.CardSvgPath
            );
            var placement = ResolveQrPlacementPoints(
                widthPt,
                heightPt,
                qr
            );
            return RenderTemplatePdf(
                pack.CardSvgPath,
                qrImage,
                placement.XPt,
                placement.YPt,
                placement.WidthPt,
                placement.HeightPt,
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

        public static (
            double XPt,
            double YPt,
            double WidthPt,
            double HeightPt
        ) ResolveQrPlacementPoints(
            double canvasWidthPt,
            double canvasHeightPt,
            PrintSlotQrSpec slot
        )
        {
            var (xMm, yMm) = ResolveQrTopLeft(
                canvasWidthPt / MmToPt,
                canvasHeightPt / MmToPt,
                slot
            );
            return (
                xMm * MmToPt,
                yMm * MmToPt,
                slot.WidthMm * MmToPt,
                slot.HeightMm * MmToPt
            );
        }

        private static byte[] RenderTemplatePdf(
            string svgPath,
            QrRasterImage qrImage,
            double qrXFromLeftPt,
            double qrYFromTopPt,
            double qrWidthPt,
            double qrHeightPt,
            string? headline,
            PrintSlotRectSpec? headlineBox
        )
        {
            using var svg = new SKSvg();
            var picture = svg.Load(svgPath)
                ?? throw new InvalidOperationException(
                    $"Print template SVG could not be loaded: {svgPath}"
                );
            var bounds = picture.CullRect;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                throw new InvalidOperationException(
                    $"Print template SVG has an invalid viewBox: {svgPath}"
                );
            }

            using var output = new MemoryStream();
            var metadata = SKDocumentPdfMetadata.Default;
            metadata.Title = "Tummly print-ready QR material";
            metadata.Subject = headline;
            using (var document = SKDocument.CreatePdf(output, metadata))
            {
                var canvas = document.BeginPage(bounds.Width, bounds.Height);
                canvas.Clear(SKColors.White);

                canvas.Save();
                canvas.Translate(-bounds.Left, -bounds.Top);
                canvas.DrawPicture(picture);
                canvas.Restore();

                if (headline is not null && headlineBox is not null)
                {
                    DrawHeadline(canvas, headline, headlineBox);
                }

                DrawQr(
                    canvas,
                    qrImage,
                    (float)qrXFromLeftPt,
                    (float)qrYFromTopPt,
                    (float)qrWidthPt,
                    (float)qrHeightPt
                );

                document.EndPage();
                document.Close();
            }

            return output.ToArray();
        }

        private static void DrawQr(
            SKCanvas canvas,
            QrRasterImage qrImage,
            float x,
            float y,
            float width,
            float height
        )
        {
            if (qrImage.RgbBytes.Length != qrImage.Width * qrImage.Height * 3)
            {
                throw new InvalidOperationException(
                    "QR raster RGB byte count does not match its dimensions."
                );
            }

            using var bitmap = new SKBitmap(
                new SKImageInfo(
                    qrImage.Width,
                    qrImage.Height,
                    SKColorType.Bgra8888,
                    SKAlphaType.Opaque
                )
            );
            for (var row = 0; row < qrImage.Height; row++)
            {
                for (var column = 0; column < qrImage.Width; column++)
                {
                    var offset = (row * qrImage.Width + column) * 3;
                    bitmap.SetPixel(
                        column,
                        row,
                        new SKColor(
                            qrImage.RgbBytes[offset],
                            qrImage.RgbBytes[offset + 1],
                            qrImage.RgbBytes[offset + 2]
                        )
                    );
                }
            }

            using var image = SKImage.FromBitmap(bitmap);
            canvas.DrawImage(
                image,
                new SKRect(x, y, x + width, y + height),
                new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None),
                null
            );
        }

        private static void DrawHeadline(
            SKCanvas canvas,
            string headline,
            PrintSlotRectSpec box
        )
        {
            var boxWidth = (float)(box.WidthMm * MmToPt);
            var boxHeight = (float)(box.HeightMm * MmToPt);
            using var typeface = SKTypeface.FromFamilyName(
                "Arial",
                SKFontStyle.Bold
            );
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(0x16, 0x1a, 0x18),
            };
            using var font = new SKFont(
                typeface,
                Math.Min(11f, boxHeight * 0.32f)
            );

            var lines = WrapHeadline(headline, font, paint, boxWidth);
            while (lines.Count > 2 && font.Size > 7f)
            {
                font.Size -= 0.5f;
                lines = WrapHeadline(headline, font, paint, boxWidth);
            }

            lines = lines.Take(2).ToList();
            var metrics = font.Metrics;
            var lineHeight = (metrics.Descent - metrics.Ascent) * 1.1f;
            var textHeight = lineHeight * lines.Count;
            var x = (float)(box.XMm * MmToPt);
            var y =
                (float)(box.YMm * MmToPt)
                + (boxHeight - textHeight) / 2f
                - metrics.Ascent;

            foreach (var line in lines)
            {
                canvas.DrawText(
                    line,
                    x,
                    y,
                    SKTextAlign.Left,
                    font,
                    paint
                );
                y += lineHeight;
            }
        }

        private static List<string> WrapHeadline(
            string headline,
            SKFont font,
            SKPaint paint,
            float maxWidth
        )
        {
            var lines = new List<string>();
            var current = string.Empty;
            foreach (
                var word in headline.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries
                )
            )
            {
                var candidate = current.Length == 0 ? word : $"{current} {word}";
                if (
                    current.Length == 0
                    || font.MeasureText(candidate, paint) <= maxWidth
                )
                {
                    current = candidate;
                    continue;
                }

                lines.Add(current);
                current = word;
            }

            if (current.Length > 0)
            {
                lines.Add(current);
            }

            return lines;
        }
    }
}
