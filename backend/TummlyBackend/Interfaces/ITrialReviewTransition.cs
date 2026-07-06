using TummlyBackend.DTOs.Admin;

namespace TummlyBackend.Interfaces
{
    public interface ITrialReviewTransition
    {
        Task<TrialReviewResult> ApplyTransitionAsync(
            int trialRequestId,
            TrialReviewDecision decision,
            TrialReviewContext context,
            CancellationToken cancellationToken = default
        );
    }
}
