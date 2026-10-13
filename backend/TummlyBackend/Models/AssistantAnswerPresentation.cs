using TummlyBackend.DTOs.Assistant;

namespace TummlyBackend.Models
{
    public sealed record AssistantAnswerChartPoint(string Label, int Value);

    public sealed record AssistantAnswerChart(
        string Title,
        string Kind,
        string? XLabel,
        IReadOnlyList<AssistantAnswerChartPoint> Series
    );

    public sealed record AssistantRecommendedNextStep(
        string Text,
        AssistantActionDto? PrimaryAction,
        AssistantActionDto? SecondaryAction
    );

    /// <summary>Compact metric chip for side-panel presentation.</summary>
    public sealed record AssistantAnswerMetric(string Label, string Value);

    public sealed record AssistantMessagePresentation(
        AssistantAnswerChart? Chart,
        AssistantRecommendedNextStep? RecommendedNextStep,
        IReadOnlyList<AssistantAnswerMetric>? Metrics = null,
        string? ShortNote = null
    );
}
