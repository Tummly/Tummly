using System.Text;
using TummlyBackend.DTOs.Reports;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class ReportsTemplatedPdfWriterTests
    {
        [Fact]
        public void Render_Single_EmitsSectionMarkersAndPdfHeader()
        {
            var document = BuildDocument(locationCount: 1);
            var (content, fileName) = ReportsTemplatedPdfWriter.Render(document);
            var text = Encoding.ASCII.GetString(content);

            Assert.StartsWith("%PDF", text, StringComparison.Ordinal);
            Assert.Contains("Guest Loop Overview", text, StringComparison.Ordinal);
            Assert.Contains("Capture Report", text, StringComparison.Ordinal);
            Assert.Contains("Feedback Report", text, StringComparison.Ordinal);
            Assert.Contains("Campaigns Report", text, StringComparison.Ordinal);
            Assert.Contains("Offer Redemption Log", text, StringComparison.Ordinal);
            Assert.Contains("Guest Consent Export", text, StringComparison.Ordinal);
            Assert.Contains("-1-", fileName, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_Multi_EmitsPortfolioAndLocationOverview()
        {
            var document = BuildDocument(locationCount: 2);
            var (content, fileName) = ReportsTemplatedPdfWriter.Render(document);
            var text = Encoding.ASCII.GetString(content);

            Assert.Contains("Portfolio Summary", text, StringComparison.Ordinal);
            Assert.Contains("Location Overview", text, StringComparison.Ordinal);
            Assert.Contains(
                "Cross-location Campaign",
                text,
                StringComparison.Ordinal
            );
            Assert.Contains("multi", fileName, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_OmitsOfferAndConsentWhenFlagsFalse()
        {
            var document = BuildDocument(locationCount: 1) with
            {
                IncludeOfferRedemptions = false,
                IncludeGuestConsent = false,
            };
            var (content, _) = ReportsTemplatedPdfWriter.Render(document);
            var text = Encoding.ASCII.GetString(content);

            Assert.DoesNotContain(
                "Offer Redemption Log",
                text,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "Guest Consent Export",
                text,
                StringComparison.Ordinal
            );
        }

        private static ReportsTemplatedPdfWriter.Document BuildDocument(
            int locationCount
        )
        {
            var locations = new List<ReportsTemplatedPdfWriter.LocationBundle>();
            for (var i = 1; i <= locationCount; i++)
            {
                locations.Add(
                    new ReportsTemplatedPdfWriter.LocationBundle(
                        LocationId: i,
                        LocationName: $"Loc {i}",
                        City: "London",
                        Overview: new ReportsOverviewDto
                        {
                            LifetimeEmpty = false,
                            Funnel = new ReportsOverviewFunnelDto
                            {
                                QrScans = new ReportsMetricDto
                                {
                                    Value = 10 * i,
                                    ValuePrevious = 5,
                                },
                                FeedbackReceived = new ReportsMetricDto
                                {
                                    Value = 4,
                                    ValuePrevious = 2,
                                },
                                MarketingOptIns = new ReportsMetricDto
                                {
                                    Value = 3,
                                    ValuePrevious = 1,
                                },
                                OfferRedemptions = new ReportsMetricDto
                                {
                                    Value = 1,
                                    ValuePrevious = 0,
                                },
                                CampaignsSent = new ReportsMetricDto
                                {
                                    Value = 1,
                                    ValuePrevious = 0,
                                },
                            },
                        },
                        Capture: new ReportsCaptureDto
                        {
                            Placements =
                            [
                                new ReportsCapturePlacementDto
                                {
                                    QrCodeId = i,
                                    Name = "Table Tent",
                                    Status = "Active",
                                    Scans = 10,
                                    Feedback = 4,
                                    Contactable = 2,
                                    Claims = 1,
                                    ConversionPercent = 40.0,
                                },
                            ],
                        },
                        FeedbackRows:
                        [
                            new ReportsExportFeedbackRowDto
                            {
                                FeedbackId = 100 + i,
                                CreatedAtUtc = new DateTime(
                                    2026,
                                    9,
                                    1,
                                    0,
                                    0,
                                    0,
                                    DateTimeKind.Utc
                                ),
                                Tags = "Service",
                                Comment = "Good",
                                Source = "Table Tent",
                                Status = "New",
                                FollowUp = "Open",
                                Guest = $"G-{i} / Guest",
                            },
                        ],
                        Campaigns: new ReportsCampaignsDto
                        {
                            Performance =
                            [
                                new ReportsCampaignsPerformanceRowDto
                                {
                                    CampaignId = i,
                                    Name = "Quiet Tuesday",
                                    Goal = "boost",
                                    Channel = "email",
                                    Sent = 10,
                                    Delivered = 9,
                                    Claims = 2,
                                    Redemptions = 1,
                                    Unsubscribes = 0,
                                    Failed = 1,
                                    Status = "sent",
                                },
                            ],
                        },
                        Redemptions: [],
                        ConsentRows:
                        [
                            [
                                $"G-{i}",
                                "Guest",
                                string.Empty,
                                "Opted in",
                                "2026-09-01 / Form",
                                "Active",
                                string.Empty,
                                "Form",
                                $"Loc {i}",
                                "Marketing",
                            ],
                        ]
                    )
                );
            }

            return new ReportsTemplatedPdfWriter.Document(
                RestaurantName: "Mavero Kitchen",
                PeriodLabel: "01 Sep 2026 - 30 Sep 2026",
                ExportedAtUtc: new DateTime(
                    2026,
                    10,
                    7,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc
                ),
                ExecutiveSummary: "Activity increased.",
                RecommendedNextAction: "Review feedback.",
                PortfolioNote: locationCount > 1 ? "Loc 1 leads." : null,
                Locations: locations,
                IncludeOfferRedemptions: true,
                IncludeGuestConsent: true
            );
        }
    }
}
