using System.Text;
using TummlyBackend.PrintReadyQrMaterials;

namespace TummlyBackend.Tests.PrintReadyQrMaterials
{
    public class PrintReadyPlacementCsvComposerTests
    {
        private static PrintReadyPlacementCsvRow SampleRow(
            string? offerHeadline = null
        ) =>
            new(
                JobId: "job-1",
                OrderId: "11111111-1111-1111-1111-111111111111",
                LocationId: "42",
                LocationName: "Front Room",
                PlacementId: "99",
                PlacementType: "TableTent",
                QrUrl: "https://guest.example/q/abc",
                PrtQty: 25,
                OfferHeadline: offerHeadline
            );

        [Fact]
        public void ContentType_IsTextCsv()
        {
            Assert.Equal("text/csv", PrintReadyPlacementCsvComposer.ContentType);
        }

        [Fact]
        public void ComposePatchCsv60_WritesHeaderAndOneDataRow()
        {
            var bytes = PrintReadyPlacementCsvComposer.ComposePatchCsv60(
                SampleRow()
            );
            var text = Encoding.UTF8.GetString(bytes);

            Assert.Equal(
                "JobId,OrderId,LocationId,LocationName,PlacementId,PlacementType,QRURL,PrtQty\r\n"
                    + "job-1,11111111-1111-1111-1111-111111111111,42,Front Room,99,TableTent,https://guest.example/q/abc,25\r\n",
                text
            );
        }

        [Fact]
        public void ComposeGuestCardCsv_AddsOfferHeadlineColumn()
        {
            var bytes = PrintReadyPlacementCsvComposer.ComposeGuestCardCsv(
                SampleRow(offerHeadline: "Free Dessert")
            );
            var text = Encoding.UTF8.GetString(bytes);

            Assert.Equal(
                "JobId,OrderId,LocationId,LocationName,PlacementId,PlacementType,QRURL,PrtQty,OfferHeadline\r\n"
                    + "job-1,11111111-1111-1111-1111-111111111111,42,Front Room,99,TableTent,https://guest.example/q/abc,25,Free Dessert\r\n",
                text
            );
        }

        [Fact]
        public void ComposePatchCsv60_EscapesCommasQuotesAndNewlines()
        {
            var row = SampleRow() with
            {
                LocationName = "Front, \"Main\" Room\nNorth",
            };
            var bytes = PrintReadyPlacementCsvComposer.ComposePatchCsv60(row);
            var text = Encoding.UTF8.GetString(bytes);
            var dataLine = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)[1];

            Assert.Contains(
                "\"Front, \"\"Main\"\" Room\nNorth\"",
                dataLine
            );
        }
    }
}
