using TummlyBackend.Helpers;

namespace TummlyBackend.Models
{
    public sealed record AssistantAdvisoryReasonInput(
        string UserMessage,
        RestaurantContextSnapshot Snapshot,
        IReadOnlyList<AssistantLiveAnswerHistoryTurn>? History = null
    );

    public sealed record AssistantAdvisoryReasonRecommendation(
        string Action,
        string Headline,
        string Reason,
        IReadOnlyList<string> EvidenceRef,
        string Confidence
    );

    public sealed record AssistantAdvisoryReasonOutput(
        string AnswerType,
        string Summary,
        string? ClarifyingQuestion,
        IReadOnlyList<AssistantAdvisoryReasonRecommendation> Recommendations,
        IReadOnlyList<string> EvidenceUsed
    );

    public abstract record AssistantAdvisoryReasonResult
    {
        private AssistantAdvisoryReasonResult()
        {
        }

        public sealed record Succeeded(AssistantAdvisoryReasonOutput Output)
            : AssistantAdvisoryReasonResult;

        public sealed record Failed(bool Retryable = true)
            : AssistantAdvisoryReasonResult;
    }

    public abstract record AdvisoryReasonValidateResult
    {
        private AdvisoryReasonValidateResult()
        {
        }

        public sealed record Valid(AssistantAdvisoryReasonOutput Output)
            : AdvisoryReasonValidateResult;

        public sealed record FallbackNoClearDriver : AdvisoryReasonValidateResult;

        public sealed record Clarify(AdvisoryGap Gap) : AdvisoryReasonValidateResult;
    }
}
