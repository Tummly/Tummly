using TummlyBackend.DTOs.Feedback;

namespace TummlyBackend.Interfaces
{
    public interface IFeedbackInboxListService
    {
        Task<FeedbackInboxListResponse> ListAsync(
            FeedbackInboxListQuery query,
            CancellationToken cancellationToken = default
        );
    }
}
