using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IAssistantLiveAnswerProvider
    {
        Task<AssistantLiveAnswerResult> CompleteAsync(
            AssistantLiveAnswerInput input,
            CancellationToken cancellationToken = default
        );
    }
}
