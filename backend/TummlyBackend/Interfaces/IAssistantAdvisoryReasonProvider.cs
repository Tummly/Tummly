using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IAssistantAdvisoryReasonProvider
    {
        Task<AssistantAdvisoryReasonResult> CompleteAsync(
            AssistantAdvisoryReasonInput input,
            CancellationToken cancellationToken = default
        );
    }
}
