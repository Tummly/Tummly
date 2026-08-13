using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IAssistantOffersRetrieve
    {
        Task<AssistantOffersRetrieveResult> RetrieveAsync(
            int ownedLocationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );
    }

    public abstract record AssistantOffersRetrieveResult
    {
        public sealed record Ok(AssistantOffersEvidence Evidence)
            : AssistantOffersRetrieveResult;

        public sealed record Failed() : AssistantOffersRetrieveResult;
    }
}
