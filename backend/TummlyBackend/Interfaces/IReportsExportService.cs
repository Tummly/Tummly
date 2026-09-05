namespace TummlyBackend.Interfaces
{
    public sealed class ReportsExportFileResult
    {
        public required string FileName { get; init; }

        public required string ContentType { get; init; }

        public required byte[] Content { get; init; }
    }

    public interface IReportsExportService
    {
        Task<ReportsExportFileResult> ExportOverviewPdfAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );

        Task<ReportsExportFileResult> ExportCaptureCsvAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );

        Task<ReportsExportFileResult> ExportFeedbackCsvAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );

        Task<ReportsExportFileResult> ExportCampaignsCsvAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );
    }
}
