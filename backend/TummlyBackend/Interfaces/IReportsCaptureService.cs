using TummlyBackend.DTOs.Reports;

namespace TummlyBackend.Interfaces
{
    public interface IReportsCaptureService
    {
        Task<ReportsCaptureDto> GetCaptureAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );
    }
}
