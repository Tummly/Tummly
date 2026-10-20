using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IBulkRecoveryBatchService
    {
        Task<BulkRecoveryBatchDto?> CreateDraftAsync(
            int restaurantId,
            int locationId,
            int createdByUserId,
            IReadOnlyList<int> feedbackIds,
            FeedbackGuestResponseChannel channel,
            FeedbackRecoveryIntent intent,
            string? subject,
            string bodyTemplate,
            string? purpose,
            string? tone,
            string? includeNotes,
            CancellationToken cancellationToken = default
        );

        Task<BulkRecoveryBatchDto?> GetAsync(
            int batchId,
            CancellationToken cancellationToken = default
        );

        Task<BulkRecoveryBatchDto?> UpdateDraftAsync(
            int batchId,
            string? subject,
            string? bodyTemplate,
            string? purpose,
            string? tone,
            string? includeNotes,
            CancellationToken cancellationToken = default
        );

        Task<SendBulkRecoveryBatchResultDto?> SendAsync(
            int batchId,
            int authorUserId,
            CancellationToken cancellationToken = default
        );
    }
}
