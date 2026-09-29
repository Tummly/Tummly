namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Public HTTPS chrome images for React Email
    /// (<c>{PublicApi|Frontend}/email/…</c>). Prefer <c>PublicApi:BaseUrl</c>
    /// so assets ship with the API and do not depend on a frontend deploy.
    /// </summary>
    public static class EmailAssets
    {
        /// <summary>
        /// Dark wordmark on light backgrounds
        /// (<c>/email/tummly-logo-dark.png</c>).
        /// </summary>
        public const string PublicDarkLogoPath = "/email/tummly-logo-dark.png";

        public const string PublicPoweredByLogoPath = "/email/logo.png";

        public const string PublicTopDecorationPath = "/email/top-decoration.png";

        public const string PublicBottomStripPath = "/email/bottom-strip.png";

        public const string PublicBrandLogoPlaceholderPath =
            "/email/brand-logo-placeholder.png";

        public const string PublicWordmarkPath = "/email/tummly-wordmark.png";

        /// <summary>
        /// Filenames served at <c>GET /email/{file}</c> from
        /// <c>Assets/emails/</c> (allow-list; templates stay private).
        /// </summary>
        public static readonly IReadOnlyList<string> PublicFileNames =
        [
            "tummly-logo-dark.png",
            "logo.png",
            "tummly-wordmark.png",
            "top-decoration.png",
            "bottom-strip.png",
            "brand-logo-placeholder.png",
        ];

        public const string DarkLogoContentId = "tummly-logo-dark";

        public const string PoweredByLogoContentId = "tummly-powered-by";

        public const string TopDecorationContentId = "tummly-top-decoration";

        public const string BottomStripContentId = "tummly-bottom-strip";

        public const string BrandLogoPlaceholderContentId =
            "tummly-brand-logo-placeholder";

        /// <summary>
        /// Absolute origin for email chrome images.
        /// Prefers <c>PublicApi:BaseUrl</c>, else <c>Frontend:BaseUrl</c>.
        /// </summary>
        public static string ResolveChromeBaseUrl(IConfiguration configuration)
        {
            var publicApi = configuration["PublicApi:BaseUrl"]
                ?.Trim()
                .TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(publicApi))
            {
                return publicApi;
            }

            var frontend = configuration["Frontend:BaseUrl"]
                ?.Trim()
                .TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(frontend))
            {
                return frontend;
            }

            throw new InvalidOperationException(
                "PublicApi:BaseUrl or Frontend:BaseUrl is required for email chrome images."
            );
        }

        /// <summary>
        /// Absolute URL for the dark wordmark (light-background emails).
        /// </summary>
        public static string GetDarkLogoPublicUrl(string chromeBaseUrl)
        {
            var baseUrl = chromeBaseUrl.Trim().TrimEnd('/');
            return $"{baseUrl}{PublicDarkLogoPath}";
        }

        public static string GetDarkLogoPublicUrl(IConfiguration configuration) =>
            GetDarkLogoPublicUrl(ResolveChromeBaseUrl(configuration));

        public static bool IsLoopbackChromeBase(string chromeBaseUrl)
        {
            if (!Uri.TryCreate(chromeBaseUrl, UriKind.Absolute, out var uri))
            {
                return false;
            }

            return uri.IsLoopback;
        }

        public static bool IsAllowedPublicFile(string fileName)
        {
            foreach (var name in PublicFileNames)
            {
                if (string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static string? TryContentRootFilePath(
            IWebHostEnvironment environment,
            string fileName
        )
        {
            if (!IsAllowedPublicFile(fileName))
            {
                return null;
            }

            var path = Path.Combine(
                environment.ContentRootPath,
                "Assets",
                "emails",
                fileName
            );
            return File.Exists(path) ? path : null;
        }

        public static byte[] ReadPublicPngBytes(
            IWebHostEnvironment environment,
            string fileName
        )
        {
            var path = TryContentRootFilePath(environment, fileName);
            if (path == null)
            {
                throw new FileNotFoundException(
                    $"Email chrome image '{fileName}' was not found under Assets/emails/."
                );
            }

            return File.ReadAllBytes(path);
        }
    }
}
