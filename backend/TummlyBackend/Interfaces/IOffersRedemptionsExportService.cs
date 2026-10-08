using TummlyBackend.DTOs.Offers;

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

        /// <summary>Windowed redemption rows for PDF / CSV (REP-03).</summary>
        Task<IReadOnlyList<OfferDetailsRedemptionListItemDto>> ListRowsAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );
    }
}
