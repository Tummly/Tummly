using TummlyBackend.Helpers;

namespace TummlyBackend.Helpers.EmailTemplates
{
    public static class EmailAssets
    {
        private const string LogoRelativePath =
            "Assets/emails/logo.png";
        private const string TopDecorationRelativePath =
            "Assets/emails/top-decoration.png";

        private static byte[]? _logoBytes;
        private static byte[]? _topDecorationBytes;
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

        public static byte[] GetGuestResponseTopDecorationBytes(
            IWebHostEnvironment environment
        )
        {
            return _topDecorationBytes ??= ReadPngBytes(
                environment,
                TopDecorationRelativePath,
                "Guest response email top decoration asset was not found."
            );
        }

        /// <summary>
        /// Chrome (+ optional Offer claim QR) as CID attachments for Gmail.
        /// </summary>
        public static IReadOnlyList<EmailInlineImage> BuildNonTransactionalInlineImages(
            IWebHostEnvironment environment,
            GuestResponseEmailOfferBlock? offer
        )
        {
            var images = new List<EmailInlineImage>
            {
                new(
                    BaseNonTransactionalEmailTemplate.CidLogo,
                    "logo.png",
                    GetLogoBytes(environment)
                ),
                new(
                    BaseNonTransactionalEmailTemplate.CidTopDecoration,
                    "top-decoration.png",
                    GetGuestResponseTopDecorationBytes(environment)
                ),
            };

            var claimCode = offer?.RedemptionCode?.Trim();
            if (!string.IsNullOrWhiteSpace(claimCode))
            {
                images.Add(
                    new EmailInlineImage(
                        BaseNonTransactionalEmailTemplate.CidOfferQr,
                        "offer-qr.png",
                        OfferClaimQr.ToPngBytes(claimCode)
                    )
                );
            }

            return images;
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
