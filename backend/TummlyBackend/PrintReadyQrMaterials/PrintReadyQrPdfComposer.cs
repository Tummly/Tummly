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

        /// <summary>
        /// Placeholder QR payload for Shop Offer Card setup previews.
        /// Mint replaces this with the location guest link.
        /// </summary>
        public const string PreviewQrPayload =
            "https://tummly.app/offer-card-preview";

        public static byte[] Compose(
            PrintTemplatePackSnapshot pack,
            QrType qrType,
            QrRasterImage qrImage,
            string? offerHeadline
        )
        {
            var plan = PlanCompose(pack, qrType, offerHeadline);
            return RenderTemplatePdf(
                plan.SvgPath,
                qrImage,
                plan.QrXPt,
                plan.QrYPt,
                plan.QrWidthPt,
                plan.QrHeightPt,
                plan.Headline,
                plan.HeadlineBox
            );
        }

        /// <summary>
        /// Raster preview that paints the same template, headline, and QR
        /// slots as <see cref="Compose"/> (mint PDF).
        /// </summary>
        public static byte[] ComposePng(
            PrintTemplatePackSnapshot pack,
            QrType qrType,
            QrRasterImage qrImage,
            string? offerHeadline,
            float scale = 2f
        )
        {
            var plan = PlanCompose(pack, qrType, offerHeadline);
            return RenderTemplatePng(
                plan.SvgPath,
                qrImage,
                plan.QrXPt,
                plan.QrYPt,
                plan.QrWidthPt,
                plan.QrHeightPt,
                plan.Headline,
                plan.HeadlineBox,
                scale
            );
        }

        private readonly record struct ComposePlan(
            string SvgPath,
            double QrXPt,
            double QrYPt,
            double QrWidthPt,
            double QrHeightPt,
            string? Headline,
            PrintSlotRectSpec? HeadlineBox
        );

        private static ComposePlan PlanCompose(
            PrintTemplatePackSnapshot pack,
            QrType qrType,
            string? offerHeadline
        )
        {
            return qrType switch
            {
                QrType.TableTent => PlanTentOrSticker(
                    pack.TentStickerEmptySvgPath,
                    pack.TableTentQr
                ),
                QrType.WindowSticker => PlanTentOrSticker(
                    pack.TentStickerEmptySvgPath,
                    pack.WindowStickerQr
                ),
                QrType.OfferCard => PlanOfferCard(pack, offerHeadline),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(qrType),
                    qrType,
                    "Unsupported QR type for print materials."
                ),
            };
        }

        private static ComposePlan PlanTentOrSticker(
            string svgPath,
            PrintSlotQrSpec qrSlot
        )
        {
            var (widthPt, heightPt) = PrintTemplatePack.ReadSvgViewBoxPoints(svgPath);
            var placement = ResolveQrPlacementPoints(
                widthPt,
                heightPt,
                qrSlot
            );
            return new ComposePlan(
                svgPath,
                placement.XPt,
                placement.YPt,
                placement.WidthPt,
                placement.HeightPt,
                Headline: null,
                HeadlineBox: null
            );
        }

        private static ComposePlan PlanOfferCard(
            PrintTemplatePackSnapshot pack,
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
            return new ComposePlan(
                pack.CardSvgPath,
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
            using var loaded = LoadTemplatePicture(svgPath);
            using var output = new MemoryStream();
            var metadata = SKDocumentPdfMetadata.Default;
            metadata.Title = "Tummly print-ready QR material";
            metadata.Subject = headline;
            using (var document = SKDocument.CreatePdf(output, metadata))
            {
                var canvas = document.BeginPage(
                    loaded.Bounds.Width,
                    loaded.Bounds.Height
                );
                PaintTemplate(
                    canvas,
                    loaded.Picture,
                    loaded.Bounds,
                    qrImage,
                    qrXFromLeftPt,
                    qrYFromTopPt,
                    qrWidthPt,
                    qrHeightPt,
                    headline,
                    headlineBox
                );
                document.EndPage();
                document.Close();
            }

            return output.ToArray();
        }

        private static byte[] RenderTemplatePng(
            string svgPath,
            QrRasterImage qrImage,
            double qrXFromLeftPt,
            double qrYFromTopPt,
            double qrWidthPt,
            double qrHeightPt,
            string? headline,
            PrintSlotRectSpec? headlineBox,
            float scale
        )
        {
            if (scale <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(scale),
                    scale,
                    "PNG preview scale must be greater than zero."
                );
            }

            using var loaded = LoadTemplatePicture(svgPath);
            var width = Math.Max(
                1,
                (int)Math.Ceiling(loaded.Bounds.Width * scale)
            );
            var height = Math.Max(
                1,
                (int)Math.Ceiling(loaded.Bounds.Height * scale)
            );
            using var bitmap = new SKBitmap(
                new SKImageInfo(
                    width,
                    height,
                    SKColorType.Rgba8888,
                    SKAlphaType.Premul
                )
            );
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Scale(scale);
                PaintTemplate(
                    canvas,
                    loaded.Picture,
                    loaded.Bounds,
                    qrImage,
                    qrXFromLeftPt,
                    qrYFromTopPt,
                    qrWidthPt,
                    qrHeightPt,
                    headline,
                    headlineBox
                );
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data =
                image.Encode(SKEncodedImageFormat.Png, 100)
                ?? throw new InvalidOperationException(
                    "Offer Card preview PNG encode failed."
                );
            return data.ToArray();
        }

        private readonly record struct LoadedTemplate(
            SKSvg Svg,
            SKPicture Picture,
            SKRect Bounds
        ) : IDisposable
        {
            public void Dispose() => Svg.Dispose();
        }

        private static LoadedTemplate LoadTemplatePicture(string svgPath)
        {
            var svg = new SKSvg();
            var picture = svg.Load(svgPath)
                ?? throw new InvalidOperationException(
                    $"Print template SVG could not be loaded: {svgPath}"
                );
            var bounds = picture.CullRect;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                svg.Dispose();
                throw new InvalidOperationException(
                    $"Print template SVG has an invalid viewBox: {svgPath}"
                );
            }

            return new LoadedTemplate(svg, picture, bounds);
        }

        private static void PaintTemplate(
            SKCanvas canvas,
            SKPicture picture,
            SKRect bounds,
            QrRasterImage qrImage,
            double qrXFromLeftPt,
            double qrYFromTopPt,
            double qrWidthPt,
            double qrHeightPt,
            string? headline,
            PrintSlotRectSpec? headlineBox
        )
        {
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

        /// <summary>
        /// Offer Card headline: wrap by width first (1 → 2 → 3 lines), then
        /// shrink the paint only when the title still needs more than three
        /// lines. Card geometry is unchanged.
        /// </summary>
        private const int MaxHeadlineLines = 3;

        private const float MinHeadlineFontPt = 7f;

        private static void DrawHeadline(
            SKCanvas canvas,
            string headline,
            PrintSlotRectSpec box
        )
        {
            var boxWidth = (float)(box.WidthMm * MmToPt);
            var boxHeight = (float)(box.HeightMm * MmToPt);
            using var typeface = LoadHeadlineTypeface();
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(0x16, 0x1a, 0x18),
            };
            using var font = new SKFont(
                typeface,
                Math.Min(11f, boxHeight * 0.32f)
            );

            // Width-wrap at the current size. Longer titles naturally move
            // from one line to two, then to three. Only shrink after that.
            var lines = WrapHeadline(headline, font, paint, boxWidth);
            while (lines.Count > MaxHeadlineLines && font.Size > MinHeadlineFontPt)
            {
                font.Size -= 0.5f;
                lines = WrapHeadline(headline, font, paint, boxWidth);
            }

            lines = lines.Take(MaxHeadlineLines).ToList();
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
                // The Linux PDF backend can omit text when its resolved
                // system font cannot be embedded. Convert glyphs to vector
                // outlines so the printable headline is always visible.
                using var textPath = font.GetTextPath(
                    line,
                    new SKPoint(x, y)
                );
                canvas.DrawPath(textPath, paint);
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

        private static SKTypeface LoadHeadlineTypeface()
        {
            var fontDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.Fonts
            );
            var candidates = new[]
            {
                Path.Combine(fontDirectory, "arialbd.ttf"),
                "/usr/share/fonts/truetype/liberation2/LiberationSans-Bold.ttf",
                "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
                "/usr/share/fonts/liberation/LiberationSans-Bold.ttf",
            };

            foreach (var path in candidates)
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    var typeface = SKTypeface.FromFile(path);
                    if (typeface != null)
                    {
                        return typeface;
                    }
                }
            }

            throw new InvalidOperationException(
                "The printable Offer headline font is not installed."
            );
        }
    }
}
