using TummlyBackend.DTOs.Assistant;

namespace TummlyBackend.Models
{
    public sealed record AssistantCompareLocationEvidence(
        int OwnedLocationId,
        string LocationName,
        CaptureLocationStatus CaptureStatus,
        AssistantRetrievedEvidence Evidence
    );

    public sealed record AssistantLiveAnswerInput(
        string UserMessage,
        string OwnedLocationName,
        string PeriodPhrase,
        AssistantRetrievedEvidence Evidence,
        IReadOnlyList<AssistantCompareLocationEvidence>? CompareLocations = null,
        string? Caveat = null,
        string? DroppedUnknownSentence = null,
        bool SuppressMixedRefusal = false
    );

    public abstract record AssistantLiveAnswerResult
    {
        public sealed record Succeeded(
            AssistantMessageClass Class,
            string? Title,
            string Body,
            IReadOnlyList<AssistantActionDto> Actions,
            string AssistantTask = Helpers.AssistantTask.Retrieve
        ) : AssistantLiveAnswerResult;

        public sealed record Failed(bool Retryable) : AssistantLiveAnswerResult;
    }
}
