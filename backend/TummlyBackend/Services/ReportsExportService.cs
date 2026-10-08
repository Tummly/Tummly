using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.DTOs.Reports;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Sync Reports export pack — templated PDF + CSV/XLSX (REP-02 / REP-03).
    /// Soft-lock gate lives on the controller (paid-write).
    /// </summary>
    public sealed class ReportsExportService : IReportsExportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IReportsOverviewService _overview;
        private readonly IReportsCaptureService _capture;
        private readonly IReportsCampaignsService _campaigns;
        private readonly IOffersRedemptionsExportService _redemptions;
        private readonly IPrivacyConsentPermissionRecordsExportService _consent;

        public ReportsExportService(
            ApplicationDbContext context,
            IReportsOverviewService overview,
            IReportsCaptureService capture,
            IReportsCampaignsService campaigns,
            IOffersRedemptionsExportService redemptions,
            IPrivacyConsentPermissionRecordsExportService consent
        )
        {
            _context = context;
            _overview = overview;
            _capture = capture;
            _campaigns = campaigns;
            _redemptions = redemptions;
            _consent = consent;
        }

        public async Task<ReportsExportFileResult> ExportOverviewPdfAsync(
            IReadOnlyList<int> locationIds,
            DateTime fromUtc,
            DateTime toUtc,
            bool includeOfferRedemptions,
            bool includeGuestConsent,
            CancellationToken cancellationToken = default
        )
        {
            var locations = await ResolveLocationDetailsAsync(
                locationIds,
                cancellationToken
            );
            var period = ReportsStyledXlsxWriter.FormatPeriodLabel(
                fromUtc,
                toUtc
            );
            var utcNow = DateTime.UtcNow;
            var bundles = new List<ReportsTemplatedPdfWriter.LocationBundle>();

            foreach (var location in locations)
            {
                var overview = await _overview.GetOverviewAsync(
                    location.LocationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
                var capture = await _capture.GetCaptureAsync(
                    location.LocationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
                var feedbackRows = await ReportsExportFeedbackRows.LoadAsync(
                    _context,
                    location.LocationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
                var campaigns = await _campaigns.GetCampaignsAsync(
                    location.LocationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );

                IReadOnlyList<OfferDetailsRedemptionListItemDto> redemptions =
                    [];
                if (includeOfferRedemptions)
                {
                    redemptions = await _redemptions.ListRowsAsync(
                        location.LocationId,
                        fromUtc,
                        toUtc,
                        cancellationToken
                    );
                }

                IReadOnlyList<string[]> consentRows = [];
                if (includeGuestConsent)
                {
                    consentRows = await _consent.ListRowsAsync(
                        location.LocationId,
                        cancellationToken
                    );
                }

                bundles.Add(
                    new ReportsTemplatedPdfWriter.LocationBundle(
                        location.LocationId,
                        location.LocationName,
                        location.City,
                        overview,
                        capture,
                        feedbackRows,
                        campaigns,
                        redemptions,
                        consentRows
                    )
                );
            }

            var primary = bundles[0].Overview;
            var document = new ReportsTemplatedPdfWriter.Document(
                RestaurantName: locations[0].RestaurantName,
                PeriodLabel: period,
                ExportedAtUtc: utcNow,
                ExecutiveSummary:
                    ReportsTemplatedPdfWriter.BuildExecutiveSummary(primary),
                RecommendedNextAction:
                    ReportsTemplatedPdfWriter.BuildRecommendedNextAction(
                        primary
                    ),
                PortfolioNote: bundles.Count > 1
                    ? ReportsTemplatedPdfWriter.BuildPortfolioNote(bundles)
                    : null,
                Locations: bundles,
                IncludeOfferRedemptions: includeOfferRedemptions,
                IncludeGuestConsent: includeGuestConsent
            );

            var (content, fileName) = ReportsTemplatedPdfWriter.Render(document);
            return new ReportsExportFileResult
            {
                FileName = fileName,
                ContentType = ReportsTemplatedPdfWriter.ContentType,
                Content = content,
            };
        }

        public async Task<ReportsExportFileResult> ExportOverviewXlsxAsync(
            IReadOnlyList<int> locationIds,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            var contexts = await ResolveLocationsAsync(
                locationIds,
                cancellationToken
            );
            var rows =
                new List<(
                    ReportsStyledXlsxPack.LocationContext Location,
                    ReportsOverviewDto Dto
                )>();
            foreach (var location in contexts)
            {
                var dto = await _overview.GetOverviewAsync(
                    location.LocationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
                rows.Add((location, dto));
            }

            var (content, fileName) = ReportsStyledXlsxPack.RenderOverviewXlsx(
                rows,
                fromUtc,
                toUtc,
                DateTime.UtcNow
            );
            return new ReportsExportFileResult
            {
                FileName = fileName,
                ContentType = ReportsStyledXlsxWriter.ContentType,
                Content = content,
            };
        }

        public async Task<ReportsExportFileResult> ExportCaptureCsvAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            var dto = await _capture.GetCaptureAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            var locationName = await ResolveLocationNameAsync(
                locationId,
                cancellationToken
            );
            var (content, fileName) = ReportsExportPackWriter.RenderCaptureCsv(
                dto,
                locationId,
                locationName,
                DateTime.UtcNow
            );
            return new ReportsExportFileResult
            {
                FileName = fileName,
                ContentType = ReportsExportPackWriter.CsvContentType,
                Content = content,
            };
        }

        public async Task<ReportsExportFileResult> ExportCaptureXlsxAsync(
            IReadOnlyList<int> locationIds,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            var contexts = await ResolveLocationsAsync(
                locationIds,
                cancellationToken
            );
            var rows =
                new List<(
                    ReportsStyledXlsxPack.LocationContext Location,
                    ReportsCaptureDto Dto
                )>();
            foreach (var location in contexts)
            {
                var dto = await _capture.GetCaptureAsync(
                    location.LocationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
                rows.Add((location, dto));
            }

            var (content, fileName) = ReportsStyledXlsxPack.RenderCaptureXlsx(
                rows,
                fromUtc,
                toUtc,
                DateTime.UtcNow
            );
            return new ReportsExportFileResult
            {
                FileName = fileName,
                ContentType = ReportsStyledXlsxWriter.ContentType,
                Content = content,
            };
        }

        public async Task<ReportsExportFileResult> ExportFeedbackCsvAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            var rows = await ReportsExportFeedbackRows.LoadAsync(
                _context,
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            var (content, fileName) =
                ReportsExportPackWriter.RenderFeedbackRowsCsv(
                    rows,
                    locationId,
                    DateTime.UtcNow
                );
            return new ReportsExportFileResult
            {
                FileName = fileName,
                ContentType = ReportsExportPackWriter.CsvContentType,
                Content = content,
            };
        }

        public async Task<ReportsExportFileResult> ExportFeedbackXlsxAsync(
            IReadOnlyList<int> locationIds,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            var contexts = await ResolveLocationsAsync(
                locationIds,
                cancellationToken
            );
            var sheets =
                new List<(
                    ReportsStyledXlsxPack.LocationContext Location,
                    IReadOnlyList<ReportsExportFeedbackRowDto> Rows
                )>();
            foreach (var location in contexts)
            {
                var rows = await ReportsExportFeedbackRows.LoadAsync(
                    _context,
                    location.LocationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
                sheets.Add((location, rows));
            }

            var (content, fileName) =
                ReportsStyledXlsxPack.RenderFeedbackRowsXlsx(
                    sheets,
                    fromUtc,
                    toUtc,
                    DateTime.UtcNow
                );
            return new ReportsExportFileResult
            {
                FileName = fileName,
                ContentType = ReportsStyledXlsxWriter.ContentType,
                Content = content,
            };
        }

        public async Task<ReportsExportFileResult> ExportCampaignsCsvAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            var dto = await _campaigns.GetCampaignsAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            var (content, fileName) =
                ReportsExportPackWriter.RenderCampaignsCsv(
                    dto,
                    locationId,
                    DateTime.UtcNow
                );
            return new ReportsExportFileResult
            {
                FileName = fileName,
                ContentType = ReportsExportPackWriter.CsvContentType,
                Content = content,
            };
        }

        public async Task<ReportsExportFileResult> ExportCampaignsXlsxAsync(
            IReadOnlyList<int> locationIds,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            var contexts = await ResolveLocationsAsync(
                locationIds,
                cancellationToken
            );
            var rows =
                new List<(
                    ReportsStyledXlsxPack.LocationContext Location,
                    ReportsCampaignsDto Dto
                )>();
            foreach (var location in contexts)
            {
                var dto = await _campaigns.GetCampaignsAsync(
                    location.LocationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
                rows.Add((location, dto));
            }

            var (content, fileName) = ReportsStyledXlsxPack.RenderCampaignsXlsx(
                rows,
                fromUtc,
                toUtc,
                DateTime.UtcNow
            );
            return new ReportsExportFileResult
            {
                FileName = fileName,
                ContentType = ReportsStyledXlsxWriter.ContentType,
                Content = content,
            };
        }

        private async Task<string> ResolveLocationNameAsync(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            return await _context.RestaurantLocations
                    .AsNoTracking()
                    .Where(row => row.Id == locationId)
                    .Select(row => row.LocationName)
                    .FirstOrDefaultAsync(cancellationToken)
                ?? "Location";
        }

        private async Task<
            IReadOnlyList<ReportsStyledXlsxPack.LocationContext>
        > ResolveLocationsAsync(
            IReadOnlyList<int> locationIds,
            CancellationToken cancellationToken
        )
        {
            var details = await ResolveLocationDetailsAsync(
                locationIds,
                cancellationToken
            );
            return details
                .Select(row => new ReportsStyledXlsxPack.LocationContext(
                    row.LocationId,
                    row.LocationName,
                    row.RestaurantName
                ))
                .ToList();
        }

        private async Task<
            IReadOnlyList<(
                int LocationId,
                string LocationName,
                string City,
                string RestaurantName
            )>
        > ResolveLocationDetailsAsync(
            IReadOnlyList<int> locationIds,
            CancellationToken cancellationToken
        )
        {
            if (locationIds.Count == 0)
            {
                throw new ArgumentException(
                    "At least one locationId is required.",
                    nameof(locationIds)
                );
            }

            var distinct = locationIds.Distinct().ToList();
            var rows = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => distinct.Contains(row.Id))
                .Select(row => new
                {
                    row.Id,
                    row.LocationName,
                    row.City,
                    RestaurantName = row.Restaurant!.Name,
                })
                .ToListAsync(cancellationToken);

            var byId = rows.ToDictionary(row => row.Id);
            var ordered =
                new List<(
                    int LocationId,
                    string LocationName,
                    string City,
                    string RestaurantName
                )>();
            foreach (var id in distinct)
            {
                if (!byId.TryGetValue(id, out var row))
                {
                    throw new KeyNotFoundException(
                        $"Location {id} was not found."
                    );
                }

                ordered.Add(
                    (
                        row.Id,
                        row.LocationName,
                        string.IsNullOrWhiteSpace(row.City)
                            ? string.Empty
                            : row.City.Trim(),
                        row.RestaurantName
                    )
                );
            }

            return ordered;
        }
    }
}
