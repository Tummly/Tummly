using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IAssistantCaptureRetrieve
    {
        Task<AssistantCaptureRetrieveResult> RetrieveAsync(
            int ownedLocationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );
    }

    public abstract record AssistantCaptureRetrieveResult
    {
        public sealed record Ok(AssistantCaptureEvidence Evidence)
            : AssistantCaptureRetrieveResult;

        public sealed record Failed() : AssistantCaptureRetrieveResult;
    }
}
