using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IAssistantRetrieveToolHost
    {
        Task<IReadOnlyList<AssistantToolCallResult>> ExecuteBatchAsync(
            AssistantRetrieveToolContext context,
            IReadOnlyList<AssistantToolCallRequest> calls,
            CancellationToken cancellationToken = default
        );
    }
}
