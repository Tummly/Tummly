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

    /// <summary>
    /// In-chat Draft preview card. Kind is campaign | offer | recovery.
    /// </summary>
    public sealed record AssistantDraftPreview(
        string Kind,
        string StatusLabel,
        int? CampaignId = null,
        string? Name = null,
        string? AudienceLabel = null,
        string? Channel = null,
        string? Subject = null,
        string? MessageBody = null,
        string? TimingLabel = null,
        int? OfferId = null,
        string? Title = null,
        string? ConditionsSummary = null,
        string? ValidityLabel = null,
        int? FeedbackId = null,
        string? Intent = null,
        string? Purpose = null
    );

    public sealed record AssistantMessagePresentation(
        AssistantAnswerChart? Chart,
        AssistantRecommendedNextStep? RecommendedNextStep,
        IReadOnlyList<AssistantAnswerMetric>? Metrics = null,
        string? ShortNote = null,
        AssistantDraftPreview? DraftPreview = null
    );
}
