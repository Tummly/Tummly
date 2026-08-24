namespace TummlyBackend.Helpers
{
    public static class BrandLogoRules
    {
        public const long MaxFileBytes = 2 * 1024 * 1024;

        public const string PublicObjectPrefix = "brand-logos/";

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
                return null;
            }

            if (file.Length > MaxFileBytes)
            {
                return "Brand logo must be 2 MB or smaller.";
            }

            var extension = Path.GetExtension(file.FileName);

            if (
                string.IsNullOrWhiteSpace(extension)
                || !AllowedExtensions.Contains(extension)
            )
            {
                return "Brand logo must be a JPEG, PNG, or WebP file.";
            }

            if (ResolveContentType(file.ContentType, file.FileName) == null)
            {
                return "Brand logo must be a JPEG, PNG, or WebP file.";
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
            return $"{PublicObjectPrefix}{Guid.NewGuid():N}{extension}";
        }

        public static string BuildPublicUrl(string objectKey)
        {
            var opaque = objectKey;
            if (opaque.StartsWith(PublicObjectPrefix, StringComparison.Ordinal))
            {
                opaque = opaque[PublicObjectPrefix.Length..];
            }

            return $"/api/public/brand-logos/{opaque}";
        }

        public static string? BuildAbsolutePublicUrl(
            string? objectKey,
            string? publicApiBaseUrl
        )
        {
            if (
                string.IsNullOrWhiteSpace(objectKey)
                || string.IsNullOrWhiteSpace(publicApiBaseUrl)
            )
            {
                return null;
            }

            return
                $"{publicApiBaseUrl.Trim().TrimEnd('/')}{BuildPublicUrl(objectKey)}";
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

            return $"{PublicObjectPrefix}{trimmed}";
        }

        public const string OperatorBrandLogoUrl =
            "/api/account-workspace/brand-logo";
    }
}
