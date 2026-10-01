namespace TummlyBackend.Interfaces
{
    public interface IOffersRedemptionsExportService
    {
        Task<ReportsExportFileResult> ExportCsvAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );

        Task<ReportsExportFileResult> ExportXlsxAsync(
            IReadOnlyList<int> locationIds,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );
    }
}
