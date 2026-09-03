using TummlyBackend.DTOs.Reports;

namespace TummlyBackend.Interfaces
{
    public interface IReportsOverviewService
    {
        Task<ReportsOverviewDto> GetOverviewAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );
    }
}
