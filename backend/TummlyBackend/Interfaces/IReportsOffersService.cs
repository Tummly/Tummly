using TummlyBackend.DTOs.Reports;

namespace TummlyBackend.Interfaces
{
    public interface IReportsOffersService
    {
        Task<ReportsOffersDto> GetOffersReportAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );
    }
}
