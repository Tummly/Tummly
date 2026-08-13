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
        /// PNG bytes for an Offer claim QR encoding <paramref name="claimCode"/>.
        /// </summary>
        public static byte[] ToPngBytes(string claimCode)
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
            return png.GetGraphic(PixelsPerModule);
        }

        /// <summary>
        /// PNG data URI for an Offer claim QR encoding <paramref name="claimCode"/>.
        /// </summary>
        public static string ToPngDataUri(string claimCode) =>
            "data:image/png;base64," + Convert.ToBase64String(ToPngBytes(claimCode));
    }
}
