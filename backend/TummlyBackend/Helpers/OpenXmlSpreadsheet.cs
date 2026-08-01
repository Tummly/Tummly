using System.IO.Compression;
using System.Text;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Minimal XLSX (Office Open XML) writer — one sheet, inline strings,
    /// no external spreadsheet package.
    /// </summary>
    public static class OpenXmlSpreadsheet
    {
        public const string ContentType =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public static byte[] Write(
            IReadOnlyList<string> headers,
            IEnumerable<IReadOnlyList<string>> rows
        )
        {
            using var stream = new MemoryStream();
            using (
                var archive = new ZipArchive(
                    stream,
                    ZipArchiveMode.Create,
                    leaveOpen: true
                )
            )
            {
                WriteEntry(
                    archive,
                    "[Content_Types].xml",
                    ContentTypesXml()
                );
                WriteEntry(archive, "_rels/.rels", RelsXml());
                WriteEntry(
                    archive,
                    "xl/workbook.xml",
                    WorkbookXml()
                );
                WriteEntry(
                    archive,
                    "xl/_rels/workbook.xml.rels",
                    WorkbookRelsXml()
                );
                WriteEntry(
                    archive,
                    "xl/worksheets/sheet1.xml",
                    SheetXml(headers, rows)
                );
            }

            return stream.ToArray();
        }

        private static void WriteEntry(
            ZipArchive archive,
            string path,
            string xml
        )
        {
            var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
            using var writer = new StreamWriter(
                entry.Open(),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            );
            writer.Write(xml);
        }

        private static string ContentTypesXml() =>
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
              <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
              <Default Extension="xml" ContentType="application/xml"/>
              <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
              <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
            </Types>
            """;

        private static string RelsXml() =>
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
            </Relationships>
            """;

        private static string WorkbookXml() =>
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
              <sheets>
                <sheet name="Feedback" sheetId="1" r:id="rId1"/>
              </sheets>
            </workbook>
            """;

        private static string WorkbookRelsXml() =>
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
            </Relationships>
            """;

        private static string SheetXml(
            IReadOnlyList<string> headers,
            IEnumerable<IReadOnlyList<string>> rows
        )
        {
            var builder = new StringBuilder();
            builder.Append(
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <sheetData>
                """
            );

            AppendRow(builder, 1, headers);
            var rowIndex = 2;
            foreach (var row in rows)
            {
                AppendRow(builder, rowIndex, row);
                rowIndex++;
            }

            builder.Append(
                """
                  </sheetData>
                </worksheet>
                """
            );
            return builder.ToString();
        }

        private static void AppendRow(
            StringBuilder builder,
            int rowIndex,
            IReadOnlyList<string> fields
        )
        {
            builder.Append("<row r=\"");
            builder.Append(rowIndex);
            builder.Append("\">");

            for (var i = 0; i < fields.Count; i++)
            {
                var cellRef = ColumnName(i) + rowIndex;
                builder.Append("<c r=\"");
                builder.Append(cellRef);
                builder.Append("\" t=\"inlineStr\"><is><t>");
                builder.Append(XmlEscape(fields[i] ?? string.Empty));
                builder.Append("</t></is></c>");
            }

            builder.Append("</row>");
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
    }
}
