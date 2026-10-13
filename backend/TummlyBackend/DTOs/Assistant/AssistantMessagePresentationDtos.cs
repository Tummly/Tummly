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

    public class AssistantMessagePresentationDto
    {
        public AssistantAnswerChartDto? Chart { get; set; }

        public AssistantRecommendedNextStepDto? RecommendedNextStep { get; set; }

        public IReadOnlyList<AssistantAnswerMetricDto>? Metrics { get; set; }

        public string? ShortNote { get; set; }
    }
}
