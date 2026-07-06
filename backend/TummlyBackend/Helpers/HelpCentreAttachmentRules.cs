namespace TummlyBackend.Helpers
{
    public static class HelpCentreAttachmentRules
    {
        public const int MaxFilesPerQuery = 5;

        public const long MaxFileBytes = 10 * 1024 * 1024;

        public const long MaxTotalBytes = 50 * 1024 * 1024;

        public static readonly HashSet<string> AllowedContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/png",
                "image/webp",
                "image/gif",
                "application/pdf",
            };

        public static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp",
                ".gif",
                ".pdf",
            };
    }
}
