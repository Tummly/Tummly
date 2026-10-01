using System.Globalization;
using TummlyBackend.DTOs.Reports;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Maps Reports DTOs / tabular export rows into styled XLSX sheets.
    /// </summary>
    public static class ReportsStyledXlsxPack
    {
        private static readonly double[] OverviewWidths = [28, 18, 18, 16, 46];
        private static readonly double[] CaptureWidths =
        [
            14,
            28,
            20,
            24,
            12,
            10,
            12,
            12,
            12,
        ];
        private static readonly double[] FeedbackWidths = [18, 14, 18, 14, 14];
        private static readonly double[] CampaignWidths =
        [
            14,
            28,
            24,
            12,
            10,
            12,
            14,
            14,
            12,
        ];
        private static readonly double[] OffersWidths =
        [
            22,
            18,
            18,
            20,
            24,
            18,
            18,
            18,
            24,
        ];
        private static readonly double[] ConsentWidths =
        [
            22,
            18,
            18,
            20,
            24,
            18,
            18,
            18,
            18,
            22,
        ];
        private static readonly double[] PortfolioWidths =
        [
            28,
            16,
            12,
            12,
            18,
            12,
            14,
            14,
        ];

        public sealed record LocationContext(
            int LocationId,
            string LocationName,
            string RestaurantName
        );

        public static (byte[] Content, string FileName) RenderOverviewXlsx(
            IReadOnlyList<(
                LocationContext Location,
                ReportsOverviewDto Dto
            )> locations,
            DateTime fromUtc,
            DateTime toUtc,
            DateTime utcNow
        )
        {
            var period = ReportsStyledXlsxWriter.FormatPeriodLabel(fromUtc, toUtc);
            var restaurant =
                locations.Count > 0
                    ? locations[0].Location.RestaurantName
                    : "Restaurant";
            var sheets = new List<ReportsStyledXlsxWriter.DataSheet>();

            if (locations.Count > 1)
            {
                sheets.Add(
                    BuildPortfolioSheet(locations, restaurant, period, utcNow)
                );
            }

            var preferredNames = locations
                .Select(row =>
                    locations.Count == 1
                        ? "Overview"
                        : $"{row.Location.LocationName} - Overview"
                )
                .ToList();
            var names = ReportsStyledXlsxWriter.DeduplicateSheetNames(
                preferredNames
            );

            for (var i = 0; i < locations.Count; i++)
            {
                var (location, dto) = locations[i];
                sheets.Add(
                    BuildOverviewSheet(
                        names[i],
                        location,
                        dto,
                        period,
                        utcNow,
                        multi: locations.Count > 1
                    )
                );
            }

            var stamp = Stamp(utcNow);
            var scope =
                locations.Count == 1
                    ? locations[0].Location.LocationId.ToString(
                        CultureInfo.InvariantCulture
                    )
                    : "multi";
            return (
                ReportsStyledXlsxWriter.Write(sheets),
                $"tummly-reports-overview-{scope}-{stamp}Z.xlsx"
            );
        }

        public static (byte[] Content, string FileName) RenderCaptureXlsx(
            IReadOnlyList<(
                LocationContext Location,
                ReportsCaptureDto Dto
            )> locations,
            DateTime fromUtc,
            DateTime toUtc,
            DateTime utcNow
        )
        {
            var period = ReportsStyledXlsxWriter.FormatPeriodLabel(fromUtc, toUtc);
            var preferredNames = locations
                .Select(row =>
                    locations.Count == 1
                        ? "Capture"
                        : $"{row.Location.LocationName} - Capture"
                )
                .ToList();
            var names = ReportsStyledXlsxWriter.DeduplicateSheetNames(
                preferredNames
            );
            var sheets = new List<ReportsStyledXlsxWriter.DataSheet>();
            for (var i = 0; i < locations.Count; i++)
            {
                sheets.Add(
                    BuildCaptureSheet(
                        names[i],
                        locations[i].Location,
                        locations[i].Dto,
                        period,
                        utcNow
                    )
                );
            }

            var stamp = Stamp(utcNow);
            var scope =
                locations.Count == 1
                    ? locations[0].Location.LocationId.ToString(
                        CultureInfo.InvariantCulture
                    )
                    : "multi";
            return (
                ReportsStyledXlsxWriter.Write(sheets),
                $"tummly-reports-capture-{scope}-{stamp}Z.xlsx"
            );
        }

        public static (byte[] Content, string FileName) RenderFeedbackXlsx(
            IReadOnlyList<(
                LocationContext Location,
                ReportsFeedbackDto Dto
            )> locations,
            DateTime fromUtc,
            DateTime toUtc,
            DateTime utcNow
        )
        {
            var period = ReportsStyledXlsxWriter.FormatPeriodLabel(fromUtc, toUtc);
            var preferredNames = locations
                .Select(row =>
                    locations.Count == 1
                        ? "Feedback"
                        : $"{row.Location.LocationName} - Feedback"
                )
                .ToList();
            var names = ReportsStyledXlsxWriter.DeduplicateSheetNames(
                preferredNames
            );
            var sheets = new List<ReportsStyledXlsxWriter.DataSheet>();
            for (var i = 0; i < locations.Count; i++)
            {
                sheets.Add(
                    BuildFeedbackSheet(
                        names[i],
                        locations[i].Location,
                        locations[i].Dto,
                        period,
                        utcNow
                    )
                );
            }

            var stamp = Stamp(utcNow);
            var scope =
                locations.Count == 1
                    ? locations[0].Location.LocationId.ToString(
                        CultureInfo.InvariantCulture
                    )
                    : "multi";
            return (
                ReportsStyledXlsxWriter.Write(sheets),
                $"tummly-reports-feedback-{scope}-{stamp}Z.xlsx"
            );
        }

        public static (byte[] Content, string FileName) RenderCampaignsXlsx(
            IReadOnlyList<(
                LocationContext Location,
                ReportsCampaignsDto Dto
            )> locations,
            DateTime fromUtc,
            DateTime toUtc,
            DateTime utcNow
        )
        {
            var period = ReportsStyledXlsxWriter.FormatPeriodLabel(fromUtc, toUtc);
            var preferredNames = locations
                .Select(row =>
                    locations.Count == 1
                        ? "Campaigns"
                        : $"{row.Location.LocationName} - Campaigns"
                )
                .ToList();
            var names = ReportsStyledXlsxWriter.DeduplicateSheetNames(
                preferredNames
            );
            var sheets = new List<ReportsStyledXlsxWriter.DataSheet>();
            for (var i = 0; i < locations.Count; i++)
            {
                sheets.Add(
                    BuildCampaignsSheet(
                        names[i],
                        locations[i].Location,
                        locations[i].Dto,
                        period,
                        utcNow
                    )
                );
            }

            var stamp = Stamp(utcNow);
            var scope =
                locations.Count == 1
                    ? locations[0].Location.LocationId.ToString(
                        CultureInfo.InvariantCulture
                    )
                    : "multi";
            return (
                ReportsStyledXlsxWriter.Write(sheets),
                $"tummly-reports-campaigns-{scope}-{stamp}Z.xlsx"
            );
        }

        public static (byte[] Content, string FileName) RenderOffersRedemptionsXlsx(
            IReadOnlyList<(
                LocationContext Location,
                IReadOnlyList<string[]> Rows
            )> locations,
            IReadOnlyList<string> headers,
            DateTime fromUtc,
            DateTime toUtc,
            DateTime utcNow
        )
        {
            var period = ReportsStyledXlsxWriter.FormatPeriodLabel(fromUtc, toUtc);
            var preferredNames = locations
                .Select(row =>
                    locations.Count == 1
                        ? "Offer Redemptions"
                        : $"{row.Location.LocationName} - Offers"
                )
                .ToList();
            var names = ReportsStyledXlsxWriter.DeduplicateSheetNames(
                preferredNames
            );
            var sheets = new List<ReportsStyledXlsxWriter.DataSheet>();
            for (var i = 0; i < locations.Count; i++)
            {
                var location = locations[i].Location;
                sheets.Add(
                    new ReportsStyledXlsxWriter.DataSheet(
                        names[i],
                        "Offer Redemption Log",
                        new ReportsStyledXlsxWriter.SheetMeta(
                            location.RestaurantName,
                            location.LocationName,
                            period,
                            utcNow
                        ),
                        headers,
                        locations[i].Rows,
                        OffersWidths
                    )
                );
            }

            var stamp = Stamp(utcNow);
            var scope =
                locations.Count == 1
                    ? locations[0].Location.LocationId.ToString(
                        CultureInfo.InvariantCulture
                    )
                    : "multi";
            return (
                ReportsStyledXlsxWriter.Write(sheets),
                $"tummly-offers-redemptions-{scope}-{stamp}Z.xlsx"
            );
        }

        public static (byte[] Content, string FileName) RenderGuestConsentXlsx(
            IReadOnlyList<(
                LocationContext Location,
                IReadOnlyList<string[]> Rows
            )> locations,
            IReadOnlyList<string> headers,
            DateTime utcNow
        )
        {
            var period = ReportsStyledXlsxWriter.FormatExportedLabel(utcNow);
            var preferredNames = locations
                .Select(row =>
                    locations.Count == 1
                        ? "Guest Consent"
                        : $"{row.Location.LocationName} - Consent"
                )
                .ToList();
            var names = ReportsStyledXlsxWriter.DeduplicateSheetNames(
                preferredNames
            );
            var sheets = new List<ReportsStyledXlsxWriter.DataSheet>();
            for (var i = 0; i < locations.Count; i++)
            {
                var location = locations[i].Location;
                sheets.Add(
                    new ReportsStyledXlsxWriter.DataSheet(
                        names[i],
                        "Guest Consent Export",
                        new ReportsStyledXlsxWriter.SheetMeta(
                            location.RestaurantName,
                            location.LocationName,
                            period,
                            utcNow,
                            FileTypeLabel: "XLSX restricted export"
                        ),
                        headers,
                        locations[i].Rows,
                        ConsentWidths,
                        Disclaimer: ReportsStyledXlsxWriter.RestrictedDisclaimer
                    )
                );
            }

            var stamp = Stamp(utcNow);
            var scope =
                locations.Count == 1
                    ? locations[0].Location.LocationId.ToString(
                        CultureInfo.InvariantCulture
                    )
                    : "multi";
            return (
                ReportsStyledXlsxWriter.Write(sheets),
                $"tummly-consent-permission-records-{scope}-{stamp}Z.xlsx"
            );
        }

        private static ReportsStyledXlsxWriter.DataSheet BuildPortfolioSheet(
            IReadOnlyList<(
                LocationContext Location,
                ReportsOverviewDto Dto
            )> locations,
            string restaurantName,
            string period,
            DateTime utcNow
        )
        {
            var headers = new[]
            {
                "Location",
                "City",
                "QR scans",
                "Feedback",
                "Contactable guests",
                "Claims",
                "Redemptions",
                "Campaigns sent",
            };
            var rows = new List<IReadOnlyList<string>>();
            foreach (var (location, dto) in locations)
            {
                var funnel = dto.Funnel;
                var offers = dto.OffersAndCampaigns;
                rows.Add(
                    [
                        location.LocationName,
                        string.Empty,
                        Int(funnel?.QrScans.Value),
                        Int(funnel?.FeedbackReceived.Value),
                        Int(funnel?.MarketingOptIns.Value),
                        Int(offers?.OfferClaims.Value),
                        Int(funnel?.OfferRedemptions.Value),
                        Int(funnel?.CampaignsSent.Value),
                    ]
                );
            }

            return new ReportsStyledXlsxWriter.DataSheet(
                "Portfolio Summary",
                "Portfolio Summary",
                new ReportsStyledXlsxWriter.SheetMeta(
                    restaurantName,
                    LocationLabel: null,
                    period,
                    utcNow,
                    ScopeLabel: "All selected locations"
                ),
                headers,
                rows,
                PortfolioWidths
            );
        }

        private static ReportsStyledXlsxWriter.DataSheet BuildOverviewSheet(
            string sheetName,
            LocationContext location,
            ReportsOverviewDto dto,
            string period,
            DateTime utcNow,
            bool multi
        )
        {
            var headers = new[]
            {
                "Metric",
                "Current period",
                "Previous period",
                "Change",
                "Notes",
            };
            var rows = new List<IReadOnlyList<string>>();
            void AddMetric(string label, ReportsMetricDto? metric, string notes)
            {
                metric ??= new ReportsMetricDto();
                rows.Add(
                    [
                        label,
                        Int(metric.Value),
                        Int(metric.ValuePrevious),
                        ReportsStyledXlsxWriter.FormatChange(
                            metric.Value,
                            metric.ValuePrevious
                        ),
                        notes,
                    ]
                );
            }

            if (dto.Funnel != null)
            {
                AddMetric(
                    "QR scans",
                    dto.Funnel.QrScans,
                    "Total valid scans across active placements."
                );
                AddMetric(
                    "Feedback received",
                    dto.Funnel.FeedbackReceived,
                    "Private feedback submissions."
                );
                AddMetric(
                    "Contactable guests",
                    dto.Funnel.MarketingOptIns,
                    "Valid contact and eligible consent status."
                );
                AddMetric(
                    "Offer redemptions",
                    dto.Funnel.OfferRedemptions,
                    "Successful offer redemptions."
                );
                AddMetric(
                    "Campaigns sent",
                    dto.Funnel.CampaignsSent,
                    "Campaigns sent in the selected period."
                );
            }

            return new ReportsStyledXlsxWriter.DataSheet(
                sheetName,
                multi ? "Guest Loop Overview" : "Guest Loop Overview",
                new ReportsStyledXlsxWriter.SheetMeta(
                    location.RestaurantName,
                    location.LocationName,
                    period,
                    utcNow
                ),
                headers,
                rows,
                OverviewWidths,
                WrapColumnIndexes: new HashSet<int> { 4 }
            );
        }

        private static ReportsStyledXlsxWriter.DataSheet BuildCaptureSheet(
            string sheetName,
            LocationContext location,
            ReportsCaptureDto dto,
            string period,
            DateTime utcNow
        )
        {
            var headers = new[]
            {
                "Source ID",
                "QR name",
                "Placement",
                "Location",
                "Status",
                "Scans",
                "Form opens",
                "Feedback",
                "Contactable",
            };
            var rows = new List<IReadOnlyList<string>>();
            foreach (var row in dto.Placements ?? [])
            {
                rows.Add(
                    [
                        row.QrCodeId.ToString(CultureInfo.InvariantCulture),
                        row.Name,
                        row.Name,
                        location.LocationName,
                        row.Status,
                        Int(row.Scans),
                        string.Empty,
                        Int(row.Feedback),
                        Int(row.Contactable),
                    ]
                );
            }

            return new ReportsStyledXlsxWriter.DataSheet(
                sheetName,
                "Capture Report",
                new ReportsStyledXlsxWriter.SheetMeta(
                    location.RestaurantName,
                    location.LocationName,
                    period,
                    utcNow
                ),
                headers,
                rows,
                CaptureWidths
            );
        }

        private static ReportsStyledXlsxWriter.DataSheet BuildFeedbackSheet(
            string sheetName,
            LocationContext location,
            ReportsFeedbackDto dto,
            string period,
            DateTime utcNow
        )
        {
            // Aggregate grain (lock 09) — KPI / status / by-source, not inbox dump.
            var headers = new[]
            {
                "Section",
                "Metric",
                "Current",
                "Previous",
                "Source",
            };
            var rows = new List<IReadOnlyList<string>>();
            void Add(
                string section,
                string metric,
                ReportsMetricDto value
            )
            {
                rows.Add(
                    [
                        section,
                        metric,
                        Int(value.Value),
                        Int(value.ValuePrevious),
                        string.Empty,
                    ]
                );
            }

            if (dto.Kpis != null)
            {
                Add("KPI", "Feedback received", dto.Kpis.FeedbackReceived);
                Add("KPI", "Marketing opt-ins", dto.Kpis.MarketingOptIns);
                Add("KPI", "Follow-up needed", dto.Kpis.FollowUpNeeded);
                Add("KPI", "Resolved", dto.Kpis.Resolved);
            }

            if (dto.Status != null)
            {
                Add("Status", "New", dto.Status.New);
                Add("Status", "In progress", dto.Status.InProgress);
                Add("Status", "Follow-up needed", dto.Status.FollowUpNeeded);
                Add("Status", "Resolved", dto.Status.Resolved);
            }

            foreach (var row in dto.BySource ?? [])
            {
                rows.Add(
                    [
                        "By source",
                        "Feedback",
                        Int(row.Feedback),
                        string.Empty,
                        row.Source,
                    ]
                );
                rows.Add(
                    [
                        "By source",
                        "Marketing opt-ins",
                        Int(row.MarketingOptIns),
                        string.Empty,
                        row.Source,
                    ]
                );
                rows.Add(
                    [
                        "By source",
                        "Follow-up needed",
                        Int(row.FollowUpNeeded),
                        string.Empty,
                        row.Source,
                    ]
                );
            }

            return new ReportsStyledXlsxWriter.DataSheet(
                sheetName,
                "Feedback Report",
                new ReportsStyledXlsxWriter.SheetMeta(
                    location.RestaurantName,
                    location.LocationName,
                    period,
                    utcNow
                ),
                headers,
                rows,
                FeedbackWidths
            );
        }

        private static ReportsStyledXlsxWriter.DataSheet BuildCampaignsSheet(
            string sheetName,
            LocationContext location,
            ReportsCampaignsDto dto,
            string period,
            DateTime utcNow
        )
        {
            var headers = new[]
            {
                "Campaign ID",
                "Campaign name",
                "Goal",
                "Channel",
                "Sent",
                "Claims",
                "Redemptions",
                "Unsubscribes",
                "Status",
            };
            var rows = new List<IReadOnlyList<string>>();
            foreach (var row in dto.Performance ?? [])
            {
                rows.Add(
                    [
                        row.CampaignId.ToString(CultureInfo.InvariantCulture),
                        row.Name,
                        row.Goal ?? string.Empty,
                        row.Channel ?? string.Empty,
                        Int(row.Sent),
                        Int(row.Claims),
                        Int(row.Redemptions),
                        Int(row.Unsubscribes),
                        row.Status,
                    ]
                );
            }

            return new ReportsStyledXlsxWriter.DataSheet(
                sheetName,
                "Campaigns Report",
                new ReportsStyledXlsxWriter.SheetMeta(
                    location.RestaurantName,
                    location.LocationName,
                    period,
                    utcNow
                ),
                headers,
                rows,
                CampaignWidths
            );
        }

        private static string Int(int? value)
            => (value ?? 0).ToString(CultureInfo.InvariantCulture);

        private static string Int(int value)
            => value.ToString(CultureInfo.InvariantCulture);

        private static string Stamp(DateTime utcNow)
            => utcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
    }
}
