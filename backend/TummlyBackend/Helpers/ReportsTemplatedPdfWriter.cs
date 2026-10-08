using System.Globalization;
using System.Text;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.DTOs.Reports;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Landscape templated Reports PDF pack (REP-03) — section parity with the
    /// approved single/multi templates; Helvetica chrome, not pixel ReportLab.
    /// </summary>
    public static class ReportsTemplatedPdfWriter
    {
        public const string ContentType = "application/pdf";

        public const int SoftMaxCapture = 100;
        public const int SoftMaxFeedback = ReportsExportFeedbackRows.SoftMaxRows;
        public const int SoftMaxCampaigns = 100;
        public const int SoftMaxRedemptions = 200;
        public const int SoftMaxConsent = 200;

        private const float PageWidth = 842f;
        private const float PageHeight = 595f;
        private const float MarginLeft = 36f;
        private const float MarginRight = 36f;
        private const float MarginBottom = 36f;
        private const float LineHeight = 11f;
        private const float TopY = PageHeight - 28f;

        public sealed record KpiCard(
            string Value,
            string Label,
            string Delta,
            string Hint
        );

        public sealed record LocationBundle(
            int LocationId,
            string LocationName,
            string City,
            ReportsOverviewDto Overview,
            ReportsCaptureDto Capture,
            IReadOnlyList<ReportsExportFeedbackRowDto> FeedbackRows,
            ReportsCampaignsDto Campaigns,
            IReadOnlyList<OfferDetailsRedemptionListItemDto> Redemptions,
            IReadOnlyList<string[]> ConsentRows
        );

        public sealed record Document(
            string RestaurantName,
            string PeriodLabel,
            DateTime ExportedAtUtc,
            string ExecutiveSummary,
            string RecommendedNextAction,
            string? PortfolioNote,
            IReadOnlyList<LocationBundle> Locations,
            bool IncludeOfferRedemptions,
            bool IncludeGuestConsent
        );

        public static (byte[] Content, string FileName) Render(
            Document document
        )
        {
            var stamp = document.ExportedAtUtc.ToString(
                "yyyyMMdd-HHmmss",
                CultureInfo.InvariantCulture
            );
            var scopeToken =
                document.Locations.Count == 1
                    ? document.Locations[0].LocationId.ToString(
                        CultureInfo.InvariantCulture
                    )
                    : "multi";
            var fileName = $"tummly-reports-overview-{scopeToken}-{stamp}Z.pdf";
            var pages =
                document.Locations.Count == 1
                    ? BuildSinglePages(document)
                    : BuildMultiPages(document);
            return (BuildPdf(pages), fileName);
        }

        private static IReadOnlyList<string> BuildSinglePages(Document document)
        {
            var pages = new List<StringBuilder>();
            var loc = document.Locations[0];
            var meta = Meta(
                document.RestaurantName,
                $"LOCATION {loc.LocationName}",
                document.PeriodLabel
            );

            WriteOverviewPage(
                pages,
                document,
                loc,
                meta,
                title: "Guest Loop Overview",
                intro:
                    "A concise, printable summary of what happened across the Guest Loop during the selected period. Detailed report sections follow on the next pages."
            );
            WriteCapturePage(pages, document, loc, meta);
            WriteFeedbackPage(pages, document, loc, meta);
            WriteCampaignsPage(pages, document, loc, meta);
            if (document.IncludeOfferRedemptions)
            {
                WriteRedemptionsPage(pages, document, loc, meta);
            }

            if (document.IncludeGuestConsent)
            {
                WriteConsentPage(pages, document, loc, meta);
            }

            return FinalizePages(pages, document.ExportedAtUtc);
        }

        private static IReadOnlyList<string> BuildMultiPages(Document document)
        {
            var pages = new List<StringBuilder>();
            var meta = Meta(
                document.RestaurantName,
                "SCOPE All selected locations",
                document.PeriodLabel
            );
            WritePortfolioPage(pages, document, meta);

            foreach (var loc in document.Locations)
            {
                var locMeta = Meta(
                    document.RestaurantName,
                    $"LOCATION {loc.LocationName}",
                    document.PeriodLabel
                );
                WriteLocationOverviewPage(pages, document, loc, locMeta);
            }

            WriteCrossCampaignPage(pages, document, meta);
            return FinalizePages(pages, document.ExportedAtUtc);
        }

        private static void WriteOverviewPage(
            List<StringBuilder> pages,
            Document document,
            LocationBundle loc,
            string metaLine,
            string title,
            string intro
        )
        {
            var sb = NewPage(pages);
            var y = WriteChrome(sb, title, metaLine);
            y -= 8f;
            SetMuted(sb);
            (sb, y) = WriteWrapped(pages, sb, y, intro, 110);
            y -= 10f;
            var cards = BuildKpis(loc.Overview);
            (sb, y) = WriteKpiStrip(pages, sb, y, cards);
            y -= 14f;
            SetBlack(sb);
            TextAt(sb, "F2", 10, MarginLeft, y, "Executive summary");
            y -= LineHeight;
            SetMuted(sb);
            (sb, y) = WriteWrapped(
                pages,
                sb,
                y,
                document.ExecutiveSummary,
                110
            );
            y -= 10f;
            SetBlack(sb);
            TextAt(sb, "F2", 10, MarginLeft, y, "Recommended next action");
            y -= LineHeight;
            SetMuted(sb);
            (sb, y) = WriteWrapped(
                pages,
                sb,
                y,
                document.RecommendedNextAction,
                110
            );
            _ = y;
        }

        private static void WritePortfolioPage(
            List<StringBuilder> pages,
            Document document,
            string metaLine
        )
        {
            var sb = NewPage(pages);
            var y = WriteChrome(sb, "Portfolio Summary", metaLine);
            y -= 8f;
            SetMuted(sb);
            (sb, y) = WriteWrapped(
                pages,
                sb,
                y,
                "Cross-location Guest Loop summary for the selected reporting period. Use the location sections that follow for drill-down detail.",
                110
            );
            y -= 10f;
            var rolled = RollUpOverview(document.Locations);
            (sb, y) = WriteKpiStrip(pages, sb, y, BuildKpis(rolled));
            y -= 12f;
            SetBlack(sb);
            TextAt(sb, "F2", 10, MarginLeft, y, "Location");
            y -= LineHeight;
            foreach (var loc in document.Locations)
            {
                var funnel = loc.Overview.Funnel;
                var line =
                    $"{loc.LocationName} | {loc.City} | QR {Metric(funnel?.QrScans)} | Feedback {Metric(funnel?.FeedbackReceived)} | Contactable {Metric(funnel?.MarketingOptIns)} | Redemptions {Metric(funnel?.OfferRedemptions)} | Campaigns {Metric(funnel?.CampaignsSent)}";
                SetMuted(sb);
                (sb, y) = WriteWrapped(pages, sb, y, line, 120);
            }

            if (!string.IsNullOrWhiteSpace(document.PortfolioNote))
            {
                y -= 8f;
                SetBlack(sb);
                TextAt(sb, "F2", 10, MarginLeft, y, "Portfolio note");
                y -= LineHeight;
                SetMuted(sb);
                (sb, y) = WriteWrapped(
                    pages,
                    sb,
                    y,
                    document.PortfolioNote!,
                    110
                );
            }

            _ = y;
        }

        private static void WriteLocationOverviewPage(
            List<StringBuilder> pages,
            Document document,
            LocationBundle loc,
            string metaLine
        )
        {
            var sb = NewPage(pages);
            var y = WriteChrome(sb, "Location Overview", metaLine);
            y -= 8f;
            (sb, y) = WriteKpiStrip(pages, sb, y, BuildKpis(loc.Overview));
            y -= 10f;
            SetBlack(sb);
            TextAt(sb, "F2", 10, MarginLeft, y, "Capture snapshot");
            y -= LineHeight;
            (sb, y) = WriteCaptureRows(pages, sb, y, loc.Capture, SoftMaxCapture);
            y -= 8f;
            SetBlack(sb);
            TextAt(sb, "F2", 10, MarginLeft, y, "Feedback snapshot");
            y -= LineHeight;
            (sb, y) = WriteFeedbackRows(
                pages,
                sb,
                y,
                loc.FeedbackRows,
                SoftMaxFeedback
            );
            _ = y;
            _ = document;
        }

        private static void WriteCapturePage(
            List<StringBuilder> pages,
            Document document,
            LocationBundle loc,
            string metaLine
        )
        {
            var sb = NewPage(pages);
            var y = WriteChrome(sb, "Capture Report", metaLine);
            y -= 8f;
            SetMuted(sb);
            (sb, y) = WriteWrapped(
                pages,
                sb,
                y,
                "QR/source performance and conversion. No guest PII is required in this section.",
                110
            );
            y -= 8f;
            (sb, y) = WriteCaptureRows(pages, sb, y, loc.Capture, SoftMaxCapture);
            _ = y;
            _ = document;
        }

        private static void WriteFeedbackPage(
            List<StringBuilder> pages,
            Document document,
            LocationBundle loc,
            string metaLine
        )
        {
            var sb = NewPage(pages);
            var y = WriteChrome(sb, "Feedback Report", metaLine);
            y -= 8f;
            SetMuted(sb);
            (sb, y) = WriteWrapped(
                pages,
                sb,
                y,
                "Private feedback records and follow-up status. Comments and guest linkage are sensitive and should follow export permissions.",
                110
            );
            y -= 8f;
            (sb, y) = WriteFeedbackRows(
                pages,
                sb,
                y,
                loc.FeedbackRows,
                SoftMaxFeedback
            );
            _ = y;
            _ = document;
        }

        private static void WriteCampaignsPage(
            List<StringBuilder> pages,
            Document document,
            LocationBundle loc,
            string metaLine
        )
        {
            var sb = NewPage(pages);
            var y = WriteChrome(sb, "Campaigns Report", metaLine);
            y -= 8f;
            SetMuted(sb);
            (sb, y) = WriteWrapped(
                pages,
                sb,
                y,
                "Campaign delivery and performance for the selected period. Recipient-level detail should remain a separate permissioned export.",
                110
            );
            y -= 8f;
            (sb, y) = WriteCampaignRows(
                pages,
                sb,
                y,
                loc.Campaigns.Performance ?? [],
                SoftMaxCampaigns
            );
            _ = y;
            _ = document;
        }

        private static void WriteCrossCampaignPage(
            List<StringBuilder> pages,
            Document document,
            string metaLine
        )
        {
            var sb = NewPage(pages);
            var y = WriteChrome(
                sb,
                "Cross-location Campaign & Offer Summary",
                metaLine
            );
            y -= 8f;
            SetBlack(sb);
            TextAt(sb, "F2", 10, MarginLeft, y, "Campaign performance");
            y -= LineHeight;
            var rows = document.Locations
                .SelectMany(loc =>
                    (loc.Campaigns.Performance ?? []).Select(row =>
                        (loc.LocationName, row)
                    )
                )
                .Take(SoftMaxCampaigns)
                .ToList();
            if (rows.Count == 0)
            {
                SetMuted(sb);
                TextAt(sb, "F1", 9, MarginLeft, y, "(none)");
                return;
            }

            foreach (var (locationName, row) in rows)
            {
                var line =
                    $"{row.Name} | {locationName} | {row.Channel ?? "-"} | Sent {row.Sent} | Delivered {row.Delivered} | Claims {row.Claims} | Redemptions {row.Redemptions} | {row.Status}";
                SetMuted(sb);
                (sb, y) = WriteWrapped(pages, sb, y, line, 120);
            }

            _ = y;
        }

        private static void WriteRedemptionsPage(
            List<StringBuilder> pages,
            Document document,
            LocationBundle loc,
            string metaLine
        )
        {
            var sb = NewPage(pages);
            var y = WriteChrome(sb, "Offer Redemption Log", metaLine);
            y -= 8f;
            SetMuted(sb);
            (sb, y) = WriteWrapped(
                pages,
                sb,
                y,
                "Offer claims and redemptions. Full unique codes/tokens should not be exposed by default.",
                110
            );
            y -= 8f;
            var rows = loc.Redemptions.Take(SoftMaxRedemptions).ToList();
            if (rows.Count == 0)
            {
                SetMuted(sb);
                TextAt(sb, "F1", 9, MarginLeft, y, "(none)");
                return;
            }

            foreach (var row in rows)
            {
                var expiry =
                    row.ExpiresAtUtc?.ToString(
                        "dd MMM yyyy",
                        CultureInfo.InvariantCulture
                    ) ?? "-";
                var line =
                    $"{row.OfferTitle} | {row.PassReferenceText} | {row.Id} | {row.DateTimeUtc:dd MMM yyyy} | {row.OutcomeLabel} | {expiry} | {row.ReasonLabel ?? string.Empty}";
                SetMuted(sb);
                (sb, y) = WriteWrapped(pages, sb, y, line, 120);
            }

            if (loc.Redemptions.Count > SoftMaxRedemptions)
            {
                y -= 4f;
                SetMuted(sb);
                TextAt(
                    sb,
                    "F1",
                    8,
                    MarginLeft,
                    y,
                    $"Showing first {SoftMaxRedemptions} rows."
                );
            }

            _ = y;
            _ = document;
        }

        private static void WriteConsentPage(
            List<StringBuilder> pages,
            Document document,
            LocationBundle loc,
            string metaLine
        )
        {
            var restrictedMeta = metaLine.Replace(
                "PDF report export",
                "PDF restricted export",
                StringComparison.Ordinal
            );
            var sb = NewPage(pages);
            var y = WriteChrome(sb, "Guest Consent Export", restrictedMeta);
            y -= 8f;
            SetMuted(sb);
            (sb, y) = WriteWrapped(
                pages,
                sb,
                y,
                "Restricted export. Contains consent/contact linkage. Export only for authorised roles and handle securely.",
                110
            );
            y -= 8f;
            var rows = loc.ConsentRows.Take(SoftMaxConsent).ToList();
            if (rows.Count == 0)
            {
                SetMuted(sb);
                TextAt(sb, "F1", 9, MarginLeft, y, "(none)");
                return;
            }

            foreach (var row in rows)
            {
                var line = string.Join(" | ", row);
                SetMuted(sb);
                (sb, y) = WriteWrapped(pages, sb, y, line, 120);
            }

            if (loc.ConsentRows.Count > SoftMaxConsent)
            {
                y -= 4f;
                SetMuted(sb);
                TextAt(
                    sb,
                    "F1",
                    8,
                    MarginLeft,
                    y,
                    $"Showing first {SoftMaxConsent} rows."
                );
            }

            _ = y;
            _ = document;
        }

        private static (StringBuilder Sb, float Y) WriteCaptureRows(
            List<StringBuilder> pages,
            StringBuilder sb,
            float y,
            ReportsCaptureDto capture,
            int softMax
        )
        {
            var placements = (capture.Placements ?? []).Take(softMax).ToList();
            if (placements.Count == 0)
            {
                SetMuted(sb);
                TextAt(sb, "F1", 9, MarginLeft, y, "(none)");
                return (sb, y - LineHeight);
            }

            foreach (var row in placements)
            {
                var formOpens =
                    row.FormOpens?.ToString(CultureInfo.InvariantCulture) ?? "-";
                var conversion =
                    row.ConversionPercent is double pct
                        ? pct.ToString("0.0", CultureInfo.InvariantCulture)
                            + "%"
                        : "-";
                var line =
                    $"{row.QrCodeId} | {row.Name} | {row.Status} | Scans {row.Scans} | Form opens {formOpens} | Feedback {row.Feedback} | Contactable {row.Contactable} | Claims {row.Claims} | Conversion {conversion}";
                SetMuted(sb);
                (sb, y) = WriteWrapped(pages, sb, y, line, 120);
            }

            return (sb, y);
        }

        private static (StringBuilder Sb, float Y) WriteFeedbackRows(
            List<StringBuilder> pages,
            StringBuilder sb,
            float y,
            IReadOnlyList<ReportsExportFeedbackRowDto> rows,
            int softMax
        )
        {
            var slice = rows.Take(softMax).ToList();
            if (slice.Count == 0)
            {
                SetMuted(sb);
                TextAt(sb, "F1", 9, MarginLeft, y, "(none)");
                return (sb, y - LineHeight);
            }

            foreach (var row in slice)
            {
                var line =
                    $"{row.FeedbackId} | {row.CreatedAtUtc:dd MMM yyyy} | {row.Tags} | {row.Comment} | {row.Source} | {row.Status} | {row.FollowUp} | {row.Guest}";
                SetMuted(sb);
                (sb, y) = WriteWrapped(pages, sb, y, line, 120);
            }

            return (sb, y);
        }

        private static (StringBuilder Sb, float Y) WriteCampaignRows(
            List<StringBuilder> pages,
            StringBuilder sb,
            float y,
            IReadOnlyList<ReportsCampaignsPerformanceRowDto> rows,
            int softMax
        )
        {
            var slice = rows.Take(softMax).ToList();
            if (slice.Count == 0)
            {
                SetMuted(sb);
                TextAt(sb, "F1", 9, MarginLeft, y, "(none)");
                return (sb, y - LineHeight);
            }

            foreach (var row in slice)
            {
                var line =
                    $"{row.CampaignId} | {row.Name} | {row.Goal ?? "-"} | {row.Channel ?? "-"} | Sent {row.Sent} | Delivered {row.Delivered} | Claims {row.Claims} | Redemptions {row.Redemptions} | Unsubs {row.Unsubscribes} | Failed {row.Failed} | {row.Status}";
                SetMuted(sb);
                (sb, y) = WriteWrapped(pages, sb, y, line, 120);
            }

            return (sb, y);
        }

        private static (StringBuilder Sb, float Y) WriteKpiStrip(
            List<StringBuilder> pages,
            StringBuilder sb,
            float y,
            IReadOnlyList<KpiCard> cards
        )
        {
            foreach (var card in cards)
            {
                SetBlack(sb);
                TextAt(
                    sb,
                    "F2",
                    14,
                    MarginLeft,
                    y,
                    $"{card.Value}  {card.Label}"
                );
                y -= LineHeight;
                SetMuted(sb);
                TextAt(sb, "F1", 8, MarginLeft, y, $"{card.Delta}  {card.Hint}");
                y -= LineHeight + 2f;
                (sb, y) = EnsureSpace(pages, sb, y, LineHeight * 2);
            }

            return (sb, y);
        }

        public static IReadOnlyList<KpiCard> BuildKpis(ReportsOverviewDto overview)
        {
            var funnel = overview.Funnel;
            return
            [
                Card("QR scans", funnel?.QrScans, "Valid scans across active placements."),
                Card(
                    "Feedback received",
                    funnel?.FeedbackReceived,
                    "Private guest feedback submissions."
                ),
                Card(
                    "Contactable guests",
                    funnel?.MarketingOptIns,
                    "Eligible contactable guests."
                ),
                Card(
                    "Offer redemptions",
                    funnel?.OfferRedemptions,
                    "Successful redemptions."
                ),
                Card(
                    "Campaigns sent",
                    funnel?.CampaignsSent,
                    "Campaigns sent in period."
                ),
            ];
        }

        private static KpiCard Card(
            string label,
            ReportsMetricDto? metric,
            string hint
        )
        {
            var value = metric?.Value ?? 0;
            var prev = metric?.ValuePrevious ?? 0;
            var delta = value - prev;
            var deltaText =
                delta == 0
                    ? "No change vs previous period"
                    : $"{(delta > 0 ? "+" : string.Empty)}{delta} vs previous period";
            return new KpiCard(
                value.ToString("N0", CultureInfo.InvariantCulture),
                label,
                deltaText,
                hint
            );
        }

        public static string BuildExecutiveSummary(ReportsOverviewDto overview)
        {
            if (overview.LifetimeEmpty || overview.Funnel is null)
            {
                return "No Guest Loop activity in this period.";
            }

            var scans = overview.Funnel.QrScans.Value;
            var feedback = overview.Funnel.FeedbackReceived.Value;
            var contactable = overview.Funnel.MarketingOptIns.Value;
            var redemptions = overview.Funnel.OfferRedemptions.Value;
            return $"Guest Loop activity in the period: {scans} QR scans, {feedback} feedback submissions, {contactable} contactable guests, and {redemptions} offer redemptions.";
        }

        public static string BuildRecommendedNextAction(
            ReportsOverviewDto overview
        )
        {
            var actions = overview.RecommendedActions;
            if (actions is { Count: > 0 })
            {
                return "Review recommended actions from the Reports hub, then prepare a targeted follow-up or campaign for the eligible audience.";
            }

            if (
                overview.PrivateFeedback?.FollowUpNeeded.Value > 0
            )
            {
                return "Review unresolved feedback first, then use the eligible audience to prepare a targeted follow-up or campaign.";
            }

            return "Review Capture and Feedback trends, then prepare a targeted follow-up or campaign for eligible guests.";
        }

        public static string BuildPortfolioNote(
            IReadOnlyList<LocationBundle> locations
        )
        {
            if (locations.Count == 0)
            {
                return "No locations in this export.";
            }

            var ranked = locations
                .Select(loc =>
                    (
                        loc.LocationName,
                        Scans: loc.Overview.Funnel?.QrScans.Value ?? 0,
                        Feedback: loc.Overview.Funnel?.FeedbackReceived.Value
                            ?? 0
                    )
                )
                .OrderByDescending(row => row.Scans)
                .ToList();
            var top = ranked[0];
            if (ranked.Count == 1)
            {
                return $"{top.LocationName} is the only location in this export.";
            }

            var bottom = ranked[^1];
            return $"{top.LocationName} leads on QR engagement. {bottom.LocationName} shows the clearest opportunity to improve QR-to-feedback conversion.";
        }

        private static ReportsOverviewDto RollUpOverview(
            IReadOnlyList<LocationBundle> locations
        )
        {
            int Sum(Func<ReportsOverviewFunnelDto, ReportsMetricDto> pick)
            {
                return locations.Sum(loc =>
                    loc.Overview.Funnel is null
                        ? 0
                        : pick(loc.Overview.Funnel).Value
                );
            }

            int SumPrev(Func<ReportsOverviewFunnelDto, ReportsMetricDto> pick)
            {
                return locations.Sum(loc =>
                    loc.Overview.Funnel is null
                        ? 0
                        : pick(loc.Overview.Funnel).ValuePrevious
                );
            }

            return new ReportsOverviewDto
            {
                LifetimeEmpty = locations.All(loc => loc.Overview.LifetimeEmpty),
                Funnel = new ReportsOverviewFunnelDto
                {
                    QrScans = new ReportsMetricDto
                    {
                        Value = Sum(f => f.QrScans),
                        ValuePrevious = SumPrev(f => f.QrScans),
                    },
                    FeedbackReceived = new ReportsMetricDto
                    {
                        Value = Sum(f => f.FeedbackReceived),
                        ValuePrevious = SumPrev(f => f.FeedbackReceived),
                    },
                    MarketingOptIns = new ReportsMetricDto
                    {
                        Value = Sum(f => f.MarketingOptIns),
                        ValuePrevious = SumPrev(f => f.MarketingOptIns),
                    },
                    OfferRedemptions = new ReportsMetricDto
                    {
                        Value = Sum(f => f.OfferRedemptions),
                        ValuePrevious = SumPrev(f => f.OfferRedemptions),
                    },
                    CampaignsSent = new ReportsMetricDto
                    {
                        Value = Sum(f => f.CampaignsSent),
                        ValuePrevious = SumPrev(f => f.CampaignsSent),
                    },
                },
            };
        }

        private static string Meta(
            string restaurant,
            string scope,
            string period
        )
            => $"RESTAURANT {restaurant}  {scope}  REPORT PERIOD {period}  FILE TYPE PDF report export";

        private static float WriteChrome(
            StringBuilder sb,
            string title,
            string metaLine
        )
        {
            var y = TopY;
            SetBlack(sb);
            TextAt(sb, "F2", 9, MarginLeft, y, "TUMMLY REPORTS");
            TextAt(
                sb,
                "F1",
                8,
                PageWidth - MarginRight - 100,
                y,
                "PDF report export"
            );
            y -= 16f;
            TextAt(sb, "F2", 16, MarginLeft, y, title);
            y -= 14f;
            SetMuted(sb);
            TextAt(sb, "F1", 8, MarginLeft, y, metaLine);
            y -= 16f;
            return y;
        }

        private static IReadOnlyList<string> FinalizePages(
            List<StringBuilder> pages,
            DateTime exportedAtUtc
        )
        {
            var exported = exportedAtUtc.ToString(
                "dd MMM yyyy",
                CultureInfo.InvariantCulture
            );
            for (var i = 0; i < pages.Count; i++)
            {
                var sb = pages[i];
                SetMuted(sb);
                TextAt(
                    sb,
                    "F1",
                    8,
                    MarginLeft,
                    20,
                    $"Exported {exported}"
                );
                TextAt(
                    sb,
                    "F1",
                    8,
                    PageWidth / 2 - 40,
                    20,
                    "Powered by Tummly"
                );
                TextAt(
                    sb,
                    "F1",
                    8,
                    PageWidth - MarginRight - 50,
                    20,
                    $"Page {i + 1}"
                );
            }

            return pages
                .Select(page =>
                    page.ToString()
                        .Replace("\r\n", "\n", StringComparison.Ordinal)
                )
                .ToList();
        }

        private static StringBuilder NewPage(List<StringBuilder> pages)
        {
            var sb = new StringBuilder(4096);
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

            return (NewPage(pages), TopY - 40f);
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
                TextAt(sb, "F1", 9, MarginLeft, y, line);
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

        private static void SetBlack(StringBuilder sb) => sb.Append("0 0 0 rg\n");

        private static void SetMuted(StringBuilder sb)
            => sb.Append("0.35 0.35 0.35 rg\n");

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
            => value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);

        private static int Metric(ReportsMetricDto? metric) => metric?.Value ?? 0;

        private static byte[] BuildPdf(IReadOnlyList<string> pageContents)
        {
            if (pageContents.Count == 0)
            {
                pageContents = [""];
            }

            var contentBytesList = pageContents
                .Select(content => Encoding.ASCII.GetBytes(content))
                .ToList();

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
                            + $"/MediaBox [0 0 {PageWidth:0} {PageHeight:0}] "
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
