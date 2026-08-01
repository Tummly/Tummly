using TummlyBackend.DTOs.Feedback;

namespace TummlyBackend.Interfaces
{
    public interface IFeedbackInboxListService
    {
        Task<FeedbackInboxListResponse> ListAsync(
            FeedbackInboxListQuery query,
            CancellationToken cancellationToken = default
        );

        Task<FeedbackExportResult> ExportAsync(
            FeedbackExportQuery query,
            CancellationToken cancellationToken = default
        );
    }
}
