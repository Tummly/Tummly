using System.Text;

namespace TummlyBackend.PrintReadyQrMaterials
{
    public sealed record PrintReadyPlacementCsvRow(
        string JobId,
        string OrderId,
        string LocationId,
        string LocationName,
        string PlacementId,
        string PlacementType,
        string QrUrl,
        int PrtQty,
        string? OfferHeadline = null
    );

    /// <summary>
    /// Pure CSV builders for PatchCsv60 and GuestCardCsv print routes.
    /// </summary>
    public static class PrintReadyPlacementCsvComposer
    {
        public const string ContentType = "text/csv";

        private static readonly string[] PatchHeader =
        [
            "JobId",
            "OrderId",
            "LocationId",
            "LocationName",
            "PlacementId",
            "PlacementType",
            "QRURL",
            "PrtQty",
        ];

        private static readonly string[] GuestCardHeader =
        [
            "JobId",
            "OrderId",
            "LocationId",
            "LocationName",
            "PlacementId",
            "PlacementType",
            "QRURL",
            "PrtQty",
            "OfferHeadline",
        ];

        public static byte[] ComposePatchCsv60(PrintReadyPlacementCsvRow row)
        {
            return Encode(
                PatchHeader,
                [
                    row.JobId,
                    row.OrderId,
                    row.LocationId,
                    row.LocationName,
                    row.PlacementId,
                    row.PlacementType,
                    row.QrUrl,
                    row.PrtQty.ToString(),
                ]
            );
        }

        public static byte[] ComposeGuestCardCsv(PrintReadyPlacementCsvRow row)
        {
            return Encode(
                GuestCardHeader,
                [
                    row.JobId,
                    row.OrderId,
                    row.LocationId,
                    row.LocationName,
                    row.PlacementId,
                    row.PlacementType,
                    row.QrUrl,
                    row.PrtQty.ToString(),
                    row.OfferHeadline ?? string.Empty,
                ]
            );
        }

        private static byte[] Encode(string[] header, string[] fields)
        {
            var sb = new StringBuilder();
            AppendLine(sb, header);
            AppendLine(sb, fields);
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static void AppendLine(StringBuilder sb, string[] fields)
        {
            for (var i = 0; i < fields.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append(Escape(fields[i]));
            }

            sb.Append("\r\n");
        }

        private static string Escape(string value)
        {
            if (
                value.Contains(',')
                || value.Contains('"')
                || value.Contains('\r')
                || value.Contains('\n')
            )
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }
}
