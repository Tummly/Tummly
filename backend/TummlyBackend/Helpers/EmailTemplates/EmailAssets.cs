namespace TummlyBackend.Helpers.EmailTemplates
{
    public static class EmailAssets
    {
        private const string LogoRelativePath =
            "Assets/emails/logo.png";
        private const string TopDecorationRelativePath =
            "Assets/emails/top-decoration.png";
        private const string BottomStripRelativePath =
            "Assets/emails/bottom-strip.png";

        private static string? _logoDataUri;
        private static string? _topDecorationDataUri;
        private static string? _bottomStripDataUri;

        public static string GetLogoDataUri(IWebHostEnvironment environment)
        {
            return _logoDataUri ??= ReadPngDataUri(
                environment,
                LogoRelativePath,
                "Email logo asset was not found."
            );
        }

        public static string GetGuestResponseTopDecorationDataUri(
            IWebHostEnvironment environment
        )
        {
            return _topDecorationDataUri ??= ReadPngDataUri(
                environment,
                TopDecorationRelativePath,
                "Guest response email top decoration asset was not found."
            );
        }

        public static string GetGuestResponseBottomStripDataUri(
            IWebHostEnvironment environment
        )
        {
            return _bottomStripDataUri ??= ReadPngDataUri(
                environment,
                BottomStripRelativePath,
                "Guest response email green paper footer asset was not found."
            );
        }

        private static string ReadPngDataUri(
            IWebHostEnvironment environment,
            string relativePath,
            string missingMessage
        )
        {
            var path = Path.Combine(
                environment.ContentRootPath,
                relativePath.Replace('/', Path.DirectorySeparatorChar)
            );

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(missingMessage, path);
            }

            var pngBytes = File.ReadAllBytes(path);
            var encoded = Convert.ToBase64String(pngBytes);
            return $"data:image/png;base64,{encoded}";
        }
    }
}
