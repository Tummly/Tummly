using System.Text;
using TummlyBackend.DTOs.Reports;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class ReportsExportPackWriterTests
    {
        [Fact]
        public void RenderCaptureCsv_HeaderRow_MatchesXlsxColumns()
        {
            var dto = new ReportsCaptureDto
            {
                Placements =
                [
                    new ReportsCapturePlacementDto
                    {
                        QrCodeId = 7,
                        Name = "Table tent",
                        Status = "Active",
                        Scans = 12,
                        Feedback = 3,
                        Contactable = 2,
                    },
                ],
            };

            var (content, _) = ReportsExportPackWriter.RenderCaptureCsv(
                dto,
                locationId: 1,
                locationName: "Camden",
                utcNow: new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc)
            );

            Assert.Equal(
                "Source ID,QR name,Placement,Location,Status,Scans,Form opens,Feedback,Contactable",
                FirstLine(content)
            );
            Assert.Equal(
                "7,Table tent,Table tent,Camden,Active,12,,3,2",
                SecondLine(content)
            );
        }

        [Fact]
        public void RenderFeedbackCsv_HeaderRow_MatchesXlsxColumns()
        {
            var dto = new ReportsFeedbackDto
            {
                Kpis = new ReportsFeedbackKpisDto
                {
                    FeedbackReceived = new ReportsMetricDto
                    {
                        Value = 4,
                        ValuePrevious = 2,
                    },
                },
                BySource =
                [
                    new ReportsFeedbackBySourceDto
                    {
                        Source = "QR A",
                        Feedback = 4,
                        MarketingOptIns = 1,
                        FollowUpNeeded = 0,
                    },
                ],
            };

            var (content, _) = ReportsExportPackWriter.RenderFeedbackCsv(
                dto,
                locationId: 1,
                utcNow: new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc)
            );

            Assert.Equal(
                "Section,Metric,Current,Previous,Source",
                FirstLine(content)
            );
            Assert.Contains(
                "KPI,Feedback received,4,2,",
                Encoding.UTF8.GetString(content),
                StringComparison.Ordinal
            );
            Assert.Contains(
                "By source,Feedback,4,,QR A",
                Encoding.UTF8.GetString(content),
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void RenderCampaignsCsv_HeaderRow_MatchesXlsxColumns()
        {
            var dto = new ReportsCampaignsDto
            {
                Performance =
                [
                    new ReportsCampaignsPerformanceRowDto
                    {
                        CampaignId = 42,
                        Name = "Win-back",
                        Goal = "Return",
                        Channel = "Email",
                        Sent = 10,
                        Claims = 3,
                        Redemptions = 1,
                        Unsubscribes = 0,
                        Status = "Sent",
                    },
                ],
            };

            var (content, _) = ReportsExportPackWriter.RenderCampaignsCsv(
                dto,
                locationId: 1,
                utcNow: new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc)
            );

            Assert.Equal(
                "Campaign ID,Campaign name,Goal,Channel,Sent,Claims,Redemptions,Unsubscribes,Status",
                FirstLine(content)
            );
            Assert.Equal(
                "42,Win-back,Return,Email,10,3,1,0,Sent",
                SecondLine(content)
            );
        }

        private static string FirstLine(byte[] content)
        {
            var text = Encoding.UTF8.GetString(content);
            var end = text.IndexOf('\n');
            return end < 0 ? text.TrimEnd('\r') : text[..end].TrimEnd('\r');
        }

        private static string SecondLine(byte[] content)
        {
            var text = Encoding.UTF8.GetString(content);
            var lines = text.Split('\n');
            Assert.True(lines.Length >= 2);
            return lines[1].TrimEnd('\r');
        }
    }
}
