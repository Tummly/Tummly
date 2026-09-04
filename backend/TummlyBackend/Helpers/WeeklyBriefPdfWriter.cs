using System.Globalization;
using System.Text;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Coarse sync Weekly brief PDF — section parity with the Figma ready page,
    /// not visual polish (ticket 09).
    /// </summary>
    public static class WeeklyBriefPdfWriter
    {
        public const string ContentType = "application/pdf";

        private const float PageHeight = 842f;
        private const float MarginLeft = 40f;
        private const float LineHeight = 14f;
        private const float SectionGap = 18f;

        public sealed record WhatChangedRow(
            string Area,
            string Change,
            string Meaning
        );

        public sealed record FeedbackSummary(
            string Text,
            string Subtitle
        );

        public sealed record SuggestedCampaign(
            string Name,
            string? AudienceKey
        );

        public sealed record Document(
            string LocationName,
            string Period,
            IReadOnlyList<string> DataSources,
            string Confidence,
            string GeneratedAtLabel,
            string ExecutiveSummary,
            IReadOnlyList<WhatChangedRow> WhatChanged,
            FeedbackSummary? FeedbackSummary,
            IReadOnlyList<string> RecommendedActionLines,
            SuggestedCampaign? SuggestedCampaign
        );

        public static (byte[] Content, string FileName) Render(
            Document document,
            int locationId,
            DateTime utcNow
        )
        {
            var stamp = utcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var fileName = $"tummly-weekly-brief-{locationId}-{stamp}Z.pdf";
            return (BuildPdf(BuildContentStream(document)), fileName);
        }

        private static string BuildContentStream(Document document)
        {
            var sb = new StringBuilder(4096);
            var y = PageHeight - 48f;

            SetFillBlack(sb);
            TextAt(sb, "F2", 18, MarginLeft, y, "Weekly Brief");
            y -= 22f;
            SetFillMuted(sb);
            TextAt(
                sb,
                "F1",
                10,
                MarginLeft,
                y,
                "A plain-English summary of what happened, what changed and what to do next."
            );
            y -= SectionGap;

            SetFillBlack(sb);
            TextAt(sb, "F2", 12, MarginLeft, y, "Meta");
            y -= LineHeight;
            SetFillMuted(sb);
            y = WriteWrapped(
                sb,
                $"Period: {document.Period}",
                y,
                maxChars: 90
            );
            y = WriteWrapped(
                sb,
                $"Location: {document.LocationName}",
                y,
                maxChars: 90
            );
            var sources =
                document.DataSources.Count == 0
                    ? "None"
                    : string.Join(", ", document.DataSources);
            y = WriteWrapped(sb, $"Data sources: {sources}", y, maxChars: 90);
            y = WriteWrapped(
                sb,
                $"Confidence: {document.Confidence}",
                y,
                maxChars: 90
            );
            y = WriteWrapped(
                sb,
                $"Generated: {document.GeneratedAtLabel}",
                y,
                maxChars: 90
            );
            y -= SectionGap - 6f;

            SetFillBlack(sb);
            TextAt(sb, "F2", 12, MarginLeft, y, "Executive summary");
            y -= LineHeight;
            SetFillMuted(sb);
            y = WriteWrapped(sb, document.ExecutiveSummary, y, maxChars: 90);
            y -= SectionGap - 6f;

            if (document.WhatChanged.Count > 0)
            {
                SetFillBlack(sb);
                TextAt(sb, "F2", 12, MarginLeft, y, "What changed");
                y -= LineHeight;
                foreach (var row in document.WhatChanged)
                {
                    SetFillMuted(sb);
                    y = WriteWrapped(
                        sb,
                        $"{row.Area} | {row.Change} | {row.Meaning}",
                        y,
                        maxChars: 90
                    );
                }

                y -= SectionGap - 6f;
            }

            if (document.FeedbackSummary is FeedbackSummary feedback)
            {
                SetFillBlack(sb);
                TextAt(sb, "F2", 12, MarginLeft, y, "Feedback summary");
                y -= LineHeight;
                SetFillMuted(sb);
                y = WriteWrapped(sb, feedback.Text, y, maxChars: 90);
                y = WriteWrapped(sb, feedback.Subtitle, y, maxChars: 90);
                y -= SectionGap - 6f;
            }

            if (document.RecommendedActionLines.Count > 0)
            {
                SetFillBlack(sb);
                TextAt(sb, "F2", 12, MarginLeft, y, "Recommended actions");
                y -= LineHeight;
                var index = 1;
                foreach (var line in document.RecommendedActionLines)
                {
                    SetFillMuted(sb);
                    y = WriteWrapped(
                        sb,
                        $"{index}. {line}",
                        y,
                        maxChars: 90
                    );
                    index++;
                }

                y -= SectionGap - 6f;
            }

            if (document.SuggestedCampaign is SuggestedCampaign campaign)
            {
                SetFillBlack(sb);
                TextAt(sb, "F2", 12, MarginLeft, y, "Suggested campaign");
                y -= LineHeight;
                SetFillMuted(sb);
                y = WriteWrapped(sb, $"Draft: {campaign.Name}", y, maxChars: 90);
                if (!string.IsNullOrWhiteSpace(campaign.AudienceKey))
                {
                    y = WriteWrapped(
                        sb,
                        $"Audience: {campaign.AudienceKey}",
                        y,
                        maxChars: 90
                    );
                }
            }

            _ = y;
            return sb.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
        }

        private static float WriteWrapped(
            StringBuilder sb,
            string text,
            float y,
            int maxChars
        )
        {
            foreach (var line in WrapText(text, maxChars))
            {
                if (y < 48f)
                {
                    break;
                }

                TextAt(sb, "F1", 10, MarginLeft, y, line);
                y -= LineHeight;
            }

            return y;
        }

        private static IReadOnlyList<string> WrapText(string text, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return [];
            }

            var words = text.Split(
                [' ', '\t', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries
            );
            var lines = new List<string>();
            var current = new StringBuilder();
            foreach (var word in words)
            {
                if (current.Length == 0)
                {
                    current.Append(word);
                    continue;
                }

                if (current.Length + 1 + word.Length <= maxChars)
                {
                    current.Append(' ');
                    current.Append(word);
                }
                else
                {
                    lines.Add(current.ToString());
                    current.Clear();
                    current.Append(word);
                }
            }

            if (current.Length > 0)
            {
                lines.Add(current.ToString());
            }

            return lines;
        }

        private static void SetFillBlack(StringBuilder sb)
        {
            sb.Append("0 0 0 rg\n");
        }

        private static void SetFillMuted(StringBuilder sb)
        {
            sb.Append("0.4 0.4 0.4 rg\n");
        }

        private static void TextAt(
            StringBuilder sb,
            string font,
            float size,
            float x,
            float y,
            string text
        )
        {
            sb.Append("BT /");
            sb.Append(font);
            sb.Append(' ');
            sb.Append(size.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(" Tf ");
            sb.Append(x.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(y.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(" Td (");
            sb.Append(EscapePdfText(SanitizeAscii(text)));
            sb.Append(") Tj ET\n");
        }

        private static string SanitizeAscii(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (ch >= 32 && ch <= 126)
                {
                    sb.Append(ch);
                }
                else if (ch == '…')
                {
                    sb.Append("...");
                }
                else if (char.IsWhiteSpace(ch))
                {
                    sb.Append(' ');
                }
                else
                {
                    sb.Append('?');
                }
            }

            return sb.ToString();
        }

        private static string EscapePdfText(string value)
        {
            return value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);
        }

        private static byte[] BuildPdf(string contentStream)
        {
            var contentBytes = Encoding.ASCII.GetBytes(contentStream);
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
                        $"4 0 obj<< /Length {contentBytes.Length} >>stream\n"
                    ),
                    contentBytes,
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
                xref.Append(offsets[i].ToString("D10", CultureInfo.InvariantCulture));
                xref.Append(" 00000 n \n");
            }

            xref.Append($"trailer<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
            xref.Append("startxref\n");
            xref.Append(xrefPos);
            xref.Append("\n%%EOF\n");
            output.Write(Encoding.ASCII.GetBytes(xref.ToString()));
            return output.ToArray();
        }

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
