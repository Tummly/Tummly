namespace TummlyBackend.Interfaces
{
    public interface IPrivacyConsentPermissionRecordsExportService
    {
        Task<ReportsExportFileResult> ExportCsvAsync(
            int locationId,
            CancellationToken cancellationToken = default
        );

        Task<ReportsExportFileResult> ExportXlsxAsync(
            IReadOnlyList<int> locationIds,
            CancellationToken cancellationToken = default
        );

        /// <summary>Current Permission records rows for PDF / CSV (REP-03).</summary>
        Task<IReadOnlyList<string[]>> ListRowsAsync(
            int locationId,
            CancellationToken cancellationToken = default
        );
    }
}
