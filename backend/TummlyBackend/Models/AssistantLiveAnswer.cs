using TummlyBackend.DTOs.Assistant;

namespace TummlyBackend.Models
{
    public sealed record AssistantCompareLocationEvidence(
        int OwnedLocationId,
        string LocationName,
        CaptureLocationStatus CaptureStatus,
        AssistantFeedbackEvidence Evidence
    );

    public sealed record AssistantLiveAnswerInput(
        string UserMessage,
        string OwnedLocationName,
        string PeriodPhrase,
        AssistantFeedbackEvidence Evidence,
        IReadOnlyList<AssistantCompareLocationEvidence>? CompareLocations = null,
        string? Caveat = null,
        string? DroppedUnknownSentence = null
    );

    public abstract record AssistantLiveAnswerResult
    {
        public sealed record Succeeded(
            AssistantMessageClass Class,
            string? Title,
            string Body,
            IReadOnlyList<AssistantActionDto> Actions
        ) : AssistantLiveAnswerResult;

        public sealed record Failed(bool Retryable) : AssistantLiveAnswerResult;
    }
}
