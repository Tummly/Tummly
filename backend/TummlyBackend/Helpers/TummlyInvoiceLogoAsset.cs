using System.IO;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Tummly wordmark for invoice PDFs (JPEG from email logo, dark-bg safe).
    /// PDF cannot embed SVG; this is the committed raster of the brand mark.
    /// </summary>
    public static class TummlyInvoiceLogoAsset
    {
        public const int PixelWidth = 200;
        public const int PixelHeight = 50;

        private static readonly Lazy<byte[]> JpegBytes = new(LoadJpeg);

        public static byte[] GetJpegBytes() => JpegBytes.Value;

        private static byte[] LoadJpeg()
        {
            var assembly = typeof(TummlyInvoiceLogoAsset).Assembly;
            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(name =>
                    name.EndsWith(
                        "tummly-logo.jpg",
                        StringComparison.OrdinalIgnoreCase
                    )
                );
            if (resourceName == null)
            {
                throw new InvalidOperationException(
                    "Embedded invoice logo resource tummly-logo.jpg was not found."
                );
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException(
                    $"Could not open embedded resource '{resourceName}'."
                );
            }

            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }
}
