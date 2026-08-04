using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IFeedbackDetectedTagsService
    {
        /// <summary>
        /// Replaces Detected Tags on Succeeded Feedback, or promotes Failed →
        /// Succeeded with required sentiment. Returns null when Feedback is
        /// missing. Throws ArgumentException for validation failures.
        /// Throws <see cref="FeedbackDetectedTagsConflictException"/> when
        /// ClassificationStatus is Pending (or otherwise not Succeeded/Failed).
        /// </summary>
        Task<UpdateFeedbackDetectedTagsResultDto?> UpdateAsync(
            int feedbackId,
            int authorUserId,
            IReadOnlyList<DetectedTag> detectedTags,
            FeedbackSentiment? sentiment,
            bool sentimentProvided,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<FeedbackDetectedTagsChangeItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        );
    }
}
