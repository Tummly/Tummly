namespace TummlyBackend.Interfaces
{
    public interface IPrivacyConsentPermissionRecordsExportService
    {
        Task<ReportsExportFileResult> ExportCsvAsync(
            int locationId,
            CancellationToken cancellationToken = default
        );
    }
}
