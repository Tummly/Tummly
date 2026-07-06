namespace TummlyBackend.Helpers
{
    public static class HelpCentreAttachmentValidator
    {
        public static string? ValidateFiles(IReadOnlyList<IFormFile>? files)
        {
            if (files == null || files.Count == 0)
            {
                return null;
            }

            if (files.Count > HelpCentreAttachmentRules.MaxFilesPerQuery)
            {
                return
                    $"You can attach up to {HelpCentreAttachmentRules.MaxFilesPerQuery} files.";
            }

            long totalBytes = 0;

            foreach (var file in files)
            {
                if (file.Length <= 0)
                {
                    return "One or more attachments are empty.";
                }

                if (file.Length > HelpCentreAttachmentRules.MaxFileBytes)
                {
                    return "Each attachment must be 10 MB or smaller.";
                }

                totalBytes += file.Length;

                var extension = Path.GetExtension(file.FileName);

                if (
                    string.IsNullOrWhiteSpace(extension)
                    || !HelpCentreAttachmentRules.AllowedExtensions.Contains(extension)
                )
                {
                    return
                        "Attachments must be JPEG, PNG, WebP, GIF, or PDF files.";
                }

                if (ResolveContentType(file.ContentType, file.FileName) == null)
                {
                    return
                        "Attachments must be JPEG, PNG, WebP, GIF, or PDF files.";
                }
            }

            if (totalBytes > HelpCentreAttachmentRules.MaxTotalBytes)
            {
                return "Total attachment size must be 50 MB or smaller.";
            }

            return null;
        }

        public static string? ResolveContentType(
            string? contentType,
            string fileName
        )
        {
            var trimmed = contentType?.Trim();

            if (
                !string.IsNullOrWhiteSpace(trimmed)
                && HelpCentreAttachmentRules.AllowedContentTypes.Contains(trimmed)
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
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                _ => null,
            };
        }

        public static string BuildStorageKey(int queryId, string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);

            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".bin";
            }

            extension = extension.ToLowerInvariant();
            var safeName = Path.GetFileNameWithoutExtension(originalFileName);
            safeName = string.Concat(
                safeName
                    .Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.')
                    .Take(80)
            );

            if (string.IsNullOrWhiteSpace(safeName))
            {
                safeName = "attachment";
            }

            return
                $"help-centre-queries/{queryId}/{Guid.NewGuid():N}-{safeName}{extension}";
        }
    }
}
