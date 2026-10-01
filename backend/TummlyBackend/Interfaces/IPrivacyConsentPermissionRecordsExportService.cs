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
    }
}
