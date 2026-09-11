namespace TummlyBackend.PrintReadyQrMaterials
{
    /// <summary>
    /// Controllable seam for black-on-white QR raster pixels (RGB, no alpha).
    /// </summary>
    public interface IQrCodeRasterizer
    {
        QrRasterImage Render(string payload, int modulePixels = 8);
    }

    public sealed record QrRasterImage(
        int Width,
        int Height,
        byte[] RgbBytes
    );
}
