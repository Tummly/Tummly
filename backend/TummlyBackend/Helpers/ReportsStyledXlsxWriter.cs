using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Branded multi-sheet XLSX writer for Reports exports — chrome matches the
    /// Tummly Reports template (green brand, dark header, zebra rows, freeze+filter).
    /// </summary>
    public static class ReportsStyledXlsxWriter
    {
        public const string ContentType = OpenXmlSpreadsheet.ContentType;

        public const string DefaultDisclaimer =
            "Server-authoritative export data for the selected period.";

        public const string RestrictedDisclaimer =
            "Restricted export — contains consent/contact linkage. Export only for authorised roles.";

        private const int BrandEyebrowStyle = 11;
        private const int TitleStyle = 12;
        private const int MetaLabelStyle = 3;
        private const int MetaValueStyle = 4;
        private const int PoweredByStyle = 13;
        private const int DisclaimerStyle = 14;
        private const int DisclaimerContinueStyle = 15;
        private const int DisclaimerTailStyle = 16;
        private const int HeaderStyle = 17;
        private const int ZebraKeyStyle = 18;
        private const int ZebraValueStyle = 19;
        private const int ZebraWrapStyle = 20;
        private const int PlainKeyStyle = 21;
        private const int PlainValueStyle = 22;
        private const int PlainWrapStyle = 23;

        public sealed record SheetMeta(
            string RestaurantName,
            string? LocationLabel,
            string ReportPeriodLabel,
            DateTime ExportedUtc,
            string FileTypeLabel = "XLSX report export",
            string? ScopeLabel = null
        );

        public sealed record DataSheet(
            string SheetName,
            string ReportTitle,
            SheetMeta Meta,
            IReadOnlyList<string> Headers,
            IReadOnlyList<IReadOnlyList<string>> Rows,
            IReadOnlyList<double> ColumnWidths,
            string Disclaimer = DefaultDisclaimer,
            IReadOnlySet<int>? WrapColumnIndexes = null
        );

        public static byte[] Write(IReadOnlyList<DataSheet> sheets)
        {
            if (sheets.Count == 0)
            {
                throw new ArgumentException(
                    "At least one sheet is required.",
                    nameof(sheets)
                );
            }

            using var stream = new MemoryStream();
            using (
                var archive = new ZipArchive(
                    stream,
                    ZipArchiveMode.Create,
                    leaveOpen: true
                )
            )
            {
                WriteEntry(archive, "[Content_Types].xml", ContentTypesXml(sheets.Count));
                WriteEntry(archive, "_rels/.rels", RelsXml());
                WriteEntry(archive, "xl/workbook.xml", WorkbookXml(sheets));
                WriteEntry(
                    archive,
                    "xl/_rels/workbook.xml.rels",
                    WorkbookRelsXml(sheets.Count)
                );
                WriteEntry(archive, "xl/styles.xml", StylesXml());

                for (var i = 0; i < sheets.Count; i++)
                {
                    WriteEntry(
                        archive,
                        $"xl/worksheets/sheet{i + 1}.xml",
                        SheetXml(sheets[i])
                    );
                }
            }

            return stream.ToArray();
        }

        public static string SanitizeSheetName(string name)
        {
            var cleaned = name
                .Replace('\\', ' ')
                .Replace('/', ' ')
                .Replace('?', ' ')
                .Replace('*', ' ')
                .Replace('[', ' ')
                .Replace(']', ' ')
                .Replace(':', ' ')
                .Trim();
            if (cleaned.Length == 0)
            {
                cleaned = "Sheet";
            }

            return cleaned.Length <= 31 ? cleaned : cleaned[..31];
        }

        public static string FormatPeriodLabel(DateTime fromUtc, DateTime toUtc)
        {
            var endInclusive = toUtc.AddDays(-1);
            if (endInclusive < fromUtc)
            {
                endInclusive = fromUtc;
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:dd MMM yyyy} - {1:dd MMM yyyy}",
                fromUtc,
                endInclusive
            );
        }

        public static string FormatExportedLabel(DateTime utcNow)
            => utcNow.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

        public static string FormatChange(int current, int previous)
        {
            var delta = current - previous;
            if (delta > 0)
            {
                return "+" + delta.ToString(CultureInfo.InvariantCulture);
            }

            return delta.ToString(CultureInfo.InvariantCulture);
        }

        public static IReadOnlyList<string> DeduplicateSheetNames(
            IReadOnlyList<string> names
        )
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>(names.Count);
            foreach (var raw in names)
            {
                var baseName = SanitizeSheetName(raw);
                var candidate = baseName;
                var suffix = 2;
                while (!used.Add(candidate))
                {
                    var trim = Math.Max(1, 31 - ($" ({suffix})").Length);
                    candidate = SanitizeSheetName(
                        baseName.Length <= trim
                            ? $"{baseName} ({suffix})"
                            : $"{baseName[..trim]} ({suffix})"
                    );
                    suffix++;
                }

                result.Add(candidate);
            }

            return result;
        }

        private static void WriteEntry(ZipArchive archive, string path, string xml)
        {
            var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
            using var writer = new StreamWriter(
                entry.Open(),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            );
            writer.Write(xml);
        }

        private static string ContentTypesXml(int sheetCount)
        {
            var builder = new StringBuilder();
            builder.Append(
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
                """
            );
            for (var i = 1; i <= sheetCount; i++)
            {
                builder.Append(
                    $"  <Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>\n"
                );
            }

            builder.Append("</Types>");
            return builder.ToString();
        }

        private static string RelsXml() =>
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
            </Relationships>
            """;

        private static string WorkbookXml(IReadOnlyList<DataSheet> sheets)
        {
            var builder = new StringBuilder();
            builder.Append(
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets>
                """
            );
            for (var i = 0; i < sheets.Count; i++)
            {
                builder.Append("    <sheet name=\"");
                builder.Append(XmlEscape(sheets[i].SheetName));
                builder.Append("\" sheetId=\"");
                builder.Append(i + 1);
                builder.Append("\" r:id=\"rId");
                builder.Append(i + 1);
                builder.Append("\"/>\n");
            }

            builder.Append(
                """
                  </sheets>
                </workbook>
                """
            );
            return builder.ToString();
        }

        private static string WorkbookRelsXml(int sheetCount)
        {
            var builder = new StringBuilder();
            builder.Append(
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                """
            );
            for (var i = 1; i <= sheetCount; i++)
            {
                builder.Append(
                    $"  <Relationship Id=\"rId{i}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i}.xml\"/>\n"
                );
            }

            builder.Append(
                $"  <Relationship Id=\"rId{sheetCount + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>\n"
            );
            builder.Append("</Relationships>");
            return builder.ToString();
        }

        private static string SheetXml(DataSheet sheet)
        {
            var colCount = Math.Max(sheet.Headers.Count, 1);
            var lastCol = ColumnName(colCount - 1);
            var lastDataRow = 7 + Math.Max(sheet.Rows.Count, 0);
            if (sheet.Rows.Count == 0)
            {
                lastDataRow = 7;
            }

            var autoFilterEnd = $"{lastCol}{Math.Max(lastDataRow, 7)}";
            var dimensionEnd = $"{lastCol}{Math.Max(lastDataRow, 7)}";

            var builder = new StringBuilder();
            builder.Append(
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <sheetPr><outlinePr summaryBelow="1" summaryRight="1"/><pageSetUpPr fitToPage="1"/></sheetPr>
                """
            );
            builder.Append($"  <dimension ref=\"A1:{dimensionEnd}\"/>\n");
            builder.Append(
                """
                  <sheetViews>
                    <sheetView showGridLines="0" workbookViewId="0">
                      <pane ySplit="7" topLeftCell="A8" activePane="bottomLeft" state="frozen"/>
                      <selection pane="bottomLeft" activeCell="A1" sqref="A1"/>
                    </sheetView>
                  </sheetViews>
                  <sheetFormatPr baseColWidth="8" defaultRowHeight="15"/>
                """
            );

            builder.Append("  <cols>\n");
            for (var i = 0; i < colCount; i++)
            {
                var width =
                    i < sheet.ColumnWidths.Count ? sheet.ColumnWidths[i] : 16d;
                builder.Append(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "    <col width=\"{0}\" customWidth=\"1\" min=\"{1}\" max=\"{1}\"/>\n",
                        width,
                        i + 1
                    )
                );
            }

            builder.Append("  </cols>\n  <sheetData>\n");

            AppendInline(builder, 1, "A", BrandEyebrowStyle, "TUMMLY REPORTS");
            builder.Append("    <row r=\"2\" ht=\"30\" customHeight=\"1\">");
            AppendCell(builder, "A2", TitleStyle, sheet.ReportTitle);
            builder.Append("</row>\n");

            // Meta row 4
            builder.Append("    <row r=\"4\">");
            AppendCell(builder, "A4", MetaLabelStyle, "Restaurant");
            AppendCell(builder, "B4", MetaValueStyle, sheet.Meta.RestaurantName);
            if (!string.IsNullOrWhiteSpace(sheet.Meta.ScopeLabel))
            {
                AppendCell(builder, "D4", MetaLabelStyle, "Scope");
                AppendCell(builder, "E4", MetaValueStyle, sheet.Meta.ScopeLabel!);
            }
            else
            {
                AppendCell(builder, "D4", MetaLabelStyle, "Location");
                AppendCell(
                    builder,
                    "E4",
                    MetaValueStyle,
                    sheet.Meta.LocationLabel ?? string.Empty
                );
            }

            AppendCell(builder, "H4", PoweredByStyle, "Powered by Tummly");
            builder.Append("</row>\n");

            // Meta row 5
            builder.Append("    <row r=\"5\">");
            AppendCell(builder, "A5", MetaLabelStyle, "Report period");
            AppendCell(builder, "B5", MetaValueStyle, sheet.Meta.ReportPeriodLabel);
            AppendCell(builder, "D5", MetaLabelStyle, "File type");
            AppendCell(builder, "E5", MetaValueStyle, sheet.Meta.FileTypeLabel);
            AppendCell(builder, "G5", MetaLabelStyle, "Exported");
            AppendCell(
                builder,
                "H5",
                MetaValueStyle,
                FormatExportedLabel(sheet.Meta.ExportedUtc)
            );
            builder.Append("</row>\n");

            // Disclaimer row 6 with green underline span
            builder.Append("    <row r=\"6\">");
            AppendCell(builder, "A6", DisclaimerStyle, sheet.Disclaimer);
            for (var c = 1; c < Math.Min(colCount, 6); c++)
            {
                AppendEmpty(builder, ColumnName(c) + "6", DisclaimerContinueStyle);
            }

            for (var c = 6; c < Math.Max(colCount, 8); c++)
            {
                AppendEmpty(builder, ColumnName(c) + "6", DisclaimerTailStyle);
            }

            builder.Append("</row>\n");

            // Header row 7
            builder.Append("    <row r=\"7\" ht=\"24\" customHeight=\"1\">");
            for (var i = 0; i < sheet.Headers.Count; i++)
            {
                AppendCell(
                    builder,
                    ColumnName(i) + "7",
                    HeaderStyle,
                    sheet.Headers[i]
                );
            }

            builder.Append("</row>\n");

            // Data rows
            var wrap = sheet.WrapColumnIndexes;
            for (var r = 0; r < sheet.Rows.Count; r++)
            {
                var row = sheet.Rows[r];
                var excelRow = r + 8;
                var zebra = r % 2 == 0;
                builder.Append("    <row r=\"");
                builder.Append(excelRow);
                builder.Append("\">");
                for (var c = 0; c < sheet.Headers.Count; c++)
                {
                    var value = c < row.Count ? row[c] ?? string.Empty : string.Empty;
                    var isWrap = wrap != null && wrap.Contains(c);
                    int style;
                    if (c == 0)
                    {
                        style = zebra ? ZebraKeyStyle : PlainKeyStyle;
                    }
                    else if (isWrap)
                    {
                        style = zebra ? ZebraWrapStyle : PlainWrapStyle;
                    }
                    else
                    {
                        style = zebra ? ZebraValueStyle : PlainValueStyle;
                    }

                    AppendCell(
                        builder,
                        ColumnName(c) + excelRow,
                        style,
                        value
                    );
                }

                builder.Append("</row>\n");
            }

            builder.Append("  </sheetData>\n");
            builder.Append($"  <autoFilter ref=\"A7:{autoFilterEnd}\"/>\n");
            builder.Append("</worksheet>");
            return builder.ToString();
        }

        private static void AppendInline(
            StringBuilder builder,
            int row,
            string col,
            int style,
            string value
        )
        {
            builder.Append("    <row r=\"");
            builder.Append(row);
            builder.Append("\">");
            AppendCell(builder, col + row, style, value);
            builder.Append("</row>\n");
        }

        private static void AppendCell(
            StringBuilder builder,
            string cellRef,
            int style,
            string value
        )
        {
            builder.Append("<c r=\"");
            builder.Append(cellRef);
            builder.Append("\" s=\"");
            builder.Append(style);
            builder.Append("\" t=\"inlineStr\"><is><t");
            if (value.Length > 0 && (value[0] == ' ' || value[^1] == ' ' || value.Contains('\n', StringComparison.Ordinal)))
            {
                builder.Append(" xml:space=\"preserve\"");
            }

            builder.Append('>');
            builder.Append(XmlEscape(value));
            builder.Append("</t></is></c>");
        }

        private static void AppendEmpty(
            StringBuilder builder,
            string cellRef,
            int style
        )
        {
            builder.Append("<c r=\"");
            builder.Append(cellRef);
            builder.Append("\" s=\"");
            builder.Append(style);
            builder.Append("\"/>");
        }

        private static string ColumnName(int zeroBasedIndex)
        {
            var n = zeroBasedIndex + 1;
            var name = string.Empty;
            while (n > 0)
            {
                n--;
                name = (char)('A' + (n % 26)) + name;
                n /= 26;
            }

            return name;
        }

        private static string XmlEscape(string value)
        {
            return value
                .Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal)
                .Replace("\"", "&quot;", StringComparison.Ordinal)
                .Replace("'", "&apos;", StringComparison.Ordinal);
        }

        private static string StylesXml() =>
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <numFmts count="3">
                <numFmt numFmtId="164" formatCode="0.0%"/>
                <numFmt numFmtId="165" formatCode="yyyy-mm-dd"/>
                <numFmt numFmtId="166" formatCode="dd mmm yyyy"/>
              </numFmts>
              <fonts count="15">
                <font><name val="Calibri"/><family val="2"/><color theme="1"/><sz val="11"/><scheme val="minor"/></font>
                <font><b val="1"/><color rgb="0014A946"/><sz val="11"/></font>
                <font><b val="1"/><color rgb="00171717"/><sz val="22"/></font>
                <font><b val="1"/><color rgb="006B7280"/><sz val="9"/></font>
                <font><color rgb="00252525"/><sz val="9"/></font>
                <font><b val="1"/><color rgb="00171717"/><sz val="12"/></font>
                <font><color rgb="006B7280"/><sz val="9"/></font>
                <font><b val="1"/><color rgb="0014A946"/><sz val="10"/></font>
                <font><b val="1"/><color rgb="00171717"/><sz val="20"/></font>
                <font><i val="1"/><color rgb="006B7280"/><sz val="9"/></font>
                <font><i val="1"/><color rgb="006B7280"/><sz val="8"/></font>
                <font><b val="1"/><color rgb="00FFFFFF"/><sz val="9"/></font>
                <font><b val="1"/><color rgb="00252525"/><sz val="9"/></font>
                <font><b val="1"/><color rgb="00171717"/><sz val="9"/></font>
                <font><b val="1"/><color rgb="0014A946"/><sz val="9"/><u val="single"/></font>
              </fonts>
              <fills count="4">
                <fill><patternFill/></fill>
                <fill><patternFill patternType="gray125"/></fill>
                <fill><patternFill patternType="solid"><fgColor rgb="00171717"/></patternFill></fill>
                <fill><patternFill patternType="solid"><fgColor rgb="00FAFAFA"/></patternFill></fill>
              </fills>
              <borders count="5">
                <border><left/><right/><top/><bottom/><diagonal/></border>
                <border><bottom style="thin"><color rgb="0014A946"/></bottom></border>
                <border><left/><right/><top/><bottom style="thin"><color rgb="0014A946"/></bottom><diagonal/></border>
                <border><bottom style="hair"><color rgb="00E5E7EB"/></bottom></border>
                <border><bottom style="hair"><color rgb="00D9D9D9"/></bottom></border>
              </borders>
              <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
              <cellXfs count="34">
                <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
                <xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0"/>
                <xf numFmtId="0" fontId="2" fillId="0" borderId="0" xfId="0"/>
                <xf numFmtId="0" fontId="3" fillId="0" borderId="0" xfId="0"/>
                <xf numFmtId="0" fontId="4" fillId="0" borderId="0" xfId="0"/>
                <xf numFmtId="0" fontId="5" fillId="0" borderId="0" xfId="0"/>
                <xf numFmtId="0" fontId="6" fillId="0" borderId="0" xfId="0"/>
                <xf numFmtId="0" fontId="13" fillId="0" borderId="4" xfId="0"/>
                <xf numFmtId="0" fontId="6" fillId="0" borderId="4" xfId="0"/>
                <xf numFmtId="0" fontId="0" fillId="0" borderId="4" xfId="0"/>
                <xf numFmtId="0" fontId="14" fillId="0" borderId="4" xfId="0"/>
                <xf numFmtId="0" fontId="7" fillId="0" borderId="0" xfId="0"/>
                <xf numFmtId="0" fontId="8" fillId="0" borderId="0" xfId="0"/>
                <xf numFmtId="0" fontId="9" fillId="0" borderId="0" applyAlignment="1" xfId="0"><alignment horizontal="right"/></xf>
                <xf numFmtId="0" fontId="10" fillId="0" borderId="1" xfId="0"/>
                <xf numFmtId="0" fontId="0" fillId="0" borderId="2" xfId="0"/>
                <xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0"/>
                <xf numFmtId="0" fontId="11" fillId="2" borderId="0" applyAlignment="1" xfId="0"><alignment horizontal="left" vertical="center"/></xf>
                <xf numFmtId="0" fontId="12" fillId="3" borderId="3" applyAlignment="1" xfId="0"><alignment vertical="top"/></xf>
                <xf numFmtId="0" fontId="4" fillId="3" borderId="3" applyAlignment="1" xfId="0"><alignment vertical="top"/></xf>
                <xf numFmtId="0" fontId="4" fillId="3" borderId="3" applyAlignment="1" xfId="0"><alignment vertical="top" wrapText="1"/></xf>
                <xf numFmtId="0" fontId="12" fillId="0" borderId="3" applyAlignment="1" xfId="0"><alignment vertical="top"/></xf>
                <xf numFmtId="0" fontId="4" fillId="0" borderId="3" applyAlignment="1" xfId="0"><alignment vertical="top"/></xf>
                <xf numFmtId="0" fontId="4" fillId="0" borderId="3" applyAlignment="1" xfId="0"><alignment vertical="top" wrapText="1"/></xf>
                <xf numFmtId="0" fontId="12" fillId="3" borderId="3" applyAlignment="1" xfId="0"><alignment vertical="top"/></xf>
                <xf numFmtId="0" fontId="4" fillId="3" borderId="3" applyAlignment="1" xfId="0"><alignment vertical="top"/></xf>
                <xf numFmtId="164" fontId="4" fillId="3" borderId="3" applyAlignment="1" xfId="0"><alignment vertical="top"/></xf>
                <xf numFmtId="0" fontId="12" fillId="0" borderId="3" applyAlignment="1" xfId="0"><alignment vertical="top"/></xf>
                <xf numFmtId="0" fontId="4" fillId="0" borderId="3" applyAlignment="1" xfId="0"><alignment vertical="top"/></xf>
                <xf numFmtId="164" fontId="4" fillId="0" borderId="3" applyAlignment="1" xfId="0"><alignment vertical="top"/></xf>
                <xf numFmtId="166" fontId="4" fillId="3" borderId="3" applyAlignment="1" xfId="0"><alignment vertical="top"/></xf>
                <xf numFmtId="166" fontId="4" fillId="0" borderId="3" applyAlignment="1" xfId="0"><alignment vertical="top"/></xf>
                <xf numFmtId="0" fontId="12" fillId="3" borderId="3" applyAlignment="1" xfId="0"><alignment vertical="top" wrapText="1"/></xf>
                <xf numFmtId="0" fontId="12" fillId="0" borderId="3" applyAlignment="1" xfId="0"><alignment vertical="top" wrapText="1"/></xf>
              </cellXfs>
              <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
            </styleSheet>
            """;
    }
}
