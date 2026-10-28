using System.Text;
using System.Text.RegularExpressions;
using TummlyBackend.Models;
using TummlyBackend.PrintReadyQrMaterials;

namespace TummlyBackend.Tests.PrintReadyQrMaterials
{
    public class PrintReadyQrPdfComposerCounterTests
    {
        [Fact]
        public void Compose_CounterCard_ReturnsPdfBytes()
        {
            var qr = new QrRasterImage(
                Width: 21,
                Height: 21,
                RgbBytes: Enumerable.Repeat((byte)255, 21 * 21 * 3).ToArray()
            );

            var pdf = PrintReadyQrPdfComposer.Compose(
                pack: null!,
                QrType.CounterCard,
                qr,
                offerHeadline: null
            );

            Assert.True(pdf.Length > 4);
            Assert.Equal((byte)'%', pdf[0]);
            Assert.Equal((byte)'P', pdf[1]);
            Assert.Equal((byte)'D', pdf[2]);
            Assert.Equal((byte)'F', pdf[3]);
        }

        [Fact]
        public void Compose_CounterCard_EmbedsQrAsImageAtQrPixelSize()
        {
            const int qrSize = 21;
            var rgb = new byte[qrSize * qrSize * 3];
            for (var row = 0; row < qrSize; row++)
            {
                for (var column = 0; column < qrSize; column++)
                {
                    var value = (row + column) % 2 == 0 ? (byte)0 : (byte)255;
                    var offset = (row * qrSize + column) * 3;
                    rgb[offset] = value;
                    rgb[offset + 1] = value;
                    rgb[offset + 2] = value;
                }
            }
            var qr = new QrRasterImage(
                Width: qrSize,
                Height: qrSize,
                RgbBytes: rgb
            );

            var pdf = PrintReadyQrPdfComposer.Compose(
                pack: null!,
                QrType.CounterCard,
                qr,
                offerHeadline: null
            );

            var text = Encoding.Latin1.GetString(pdf);
            var imageObjects = Regex.Matches(text, @"/Subtype\s*/Image\b");
            Assert.NotEmpty(imageObjects);
            Assert.Contains(
                imageObjects.Cast<Match>(),
                match =>
                {
                    var dictionary = text.Substring(
                        match.Index,
                        Math.Min(400, text.Length - match.Index)
                    );
                    return Regex.IsMatch(dictionary, $@"/Width\s+{qrSize}\b")
                        && Regex.IsMatch(dictionary, $@"/Height\s+{qrSize}\b");
                }
            );
        }
    }
}
