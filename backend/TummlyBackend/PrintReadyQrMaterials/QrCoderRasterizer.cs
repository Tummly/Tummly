using QRCoder;

namespace TummlyBackend.PrintReadyQrMaterials
{
    public sealed class QrCoderRasterizer : IQrCodeRasterizer
    {
        public QrRasterImage Render(string payload, int modulePixels = 8)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            var modules = data.ModuleMatrix;
            var moduleCount = modules.Count;
            var size = moduleCount * modulePixels;
            var rgb = new byte[size * size * 3];

            for (var y = 0; y < moduleCount; y++)
            {
                for (var x = 0; x < moduleCount; x++)
                {
                    var dark = modules[y][x];
                    var tone = dark ? (byte)0 : (byte)255;
                    for (var py = 0; py < modulePixels; py++)
                    {
                        for (var px = 0; px < modulePixels; px++)
                        {
                            var row = y * modulePixels + py;
                            var col = x * modulePixels + px;
                            var offset = (row * size + col) * 3;
                            rgb[offset] = tone;
                            rgb[offset + 1] = tone;
                            rgb[offset + 2] = tone;
                        }
                    }
                }
            }

            return new QrRasterImage(size, size, rgb);
        }
    }
}
