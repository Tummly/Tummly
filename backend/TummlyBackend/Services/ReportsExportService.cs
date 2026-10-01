using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Sync Reports export pack — PDF/CSV retained; styled XLSX additive.
    /// Soft-lock gate lives on the controller (paid-write).
    /// </summary>
    public sealed class ReportsExportService : IReportsExportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IReportsOverviewService _overview;
        private readonly IReportsCaptureService _capture;
        private readonly IReportsFeedbackService _feedback;
        private readonly IReportsCampaignsService _campaigns;

        public ReportsExportService(
            ApplicationDbContext context,
            IReportsOverviewService overview,
            IReportsCaptureService capture,
            IReportsFeedbackService feedback,
            IReportsCampaignsService campaigns
        )
        {
            _context = context;
            _overview = overview;
            _capture = capture;
            _feedback = feedback;
            _campaigns = campaigns;
        }

        public async Task<ReportsExportFileResult> ExportOverviewPdfAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            var dto = await _overview.GetOverviewAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            var locationName = await ResolveLocationNameAsync(
                locationId,
                cancellationToken
            );
            var (content, fileName) = ReportsExportPackWriter.RenderOverviewPdf(
                dto,
                locationName,
                locationId,
                fromUtc,
                toUtc,
                DateTime.UtcNow
            );
            return new ReportsExportFileResult
            {
                FileName = fileName,
                ContentType = ReportsExportPackWriter.PdfContentType,
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
                    DTOs.Reports.ReportsOverviewDto Dto
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
            var (content, fileName) = ReportsExportPackWriter.RenderCaptureCsv(
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
                    DTOs.Reports.ReportsCaptureDto Dto
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
            var dto = await _feedback.GetFeedbackReportAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            var (content, fileName) = ReportsExportPackWriter.RenderFeedbackCsv(
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
            var rows =
                new List<(
                    ReportsStyledXlsxPack.LocationContext Location,
                    DTOs.Reports.ReportsFeedbackDto Dto
                )>();
            foreach (var location in contexts)
            {
                var dto = await _feedback.GetFeedbackReportAsync(
                    location.LocationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
                rows.Add((location, dto));
            }

            var (content, fileName) = ReportsStyledXlsxPack.RenderFeedbackXlsx(
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
                    DTOs.Reports.ReportsCampaignsDto Dto
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
                    RestaurantName = row.Restaurant!.Name,
                })
                .ToListAsync(cancellationToken);

            var byId = rows.ToDictionary(row => row.Id);
            var ordered = new List<ReportsStyledXlsxPack.LocationContext>();
            foreach (var id in distinct)
            {
                if (!byId.TryGetValue(id, out var row))
                {
                    throw new KeyNotFoundException(
                        $"Location {id} was not found."
                    );
                }

                ordered.Add(
                    new ReportsStyledXlsxPack.LocationContext(
                        row.Id,
                        row.LocationName,
                        row.RestaurantName
                    )
                );
            }

            return ordered;
        }
    }
}
