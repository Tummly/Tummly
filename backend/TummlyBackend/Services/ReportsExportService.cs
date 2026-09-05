using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Sync Reports export pack (ticket 17) — reuses KPI aggregates; Soft-lock
    /// gate lives on the controller (paid-write).
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
    }
}
