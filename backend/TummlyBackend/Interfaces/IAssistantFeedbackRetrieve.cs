using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IAssistantFeedbackRetrieve
    {
        Task<AssistantFeedbackRetrieveResult> RetrieveAsync(
            int ownedLocationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );
    }

    public abstract record AssistantFeedbackRetrieveResult
    {
        public sealed record Ok(AssistantFeedbackEvidence Evidence)
            : AssistantFeedbackRetrieveResult;

        public sealed record Failed() : AssistantFeedbackRetrieveResult;
    }
}
