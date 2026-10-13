using System.Text.Json;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class AssistantMessagePresentationJson
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        public static string? Serialize(AssistantMessagePresentation? presentation)
        {
            if (presentation is null)
            {
                return null;
            }

            if (presentation.Chart is null
                && presentation.RecommendedNextStep is null
                && (presentation.Metrics is null || presentation.Metrics.Count == 0)
                && string.IsNullOrWhiteSpace(presentation.ShortNote))
            {
                return null;
            }

            return JsonSerializer.Serialize(ToDto(presentation), JsonOptions);
        }

        public static AssistantMessagePresentationDto? ParseDto(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<AssistantMessagePresentationDto>(
                    json,
                    JsonOptions
                );
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static AssistantMessagePresentationDto ToDto(
            AssistantMessagePresentation presentation
        )
            => new()
            {
                Chart = presentation.Chart is null
                    ? null
                    : new AssistantAnswerChartDto
                    {
                        Title = presentation.Chart.Title,
                        Kind = presentation.Chart.Kind,
                        XLabel = presentation.Chart.XLabel,
                        Series = presentation.Chart.Series
                            .Select(point => new AssistantAnswerChartPointDto
                            {
                                Label = point.Label,
                                Value = point.Value,
                            })
                            .ToList(),
                    },
                RecommendedNextStep = presentation.RecommendedNextStep is null
                    ? null
                    : new AssistantRecommendedNextStepDto
                    {
                        Text = presentation.RecommendedNextStep.Text,
                        PrimaryAction =
                            presentation.RecommendedNextStep.PrimaryAction,
                        SecondaryAction =
                            presentation.RecommendedNextStep.SecondaryAction,
                    },
                Metrics = presentation.Metrics is null || presentation.Metrics.Count == 0
                    ? null
                    : presentation.Metrics
                        .Select(metric => new AssistantAnswerMetricDto
                        {
                            Label = metric.Label,
                            Value = metric.Value,
                        })
                        .ToList(),
                ShortNote = string.IsNullOrWhiteSpace(presentation.ShortNote)
                    ? null
                    : presentation.ShortNote.Trim(),
            };
    }
}
