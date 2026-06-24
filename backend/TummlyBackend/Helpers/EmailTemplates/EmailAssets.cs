namespace TummlyBackend.Helpers.EmailTemplates
{
    public static class EmailAssets
    {
        private const string LogoRelativePath =
            "Assets/emails/logo.png";

        private static string? _logoDataUri;

        public static string GetLogoDataUri(IWebHostEnvironment environment)
        {
            if (_logoDataUri is not null)
            {
                return _logoDataUri;
            }

            var logoPath = Path.Combine(
                environment.ContentRootPath,
                LogoRelativePath.Replace('/', Path.DirectorySeparatorChar)
            );

            if (!File.Exists(logoPath))
            {
                throw new FileNotFoundException(
                    "Email logo asset was not found.",
                    logoPath
                );
            }

            var pngBytes = File.ReadAllBytes(logoPath);
            var encoded = Convert.ToBase64String(pngBytes);

            _logoDataUri = $"data:image/png;base64,{encoded}";
            return _logoDataUri;
        }
    }
}
