using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IAssistantHomeKpiRetrieve
    {
        Task<AssistantHomeKpiRetrieveResult> RetrieveAsync(
            int ownedLocationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );
    }

    public abstract record AssistantHomeKpiRetrieveResult
    {
        public sealed record Ok(AssistantHomeKpiEvidence Evidence)
            : AssistantHomeKpiRetrieveResult;

        public sealed record Failed() : AssistantHomeKpiRetrieveResult;
    }
}
