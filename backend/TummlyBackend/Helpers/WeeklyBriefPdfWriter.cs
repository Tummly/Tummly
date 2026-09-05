using System.Globalization;
using System.Text;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Coarse sync Weekly brief PDF — section parity with the Figma ready page,
    /// not visual polish (ticket 09). Multi-page so later sections are not dropped.
    /// </summary>
    public static class WeeklyBriefPdfWriter
    {
        public const string ContentType = "application/pdf";

        private const float PageHeight = 842f;
        private const float MarginLeft = 40f;
        private const float MarginBottom = 48f;
        private const float LineHeight = 14f;
        private const float SectionGap = 18f;
        private const float TopY = PageHeight - 48f;

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
            return (BuildPdf(BuildPages(document)), fileName);
        }

        private static IReadOnlyList<string> BuildPages(Document document)
        {
            var pages = new List<StringBuilder>();
            var sb = NewPage(pages);
            var y = TopY;

            (sb, y) = EnsureSpace(pages, sb, y, LineHeight * 2 + SectionGap);
            SetFillBlack(sb);
            TextAt(sb, "F2", 18, MarginLeft, y, "Weekly Brief");
            y -= 22f;
            SetFillMuted(sb);
            (sb, y) = WriteWrapped(
                pages,
                sb,
                y,
                "A plain-English summary of what happened, what changed and what to do next.",
                maxChars: 90
            );
            y -= SectionGap - LineHeight;

            (sb, y) = EnsureSpace(pages, sb, y, LineHeight * 6);
            SetFillBlack(sb);
            TextAt(sb, "F2", 12, MarginLeft, y, "Meta");
            y -= LineHeight;
            SetFillMuted(sb);
            (sb, y) = WriteWrapped(pages, sb, y, $"Period: {document.Period}", 90);
            (sb, y) = WriteWrapped(
                pages,
                sb,
                y,
                $"Location: {document.LocationName}",
                90
            );
            var sources =
                document.DataSources.Count == 0
                    ? "None"
                    : string.Join(", ", document.DataSources);
            (sb, y) = WriteWrapped(pages, sb, y, $"Data sources: {sources}", 90);
            (sb, y) = WriteWrapped(
                pages,
                sb,
                y,
                $"Confidence: {document.Confidence}",
                90
            );
            (sb, y) = WriteWrapped(
                pages,
                sb,
                y,
                $"Generated: {document.GeneratedAtLabel}",
                90
            );
            y -= SectionGap - 6f;

            (sb, y) = EnsureSpace(pages, sb, y, LineHeight * 3);
            SetFillBlack(sb);
            TextAt(sb, "F2", 12, MarginLeft, y, "Executive summary");
            y -= LineHeight;
            SetFillMuted(sb);
            (sb, y) = WriteWrapped(pages, sb, y, document.ExecutiveSummary, 90);
            y -= SectionGap - 6f;

            if (document.WhatChanged.Count > 0)
            {
                (sb, y) = EnsureSpace(pages, sb, y, LineHeight * 2);
                SetFillBlack(sb);
                TextAt(sb, "F2", 12, MarginLeft, y, "What changed");
                y -= LineHeight;
                foreach (var row in document.WhatChanged)
                {
                    SetFillMuted(sb);
                    (sb, y) = WriteWrapped(
                        pages,
                        sb,
                        y,
                        $"{row.Area} | {row.Change} | {row.Meaning}",
                        90
                    );
                }

                y -= SectionGap - 6f;
            }

            if (document.FeedbackSummary is FeedbackSummary feedback)
            {
                (sb, y) = EnsureSpace(pages, sb, y, LineHeight * 3);
                SetFillBlack(sb);
                TextAt(sb, "F2", 12, MarginLeft, y, "Feedback summary");
                y -= LineHeight;
                SetFillMuted(sb);
                (sb, y) = WriteWrapped(pages, sb, y, feedback.Text, 90);
                (sb, y) = WriteWrapped(pages, sb, y, feedback.Subtitle, 90);
                y -= SectionGap - 6f;
            }

            if (document.RecommendedActionLines.Count > 0)
            {
                (sb, y) = EnsureSpace(pages, sb, y, LineHeight * 2);
                SetFillBlack(sb);
                TextAt(sb, "F2", 12, MarginLeft, y, "Recommended actions");
                y -= LineHeight;
                var index = 1;
                foreach (var line in document.RecommendedActionLines)
                {
                    SetFillMuted(sb);
                    (sb, y) = WriteWrapped(
                        pages,
                        sb,
                        y,
                        $"{index}. {line}",
                        90
                    );
                    index++;
                }

                y -= SectionGap - 6f;
            }

            if (document.SuggestedCampaign is SuggestedCampaign campaign)
            {
                (sb, y) = EnsureSpace(pages, sb, y, LineHeight * 3);
                SetFillBlack(sb);
                TextAt(sb, "F2", 12, MarginLeft, y, "Suggested campaign");
                y -= LineHeight;
                SetFillMuted(sb);
                (sb, y) = WriteWrapped(pages, sb, y, $"Draft: {campaign.Name}", 90);
                if (!string.IsNullOrWhiteSpace(campaign.AudienceKey))
                {
                    (sb, y) = WriteWrapped(
                        pages,
                        sb,
                        y,
                        $"Audience: {campaign.AudienceKey}",
                        90
                    );
                }
            }

            _ = y;
            return pages
                .Select(page =>
                    page.ToString().Replace("\r\n", "\n", StringComparison.Ordinal)
                )
                .ToList();
        }

        private static StringBuilder NewPage(List<StringBuilder> pages)
        {
            var sb = new StringBuilder(2048);
            pages.Add(sb);
            return sb;
        }

        private static (StringBuilder Sb, float Y) EnsureSpace(
            List<StringBuilder> pages,
            StringBuilder sb,
            float y,
            float needed
        )
        {
            if (y - needed >= MarginBottom)
            {
                return (sb, y);
            }

            return (NewPage(pages), TopY);
        }

        private static (StringBuilder Sb, float Y) WriteWrapped(
            List<StringBuilder> pages,
            StringBuilder sb,
            float y,
            string text,
            int maxChars
        )
        {
            foreach (var line in WrapText(text, maxChars))
            {
                (sb, y) = EnsureSpace(pages, sb, y, LineHeight);
                TextAt(sb, "F1", 10, MarginLeft, y, line);
                y -= LineHeight;
            }

            return (sb, y);
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

        private static byte[] BuildPdf(IReadOnlyList<string> pageContents)
        {
            if (pageContents.Count == 0)
            {
                pageContents = [""];
            }

            var contentBytesList = pageContents
                .Select(content => Encoding.ASCII.GetBytes(content))
                .ToList();

            // Object layout:
            // 1 Catalog, 2 Pages, 3..N page dicts, then content streams, then F1/F2 fonts.
            var pageCount = contentBytesList.Count;
            var firstPageObj = 3;
            var firstContentObj = firstPageObj + pageCount;
            var font1Obj = firstContentObj + pageCount;
            var font2Obj = font1Obj + 1;

            var pageKids = string.Join(
                " ",
                Enumerable.Range(0, pageCount).Select(i => $"{firstPageObj + i} 0 R")
            );

            var objects = new List<byte[]>
            {
                Encoding.ASCII.GetBytes(
                    "1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj\n"
                ),
                Encoding.ASCII.GetBytes(
                    $"2 0 obj<< /Type /Pages /Kids [{pageKids}] /Count {pageCount} >>endobj\n"
                ),
            };

            for (var i = 0; i < pageCount; i++)
            {
                var contentObj = firstContentObj + i;
                objects.Add(
                    Encoding.ASCII.GetBytes(
                        $"{firstPageObj + i} 0 obj<< /Type /Page /Parent 2 0 R "
                            + "/MediaBox [0 0 595 842] "
                            + $"/Contents {contentObj} 0 R "
                            + $"/Resources<< /Font<< /F1 {font1Obj} 0 R /F2 {font2Obj} 0 R >> >> >>endobj\n"
                    )
                );
            }

            for (var i = 0; i < pageCount; i++)
            {
                var bytes = contentBytesList[i];
                objects.Add(
                    Concat(
                        Encoding.ASCII.GetBytes(
                            $"{firstContentObj + i} 0 obj<< /Length {bytes.Length} >>stream\n"
                        ),
                        bytes,
                        Encoding.ASCII.GetBytes("\nendstream\nendobj\n")
                    )
                );
            }

            objects.Add(
                Encoding.ASCII.GetBytes(
                    $"{font1Obj} 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>endobj\n"
                )
            );
            objects.Add(
                Encoding.ASCII.GetBytes(
                    $"{font2Obj} 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>endobj\n"
                )
            );

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
