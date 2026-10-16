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
    }
}
