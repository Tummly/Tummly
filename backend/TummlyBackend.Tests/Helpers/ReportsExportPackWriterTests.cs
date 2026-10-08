using System.Text;
using TummlyBackend.DTOs.Reports;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class ReportsExportPackWriterTests
    {
        [Fact]
        public void RenderCaptureCsv_HeaderRow_MatchesTemplateColumns()
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
                        Claims = 1,
                        ConversionPercent = 25.0,
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
                "Source ID,QR name,Placement,Location,Status,Scans,Form opens,Feedback,Contactable,Claims,Conversion",
                FirstLine(content)
            );
            Assert.Equal(
                "7,Table tent,Table tent,Camden,Active,12,,3,2,1,25.0%",
                SecondLine(content)
            );
        }

        [Fact]
        public void RenderFeedbackRowsCsv_HeaderRow_MatchesTemplateColumns()
        {
            var rows = new[]
            {
                new ReportsExportFeedbackRowDto
                {
                    FeedbackId = 42,
                    CreatedAtUtc = new DateTime(
                        2026,
                        9,
                        29,
                        0,
                        0,
                        0,
                        DateTimeKind.Utc
                    ),
                    Tags = "Food; Service",
                    Comment = "Great food",
                    Source = "Table Tent",
                    Status = "Resolved",
                    FollowUp = "Followed up",
                    Guest = "G-1 / Maya",
                },
            };

            var (content, _) = ReportsExportPackWriter.RenderFeedbackRowsCsv(
                rows,
                locationId: 1,
                utcNow: new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc)
            );

            Assert.Equal(
                "Feedback ID,Date,Tags,Comment,Source,Status,Follow-up,Guest",
                FirstLine(content)
            );
            Assert.Contains(
                "42,29 Sep 2026,Food; Service,Great food,Table Tent,Resolved,Followed up,G-1 / Maya",
                Encoding.UTF8.GetString(content),
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void RenderCampaignsCsv_HeaderRow_MatchesTemplateColumns()
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
                        Delivered = 9,
                        Claims = 3,
                        Redemptions = 1,
                        Unsubscribes = 0,
                        Failed = 1,
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
                "Campaign ID,Campaign name,Goal,Channel,Sent,Delivered,Claims,Redemptions,Unsubs,Failed,Status",
                FirstLine(content)
            );
            Assert.Equal(
                "42,Win-back,Return,Email,10,9,3,1,0,1,Sent",
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
