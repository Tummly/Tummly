namespace TummlyBackend.DTOs.Assistant
{
    public class AssistantAnswerChartPointDto
    {
        public string Label { get; set; } = string.Empty;

        public int Value { get; set; }
    }

    public class AssistantAnswerChartDto
    {
        public string Title { get; set; } = string.Empty;

        public string Kind { get; set; } = "bar";

        public string? XLabel { get; set; }

        public IReadOnlyList<AssistantAnswerChartPointDto> Series { get; set; }
            = Array.Empty<AssistantAnswerChartPointDto>();
    }

    public class AssistantRecommendedNextStepDto
    {
        public string Text { get; set; } = string.Empty;

        public AssistantActionDto? PrimaryAction { get; set; }

        public AssistantActionDto? SecondaryAction { get; set; }
    }

    public class AssistantAnswerMetricDto
    {
        public string Label { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }

    public class AssistantDraftPreviewDto
    {
        public string Kind { get; set; } = string.Empty;

        public string StatusLabel { get; set; } = "Draft · Not sent";

        public int? CampaignId { get; set; }

        public string? Name { get; set; }

        public string? AudienceLabel { get; set; }

        public string? Channel { get; set; }

        public string? Subject { get; set; }

        public string? MessageBody { get; set; }

        public string? TimingLabel { get; set; }

        public int? OfferId { get; set; }

        public string? Title { get; set; }

        public string? ConditionsSummary { get; set; }

        public string? ValidityLabel { get; set; }

        public int? FeedbackId { get; set; }

        public string? Intent { get; set; }

        public string? Purpose { get; set; }
    }

    public class AssistantMessagePresentationDto
    {
        public AssistantAnswerChartDto? Chart { get; set; }

        public AssistantRecommendedNextStepDto? RecommendedNextStep { get; set; }

        public IReadOnlyList<AssistantAnswerMetricDto>? Metrics { get; set; }

        public string? ShortNote { get; set; }

        public AssistantDraftPreviewDto? DraftPreview { get; set; }
    }
}
