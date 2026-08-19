namespace TummlyBackend.Helpers.EmailTemplates
{
    public static class EmailAssets
    {
        private const string LogoRelativePath =
            "Assets/emails/logo.png";

        private static byte[]? _logoBytes;
        private static string? _logoDataUri;

        public static byte[] GetLogoBytes(IWebHostEnvironment environment)
        {
            return _logoBytes ??= ReadPngBytes(
                environment,
                LogoRelativePath,
                "Email logo asset was not found."
            );
        }

        public static string GetLogoDataUri(IWebHostEnvironment environment)
        {
            return _logoDataUri
                ??= ToPngDataUri(GetLogoBytes(environment));
        }

        private static string ToPngDataUri(byte[] pngBytes) =>
            $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";

        private static byte[] ReadPngBytes(
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

            return File.ReadAllBytes(path);
        }
    }
}
