using TummlyBackend.DTOs.Reports;

namespace TummlyBackend.Interfaces
{
    public interface IReportsCampaignsService
    {
        Task<ReportsCampaignsDto> GetCampaignsAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );
    }
}
