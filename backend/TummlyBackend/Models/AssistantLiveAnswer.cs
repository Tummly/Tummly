namespace TummlyBackend.Models
{
    public sealed record AssistantLiveAnswerInput(
        string UserMessage,
        string OwnedLocationName,
        string PeriodPhrase
    );

    public abstract record AssistantLiveAnswerResult
    {
        public sealed record Succeeded(
            AssistantMessageClass Class,
            string? Title,
            string Body
        ) : AssistantLiveAnswerResult;

        public sealed record Failed(bool Retryable) : AssistantLiveAnswerResult;
    }
}
