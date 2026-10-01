using System.IO.Compression;
using System.Text;
using TummlyBackend.DTOs.Reports;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class ReportsStyledXlsxWriterTests
    {
        [Fact]
        public void Write_EmptyTable_IncludesStylesFreezeAndAutofilter()
        {
            var bytes = ReportsStyledXlsxWriter.Write(
                [
                    new ReportsStyledXlsxWriter.DataSheet(
                        "Overview",
                        "Guest Loop Overview",
                        new ReportsStyledXlsxWriter.SheetMeta(
                            "Venue",
                            "Main",
                            "01 Sep 2026 - 30 Sep 2026",
                            new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc)
                        ),
                        ["Metric", "Current period"],
                        [],
                        [28, 18]
                    ),
                ]
            );

            using var stream = new MemoryStream(bytes);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            Assert.Contains(
                archive.Entries,
                entry => entry.FullName == "xl/styles.xml"
            );
            var sheet = ReadEntry(archive, "xl/worksheets/sheet1.xml");
            Assert.Contains("ySplit=\"7\"", sheet, StringComparison.Ordinal);
            Assert.Contains("autoFilter", sheet, StringComparison.Ordinal);
            Assert.Contains("TUMMLY REPORTS", sheet, StringComparison.Ordinal);
            Assert.Contains("Powered by Tummly", sheet, StringComparison.Ordinal);
        }

        [Fact]
        public void RenderOverviewXlsx_Multi_IncludesPortfolioAndPerLocationSheets()
        {
            var locations = new List<(
                ReportsStyledXlsxPack.LocationContext Location,
                ReportsOverviewDto Dto
            )>
            {
                (
                    new ReportsStyledXlsxPack.LocationContext(
                        1,
                        "Camden",
                        "Mavero"
                    ),
                    new ReportsOverviewDto
                    {
                        LifetimeEmpty = false,
                        Funnel = new ReportsOverviewFunnelDto
                        {
                            QrScans = new ReportsMetricDto
                            {
                                Value = 10,
                                ValuePrevious = 8,
                            },
                        },
                    }
                ),
                (
                    new ReportsStyledXlsxPack.LocationContext(
                        2,
                        "Shoreditch",
                        "Mavero"
                    ),
                    new ReportsOverviewDto
                    {
                        LifetimeEmpty = false,
                        Funnel = new ReportsOverviewFunnelDto
                        {
                            QrScans = new ReportsMetricDto
                            {
                                Value = 5,
                                ValuePrevious = 4,
                            },
                        },
                    }
                ),
            };

            var (content, fileName) = ReportsStyledXlsxPack.RenderOverviewXlsx(
                locations,
                new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc)
            );

            Assert.Contains("-multi-", fileName, StringComparison.Ordinal);
            Assert.EndsWith(".xlsx", fileName);

            using var stream = new MemoryStream(content);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var workbook = ReadEntry(archive, "xl/workbook.xml");
            Assert.Contains("Portfolio Summary", workbook, StringComparison.Ordinal);
            Assert.Contains("Camden - Overview", workbook, StringComparison.Ordinal);
            Assert.Contains(
                "Shoreditch - Overview",
                workbook,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void DeduplicateSheetNames_TruncatesAndUniques()
        {
            var longName = new string('A', 40);
            var names = ReportsStyledXlsxWriter.DeduplicateSheetNames(
                [longName, longName]
            );
            Assert.Equal(2, names.Count);
            Assert.True(names[0].Length <= 31);
            Assert.True(names[1].Length <= 31);
            Assert.NotEqual(names[0], names[1]);
        }

        private static string ReadEntry(ZipArchive archive, string path)
        {
            var entry = archive.GetEntry(path);
            Assert.NotNull(entry);
            using var reader = new StreamReader(
                entry!.Open(),
                Encoding.UTF8
            );
            return reader.ReadToEnd();
        }
    }
}
