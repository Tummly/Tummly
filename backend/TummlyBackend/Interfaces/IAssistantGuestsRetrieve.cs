using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IAssistantGuestsRetrieve
    {
        Task<AssistantGuestsRetrieveResult> RetrieveAsync(
            int ownedLocationId,
            CancellationToken cancellationToken = default
        );
    }

    public abstract record AssistantGuestsRetrieveResult
    {
        public sealed record Ok(AssistantGuestsEvidence Evidence)
            : AssistantGuestsRetrieveResult;

        public sealed record Failed() : AssistantGuestsRetrieveResult;
    }
}
