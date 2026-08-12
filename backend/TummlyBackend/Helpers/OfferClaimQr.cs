using QRCoder;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Builds guest-facing Offer claim QR images that encode the plain
    /// Offer Claim code (Staff Redeem scan path).
    /// </summary>
    public static class OfferClaimQr
    {
        private const int PixelsPerModule = 4;

        /// <summary>
        /// PNG data URI for an Offer claim QR encoding <paramref name="claimCode"/>.
        /// </summary>
        public static string ToPngDataUri(string claimCode)
        {
            var payload = (claimCode ?? string.Empty).Trim();
            if (payload.Length == 0)
            {
                throw new ArgumentException(
                    "Offer Claim code is required for Offer claim QR.",
                    nameof(claimCode)
                );
            }

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(
                payload,
                QRCodeGenerator.ECCLevel.Q
            );
            var png = new PngByteQRCode(data);
            var bytes = png.GetGraphic(PixelsPerModule);
            return "data:image/png;base64," + Convert.ToBase64String(bytes);
        }
    }
}
