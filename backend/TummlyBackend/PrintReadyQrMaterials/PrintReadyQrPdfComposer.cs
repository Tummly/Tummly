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
            string? offerHeadline,
            CounterCardBranding? counterBranding = null,
            CounterCardAssetPaths? counterAssets = null
        )
        {
            if (qrType == QrType.CounterCard)
            {
                return ComposeCounterCard(
                    qrImage,
                    counterBranding ?? CounterCardBranding.Empty,
                    counterAssets
                );
            }

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
        /// A5 (148×210 mm) Counter Display PDF: Design green card with pattern,
        /// restaurant logo, Location name/address, production QR, and powered strip.
        /// </summary>
        public static byte[] ComposeCounterCard(
            QrRasterImage qrImage,
            CounterCardBranding branding,
            CounterCardAssetPaths? assets = null
        )
        {
            const double widthMm = 148;
            const double heightMm = 210;
            const double qrMm = 52;
            var widthPt = (float)(widthMm * MmToPt);
            var heightPt = (float)(heightMm * MmToPt);
            var qrPt = (float)(qrMm * MmToPt);
            var qrX = (widthPt - qrPt) / 2f;
            var qrY = (float)(88 * MmToPt);

            // Design Counter Card green (approx from A5 reference PDF).
            var pageGreen = new SKColor(0x3D, 0xC9, 0x6C);

            using var output = new MemoryStream();
            var metadata = SKDocumentPdfMetadata.Default;
            metadata.Title = "Tummly Counter Display";
            using (var document = SKDocument.CreatePdf(output, metadata))
            {
                var canvas = document.BeginPage(widthPt, heightPt);
                canvas.Clear(pageGreen);

                DrawSvgFitted(
                    canvas,
                    assets?.PatternSvgPath,
                    x: 0,
                    y: 0,
                    destWidth: widthPt,
                    destHeight: (float)(78 * MmToPt)
                );

                DrawCounterLogo(
                    canvas,
                    branding,
                    centerX: widthPt / 2f,
                    topY: (float)(14 * MmToPt),
                    sizePt: (float)(22 * MmToPt)
                );

                DrawCounterCenteredText(
                    canvas,
                    "HOW WAS YOUR VISIT?",
                    centerX: widthPt / 2f,
                    baselineY: (float)(48 * MmToPt),
                    fontSizePt: 16f,
                    bold: true,
                    color: SKColors.Black,
                    maxWidthPt: widthPt - (float)(16 * MmToPt)
                );
                DrawCounterCenteredText(
                    canvas,
                    "Share quick private feedback and get your thank-you offer.",
                    centerX: widthPt / 2f,
                    baselineY: (float)(58 * MmToPt),
                    fontSizePt: 8f,
                    bold: false,
                    color: SKColors.Black,
                    maxWidthPt: widthPt - (float)(20 * MmToPt)
                );

                // White QR well.
                using (var wellPaint = new SKPaint
                {
                    IsAntialias = true,
                    Color = SKColors.White,
                    Style = SKPaintStyle.Fill,
                })
                {
                    var pad = (float)(4 * MmToPt);
                    canvas.DrawRoundRect(
                        qrX - pad,
                        qrY - pad,
                        qrPt + pad * 2,
                        qrPt + pad * 2,
                        4,
                        4,
                        wellPaint
                    );
                }

                DrawQr(canvas, qrImage, qrX, qrY, qrPt, qrPt);

                DrawScanPill(
                    canvas,
                    centerX: widthPt / 2f,
                    topY: qrY + qrPt + (float)(4 * MmToPt)
                );

                var name = string.IsNullOrWhiteSpace(branding.LocationName)
                    ? "Your location"
                    : branding.LocationName.Trim();
                DrawCounterCenteredText(
                    canvas,
                    name,
                    centerX: widthPt / 2f,
                    baselineY: (float)(162 * MmToPt),
                    fontSizePt: 12f,
                    bold: true,
                    color: SKColors.Black,
                    maxWidthPt: widthPt - (float)(16 * MmToPt)
                );

                if (!string.IsNullOrWhiteSpace(branding.AddressLine))
                {
                    DrawCounterCenteredText(
                        canvas,
                        branding.AddressLine.Trim(),
                        centerX: widthPt / 2f,
                        baselineY: (float)(172 * MmToPt),
                        fontSizePt: 8f,
                        bold: false,
                        color: SKColors.Black,
                        maxWidthPt: widthPt - (float)(16 * MmToPt)
                    );
                }

                var stripHeight = (float)(14 * MmToPt);
                DrawSvgFitted(
                    canvas,
                    assets?.StripSvgPath,
                    x: 0,
                    y: heightPt - stripHeight,
                    destWidth: widthPt,
                    destHeight: stripHeight
                );

                var poweredHeight = (float)(4 * MmToPt);
                var poweredWidth = (float)(28 * MmToPt);
                DrawSvgFitted(
                    canvas,
                    assets?.PoweredSvgPath,
                    x: (widthPt - poweredWidth) / 2f,
                    y: heightPt - stripHeight - poweredHeight - (float)(4 * MmToPt),
                    destWidth: poweredWidth,
                    destHeight: poweredHeight
                );

                document.EndPage();
                document.Close();
            }

            return output.ToArray();
        }

        private static void DrawScanPill(SKCanvas canvas, float centerX, float topY)
        {
            const string label = "SCAN QR CODE";
            using var typeface = LoadHeadlineTypeface();
            using var font = new SKFont(typeface, 7f);
            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.White,
            };
            var textWidth = font.MeasureText(label, textPaint);
            var padX = (float)(4 * MmToPt);
            var padY = (float)(2 * MmToPt);
            var metrics = font.Metrics;
            var textHeight = metrics.Descent - metrics.Ascent;
            var pillWidth = textWidth + padX * 2;
            var pillHeight = textHeight + padY * 2;
            var left = centerX - pillWidth / 2f;
            using var pillPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.Black,
                Style = SKPaintStyle.Fill,
            };
            canvas.DrawRoundRect(
                left,
                topY,
                pillWidth,
                pillHeight,
                pillHeight / 2f,
                pillHeight / 2f,
                pillPaint
            );
            var baseline = topY + padY - metrics.Ascent;
            using var textPath = font.GetTextPath(
                label,
                new SKPoint(centerX - textWidth / 2f, baseline)
            );
            canvas.DrawPath(textPath, textPaint);
        }

        private static void DrawCounterLogo(
            SKCanvas canvas,
            CounterCardBranding branding,
            float centerX,
            float topY,
            float sizePt
        )
        {
            var left = centerX - sizePt / 2f;
            var dest = new SKRect(left, topY, left + sizePt, topY + sizePt);
            if (
                branding.LogoBytes is { Length: > 0 }
                && TryDecodeBitmap(branding.LogoBytes, out var bitmap)
            )
            {
                using (bitmap)
                {
                    using var image = SKImage.FromBitmap(bitmap);
                    canvas.DrawImage(
                        image,
                        dest,
                        new SKSamplingOptions(
                            SKFilterMode.Linear,
                            SKMipmapMode.Linear
                        ),
                        null
                    );
                }

                return;
            }

            using var placeholder = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(0x8B, 0x1E, 0x1E),
                Style = SKPaintStyle.Fill,
            };
            canvas.DrawRoundRect(dest, 6, 6, placeholder);
        }

        private static bool TryDecodeBitmap(byte[] bytes, out SKBitmap bitmap)
        {
            var decoded = SKBitmap.Decode(bytes);
            if (decoded == null)
            {
                bitmap = null!;
                return false;
            }

            bitmap = decoded;
            return true;
        }

        private static void DrawCounterCenteredText(
            SKCanvas canvas,
            string text,
            float centerX,
            float baselineY,
            float fontSizePt,
            bool bold,
            SKColor color,
            float maxWidthPt
        )
        {
            using var typeface = bold
                ? LoadHeadlineTypeface()
                : LoadRegularTypeface();
            using var paint = new SKPaint { IsAntialias = true, Color = color };
            using var font = new SKFont(typeface, fontSizePt);
            var width = font.MeasureText(text, paint);
            while (width > maxWidthPt && font.Size > 6f)
            {
                font.Size -= 0.5f;
                width = font.MeasureText(text, paint);
            }

            using var textPath = font.GetTextPath(
                text,
                new SKPoint(centerX - width / 2f, baselineY)
            );
            canvas.DrawPath(textPath, paint);
        }

        private static void DrawSvgFitted(
            SKCanvas canvas,
            string? svgPath,
            float x,
            float y,
            float destWidth,
            float destHeight
        )
        {
            if (
                string.IsNullOrWhiteSpace(svgPath)
                || !File.Exists(svgPath)
                || destWidth <= 0
                || destHeight <= 0
            )
            {
                return;
            }

            try
            {
                using var loaded = LoadTemplatePicture(svgPath);
                var bounds = loaded.Bounds;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    return;
                }

                var scale = Math.Min(
                    destWidth / bounds.Width,
                    destHeight / bounds.Height
                );
                canvas.Save();
                canvas.Translate(x, y);
                canvas.Scale(scale);
                canvas.Translate(-bounds.Left, -bounds.Top);
                canvas.DrawPicture(loaded.Picture);
                canvas.Restore();
            }
            catch
            {
                // Optional artwork — page green + text still print without it.
            }
        }

        private static SKTypeface LoadRegularTypeface()
        {
            var fontDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.Fonts
            );
            var candidates = new[]
            {
                Path.Combine(fontDirectory, "arial.ttf"),
                "/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf",
                "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
                "/usr/share/fonts/liberation/LiberationSans-Regular.ttf",
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

            return LoadHeadlineTypeface();
        }

        /// <summary>
        /// Resolves Counter Card SVG parts next to the print template pack.
        /// </summary>
        public static CounterCardAssetPaths? TryResolveCounterCardAssets(
            string contentRootPath
        )
        {
            try
            {
                var packDir = PrintTemplatePack.ResolvePackDirectory(
                    contentRootPath
                );
                var counterDir = Path.Combine(packDir, "counter-card");
                if (!Directory.Exists(counterDir))
                {
                    return null;
                }

                string? Pick(string fileName)
                {
                    var path = Path.Combine(counterDir, fileName);
                    return File.Exists(path) ? path : null;
                }

                return new CounterCardAssetPaths(
                    PatternSvgPath: Pick("group6.svg"),
                    StripSvgPath: Pick("strip.svg"),
                    PoweredSvgPath: Pick("powered.svg")
                );
            }
            catch
            {
                return null;
            }
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
