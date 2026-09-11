using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;

namespace TummlyBackend.PrintReadyQrMaterials
{
    public sealed class PrintTemplatePack
    {
        public const string CurrentIdFileName = "current-print-template-pack-id";
        public const string ManifestFileName = "manifest.json";
        public const string PackRelativeDirectory = "docs/product/print-template-pack-v1";
        public const string AssetsRelativeDirectory = "Assets/print-template-pack";

        private PrintTemplatePack(PrintTemplatePackSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public PrintTemplatePackSnapshot Snapshot { get; }

        public string CurrentPackId => Snapshot.Id;

        public static PrintTemplatePack LoadFromDirectory(string packDirectory)
        {
            if (!Directory.Exists(packDirectory))
            {
                throw new InvalidOperationException(
                    $"Print template pack directory is missing: {packDirectory}"
                );
            }

            var currentIdPath = Path.Combine(packDirectory, CurrentIdFileName);
            if (!File.Exists(currentIdPath))
            {
                throw new InvalidOperationException(
                    $"Print template pack current id file is missing: {currentIdPath}"
                );
            }

            var currentId = File.ReadAllText(currentIdPath).Trim();
            if (string.IsNullOrWhiteSpace(currentId))
            {
                throw new InvalidOperationException(
                    "Print template pack current id file is empty."
                );
            }

            var manifestPath = Path.Combine(packDirectory, ManifestFileName);
            if (!File.Exists(manifestPath))
            {
                throw new InvalidOperationException(
                    $"Print template pack manifest is missing: {manifestPath}"
                );
            }

            using var stream = File.OpenRead(manifestPath);
            using var document = JsonDocument.Parse(stream);
            var snapshot = BindSnapshot(document.RootElement, packDirectory);
            if (!string.Equals(snapshot.Id, currentId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Print template pack current id '{currentId}' does not match pack id '{snapshot.Id}'."
                );
            }

            return new PrintTemplatePack(snapshot);
        }

        public static PrintTemplatePack LoadFromContentRoot(string contentRootPath)
        {
            return LoadFromDirectory(ResolvePackDirectory(contentRootPath));
        }

        public static PrintTemplatePack CreateForHost(IHostEnvironment environment)
        {
            return LoadFromContentRoot(environment.ContentRootPath);
        }

        public static string ResolvePackDirectory(string contentRootPath)
        {
            var candidates = new[]
            {
                Path.Combine(contentRootPath, AssetsRelativeDirectory),
                Path.GetFullPath(
                    Path.Combine(contentRootPath, "..", "..", PackRelativeDirectory)
                ),
                Path.GetFullPath(
                    Path.Combine(contentRootPath, "..", PackRelativeDirectory)
                ),
            };

            foreach (var candidate in candidates)
            {
                if (
                    Directory.Exists(candidate)
                    && File.Exists(Path.Combine(candidate, CurrentIdFileName))
                    && File.Exists(Path.Combine(candidate, ManifestFileName))
                )
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                "Print template pack directory could not be resolved from content root."
            );
        }

        public static (double WidthMm, double HeightMm) ReadSvgViewBoxMm(string svgPath)
        {
            var text = File.ReadAllText(svgPath);
            var match = Regex.Match(
                text,
                """viewBox\s*=\s*["']\s*([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)\s*["']""",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            if (!match.Success)
            {
                throw new InvalidOperationException(
                    $"SVG viewBox is missing in {svgPath}."
                );
            }

            var width = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            var height = double.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
            return (width, height);
        }

        private static PrintTemplatePackSnapshot BindSnapshot(
            JsonElement root,
            string packDirectory
        )
        {
            var pack = root.GetProperty("pack");
            var id = pack.GetProperty("id").GetString()
                ?? throw new InvalidOperationException("pack.id is missing.");
            var defaultOfferHeadline = pack.GetProperty("defaultOfferHeadline").GetString()
                ?? throw new InvalidOperationException("pack.defaultOfferHeadline is missing.");
            var offerCopyVersion = pack.GetProperty("offerCopyVersion").GetString()
                ?? throw new InvalidOperationException("pack.offerCopyVersion is missing.");

            var templates = root.GetProperty("templates");
            var tentName = templates.GetProperty("tentStickerEmptySvg").GetString()
                ?? throw new InvalidOperationException("templates.tentStickerEmptySvg is missing.");
            var cardName = templates.GetProperty("cardSvg").GetString()
                ?? throw new InvalidOperationException("templates.cardSvg is missing.");

            var tentPath = Path.Combine(packDirectory, tentName);
            var cardPath = Path.Combine(packDirectory, cardName);
            if (!File.Exists(tentPath))
            {
                throw new InvalidOperationException(
                    $"Tent/Sticker empty SVG is missing: {tentPath}"
                );
            }

            if (!File.Exists(cardPath))
            {
                throw new InvalidOperationException($"Card Dev SVG is missing: {cardPath}");
            }

            var slots = root.GetProperty("slots");
            return new PrintTemplatePackSnapshot
            {
                Id = id,
                DefaultOfferHeadline = defaultOfferHeadline,
                OfferCopyVersion = offerCopyVersion,
                TentStickerEmptySvgPath = tentPath,
                CardSvgPath = cardPath,
                TableTentQr = BindQr(slots.GetProperty("tableTent").GetProperty("qr")),
                WindowStickerQr = BindQr(slots.GetProperty("windowSticker").GetProperty("qr")),
                OfferCardHeadline = BindRect(
                    slots.GetProperty("offerCard").GetProperty("offerHeadline")
                ),
                OfferCardQr = BindQr(slots.GetProperty("offerCard").GetProperty("qr")),
            };
        }

        private static PrintSlotQrSpec BindQr(JsonElement element)
        {
            return new PrintSlotQrSpec
            {
                XMm = ReadNullableDouble(element, "xMm"),
                YMm = ReadNullableDouble(element, "yMm"),
                WidthMm = element.GetProperty("widthMm").GetDouble(),
                HeightMm = element.GetProperty("heightMm").GetDouble(),
                CentreWhenBlank = element.TryGetProperty("centreWhenBlank", out var centre)
                    && centre.ValueKind == JsonValueKind.True,
            };
        }

        private static PrintSlotRectSpec BindRect(JsonElement element)
        {
            return new PrintSlotRectSpec
            {
                XMm = element.GetProperty("xMm").GetDouble(),
                YMm = element.GetProperty("yMm").GetDouble(),
                WidthMm = element.GetProperty("widthMm").GetDouble(),
                HeightMm = element.GetProperty("heightMm").GetDouble(),
            };
        }

        private static double? ReadNullableDouble(JsonElement element, string name)
        {
            if (!element.TryGetProperty(name, out var value)
                || value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return value.GetDouble();
        }
    }
}
