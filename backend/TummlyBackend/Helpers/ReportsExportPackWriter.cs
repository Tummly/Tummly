using System.Globalization;
using System.Text;
using TummlyBackend.DTOs.Reports;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Coarse Reports export pack files (lock 09 / ticket 17) — hub PDF strips
    /// and Capture / Feedback / Campaign CSV tables. Not visual polish.
    /// </summary>
    public static class ReportsExportPackWriter
    {
        public const string PdfContentType = "application/pdf";
        public const string CsvContentType = "text/csv";

        /// <summary>Matches <see cref="ReportsStyledXlsxPack"/> Capture sheet columns.</summary>
        private static readonly string[] CaptureHeaders =
        [
            "Source ID",
            "QR name",
            "Placement",
            "Location",
            "Status",
            "Scans",
            "Form opens",
            "Feedback",
            "Contactable",
            "Claims",
            "Conversion",
        ];

        /// <summary>Matches <see cref="ReportsStyledXlsxPack"/> Campaigns sheet columns.</summary>
        private static readonly string[] CampaignHeaders =
        [
            "Campaign ID",
            "Campaign name",
            "Goal",
            "Channel",
            "Sent",
            "Delivered",
            "Claims",
            "Redemptions",
            "Unsubs",
            "Failed",
            "Status",
        ];

        public static (byte[] Content, string FileName) RenderCaptureCsv(
            ReportsCaptureDto dto,
            int locationId,
            string locationName,
            DateTime utcNow
        )
        {
            var stamp = Stamp(utcNow);
            var fileName =
                $"tummly-reports-capture-{locationId}-{stamp}Z.csv";
            var rows = new List<string[]>();
            foreach (var row in dto.Placements ?? [])
            {
                var conversion =
                    row.ConversionPercent is double pct
                        ? pct.ToString("0.0", CultureInfo.InvariantCulture)
                            + "%"
                        : string.Empty;
                rows.Add(
                    [
                        row.QrCodeId.ToString(CultureInfo.InvariantCulture),
                        row.Name,
                        row.Name,
                        locationName,
                        row.Status,
                        row.Scans.ToString(CultureInfo.InvariantCulture),
                        row.FormOpens?.ToString(CultureInfo.InvariantCulture)
                            ?? string.Empty,
                        row.Feedback.ToString(CultureInfo.InvariantCulture),
                        row.Contactable.ToString(CultureInfo.InvariantCulture),
                        row.Claims.ToString(CultureInfo.InvariantCulture),
                        conversion,
                    ]
                );
            }

            return (Rfc4180Csv.WriteUtf8(CaptureHeaders, rows), fileName);
        }

        public static (byte[] Content, string FileName) RenderFeedbackRowsCsv(
            IReadOnlyList<ReportsExportFeedbackRowDto> feedbackRows,
            int locationId,
            DateTime utcNow
        )
        {
            var stamp = Stamp(utcNow);
            var fileName =
                $"tummly-reports-feedback-{locationId}-{stamp}Z.csv";
            var rows = feedbackRows
                .Select(ReportsExportFeedbackRows.ToCsvRow)
                .ToList();
            return (
                Rfc4180Csv.WriteUtf8(ReportsExportFeedbackRows.Headers, rows),
                fileName
            );
        }

        public static (byte[] Content, string FileName) RenderCampaignsCsv(
            ReportsCampaignsDto dto,
            int locationId,
            DateTime utcNow
        )
        {
            var stamp = Stamp(utcNow);
            var fileName =
                $"tummly-reports-campaigns-{locationId}-{stamp}Z.csv";
            var rows = new List<string[]>();
            foreach (var row in dto.Performance ?? [])
            {
                rows.Add(
                    [
                        row.CampaignId.ToString(CultureInfo.InvariantCulture),
                        row.Name,
                        row.Goal ?? string.Empty,
                        row.Channel ?? string.Empty,
                        row.Sent.ToString(CultureInfo.InvariantCulture),
                        row.Delivered.ToString(CultureInfo.InvariantCulture),
                        row.Claims.ToString(CultureInfo.InvariantCulture),
                        row.Redemptions.ToString(CultureInfo.InvariantCulture),
                        row.Unsubscribes.ToString(CultureInfo.InvariantCulture),
                        row.Failed.ToString(CultureInfo.InvariantCulture),
                        row.Status,
                    ]
                );
            }

            return (Rfc4180Csv.WriteUtf8(CampaignHeaders, rows), fileName);
        }

        private static string Stamp(DateTime utcNow)
            => utcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        private static IReadOnlyList<string> BuildOverviewLines(
            ReportsOverviewDto dto,
            string locationName,
            DateTime fromUtc,
            DateTime toUtc
        )
        {
            var lines = new List<string>
            {
                $"Location: {locationName}",
                $"Window: {fromUtc:yyyy-MM-dd} to {toUtc:yyyy-MM-dd} (half-open UTC)",
            };

            if (dto.LifetimeEmpty || dto.Funnel == null)
            {
                lines.Add("No activity in this window (empty overview).");
                return lines;
            }

            lines.Add("Funnel");
            lines.Add(
                $"  QR scans: {dto.Funnel.QrScans.Value} (prev {dto.Funnel.QrScans.ValuePrevious})"
            );
            lines.Add(
                $"  Feedback received: {dto.Funnel.FeedbackReceived.Value} (prev {dto.Funnel.FeedbackReceived.ValuePrevious})"
            );
            lines.Add(
                $"  Marketing opt-ins: {dto.Funnel.MarketingOptIns.Value} (prev {dto.Funnel.MarketingOptIns.ValuePrevious})"
            );
            lines.Add(
                $"  Offer redemptions: {dto.Funnel.OfferRedemptions.Value} (prev {dto.Funnel.OfferRedemptions.ValuePrevious})"
            );
            lines.Add(
                $"  Campaigns sent: {dto.Funnel.CampaignsSent.Value} (prev {dto.Funnel.CampaignsSent.ValuePrevious})"
            );

            if (dto.PrivateFeedback != null)
            {
                lines.Add("Private feedback");
                lines.Add(
                    $"  Messages: {dto.PrivateFeedback.FeedbackMessages.Value} (prev {dto.PrivateFeedback.FeedbackMessages.ValuePrevious})"
                );
                lines.Add(
                    $"  Marketing opt-ins: {dto.PrivateFeedback.MarketingOptIns.Value} (prev {dto.PrivateFeedback.MarketingOptIns.ValuePrevious})"
                );
                lines.Add(
                    $"  Follow-up needed: {dto.PrivateFeedback.FollowUpNeeded.Value} (prev {dto.PrivateFeedback.FollowUpNeeded.ValuePrevious})"
                );
                lines.Add(
                    $"  Followed up: {dto.PrivateFeedback.FollowedUp.Value} (prev {dto.PrivateFeedback.FollowedUp.ValuePrevious})"
                );
            }

            if (dto.OffersAndCampaigns != null)
            {
                lines.Add("Offers and campaigns");
                lines.Add(
                    $"  Active offers: {dto.OffersAndCampaigns.ActiveOffers.Value} (prev {dto.OffersAndCampaigns.ActiveOffers.ValuePrevious})"
                );
                lines.Add(
                    $"  Offer claims: {dto.OffersAndCampaigns.OfferClaims.Value} (prev {dto.OffersAndCampaigns.OfferClaims.ValuePrevious})"
                );
                lines.Add(
                    $"  Offer redemptions: {dto.OffersAndCampaigns.OfferRedemptions.Value} (prev {dto.OffersAndCampaigns.OfferRedemptions.ValuePrevious})"
                );
                lines.Add(
                    $"  Campaigns sent: {dto.OffersAndCampaigns.CampaignsSent.Value} (prev {dto.OffersAndCampaigns.CampaignsSent.ValuePrevious})"
                );
                lines.Add(
                    $"  Unsubscribes: {dto.OffersAndCampaigns.Unsubscribes.Value} (prev {dto.OffersAndCampaigns.Unsubscribes.ValuePrevious})"
                );
            }

            lines.Add("Top capture sources");
            var sources = dto.TopCaptureSources ?? [];
            if (sources.Count == 0)
            {
                lines.Add("  (none)");
            }
            else
            {
                foreach (var source in sources)
                {
                    lines.Add(
                        $"  {source.Source}: scans {source.Scans}, feedback {source.Feedback}, opt-ins {source.MarketingOptIns}"
                    );
                }
            }

            return lines;
        }

        private static byte[] BuildSimplePdf(
            string title,
            IReadOnlyList<string> lines
        )
        {
            var sb = new StringBuilder();
            sb.Append("BT\n");
            sb.Append("/F2 16 Tf\n");
            sb.Append("0 0 0 rg\n");
            sb.Append("40 780 Td\n");
            sb.Append($"({EscapePdf(title)}) Tj\n");
            sb.Append("/F1 10 Tf\n");
            sb.Append("0 -20 Td\n");
            for (var i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append("0 -14 Td\n");
                }

                sb.Append($"({EscapePdf(lines[i])}) Tj\n");
            }

            sb.Append("ET\n");
            var content = Encoding.ASCII.GetBytes(sb.ToString());

            var objects = new List<byte[]>
            {
                Encoding.ASCII.GetBytes(
                    "1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj\n"
                ),
                Encoding.ASCII.GetBytes(
                    "2 0 obj<< /Type /Pages /Kids [3 0 R] /Count 1 >>endobj\n"
                ),
                Encoding.ASCII.GetBytes(
                    "3 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] "
                        + "/Contents 4 0 R /Resources<< /Font<< /F1 5 0 R /F2 6 0 R >> >> >>endobj\n"
                ),
                Concat(
                    Encoding.ASCII.GetBytes(
                        $"4 0 obj<< /Length {content.Length} >>stream\n"
                    ),
                    content,
                    Encoding.ASCII.GetBytes("\nendstream\nendobj\n")
                ),
                Encoding.ASCII.GetBytes(
                    "5 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>endobj\n"
                ),
                Encoding.ASCII.GetBytes(
                    "6 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>endobj\n"
                ),
            };

            using var output = new MemoryStream();
            output.Write(Encoding.ASCII.GetBytes("%PDF-1.4\n"));
            var offsets = new List<int> { 0 };
            foreach (var obj in objects)
            {
                offsets.Add((int)output.Position);
                output.Write(obj);
            }

            var xrefPos = (int)output.Position;
            var xref = new StringBuilder();
            xref.Append($"xref\n0 {objects.Count + 1}\n");
            xref.Append("0000000000 65535 f \n");
            for (var i = 1; i < offsets.Count; i++)
            {
                xref.Append(
                    offsets[i].ToString("D10", CultureInfo.InvariantCulture)
                );
                xref.Append(" 00000 n \n");
            }

            xref.Append($"trailer<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
            xref.Append("startxref\n");
            xref.Append(xrefPos);
            xref.Append("\n%%EOF\n");
            output.Write(Encoding.ASCII.GetBytes(xref.ToString()));
            return output.ToArray();
        }

        private static string EscapePdf(string value)
            => value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);

        private static byte[] Concat(params byte[][] parts)
        {
            var length = parts.Sum(part => part.Length);
            var buffer = new byte[length];
            var offset = 0;
            foreach (var part in parts)
            {
                Buffer.BlockCopy(part, 0, buffer, offset, part.Length);
                offset += part.Length;
            }

            return buffer;
        }
    }
}
