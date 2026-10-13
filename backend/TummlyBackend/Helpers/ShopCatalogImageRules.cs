namespace TummlyBackend.Helpers
{
    public static class ShopCatalogImageRules
    {
        public const long MaxFileBytes = 5 * 1024 * 1024;

        public const int MaxImagesPerSku = 8;

        public const string ObjectPrefix = "shop-materials/";

        public const string LegacyKeyPrefix = "legacy:";

        public static readonly HashSet<string> AllowedContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/png",
                "image/webp",
            };

        public static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp",
            };

        public static string? ResolveContentType(
            string? contentType,
            string fileName
        )
        {
            var trimmed = contentType?.Trim();
            if (
                !string.IsNullOrWhiteSpace(trimmed)
                && AllowedContentTypes.Contains(trimmed)
            )
            {
                return trimmed;
            }

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return null;
            }

            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => null,
            };
        }

        public static string? ValidateFile(IFormFile? file)
        {
            if (file == null || file.Length <= 0)
            {
                return "An image file is required.";
            }

            if (file.Length > MaxFileBytes)
            {
                return "Shop catalog image must be 5 MB or smaller.";
            }

            var extension = Path.GetExtension(file.FileName);
            if (
                string.IsNullOrWhiteSpace(extension)
                || !AllowedExtensions.Contains(extension)
            )
            {
                return "Shop catalog image must be a JPEG, PNG, or WebP file.";
            }

            if (ResolveContentType(file.ContentType, file.FileName) == null)
            {
                return "Shop catalog image must be a JPEG, PNG, or WebP file.";
            }

            return null;
        }

        public static string BuildStorageKey(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".bin";
            }

            extension = extension.ToLowerInvariant();
            return $"{ObjectPrefix}{Guid.NewGuid():N}{extension}";
        }

        public static string BuildLegacyStorageKey(string publicPath) =>
            $"{LegacyKeyPrefix}{publicPath}";

        public static bool TryGetLegacyPublicPath(
            string storageKey,
            out string publicPath
        )
        {
            publicPath = string.Empty;
            if (
                !storageKey.StartsWith(
                    LegacyKeyPrefix,
                    StringComparison.Ordinal
                )
            )
            {
                return false;
            }

            publicPath = storageKey[LegacyKeyPrefix.Length..];
            return !string.IsNullOrWhiteSpace(publicPath);
        }

        public static string BuildPublicUrl(string storageKey)
        {
            if (TryGetLegacyPublicPath(storageKey, out var legacyPath))
            {
                return legacyPath;
            }

            var opaque = storageKey;
            if (opaque.StartsWith(ObjectPrefix, StringComparison.Ordinal))
            {
                opaque = opaque[ObjectPrefix.Length..];
            }

            return $"/api/public/shop-catalog-images/{opaque}";
        }

        public static string? TryParseObjectKeyFromPublicSegment(string segment)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                return null;
            }

            var trimmed = segment.Trim().TrimStart('/');
            if (
                trimmed.Contains("..", StringComparison.Ordinal)
                || trimmed.Contains('/', StringComparison.Ordinal)
                || trimmed.Contains('\\', StringComparison.Ordinal)
            )
            {
                return null;
            }

            return $"{ObjectPrefix}{trimmed}";
        }
    }
}
