using TummlyBackend.DTOs.Reports;

namespace TummlyBackend.Interfaces
{
    public interface IReportsFeedbackService
    {
        Task<ReportsFeedbackDto> GetFeedbackReportAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );
    }
}
