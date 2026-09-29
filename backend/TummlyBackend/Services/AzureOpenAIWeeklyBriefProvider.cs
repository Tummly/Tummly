using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Production Weekly brief provider: Azure OpenAI Structured Outputs.
    /// Weekly brief via one-wave surface read tools.
    /// Reuses FeedbackClassification Endpoint/ApiKey/Deployment settings.
    /// </summary>
    public sealed class AzureOpenAIWeeklyBriefProvider : IWeeklyBriefProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly FeedbackClassificationSettings _settings;
        private readonly ILogger<AzureOpenAIWeeklyBriefProvider> _logger;

        public AzureOpenAIWeeklyBriefProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<FeedbackClassificationSettings> settings,
            ILogger<AzureOpenAIWeeklyBriefProvider> logger
        )
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<WeeklyBriefProviderResult> GenerateAsync(
            WeeklyBriefProviderInput input,
            CancellationToken cancellationToken = default
        )
        {
            var maxAttempts = Math.Max(1, _settings.MaxAttempts);

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var attemptResult = await AttemptGenerateWithToolsAsync(input, cancellationToken);

                    if (attemptResult.Kind == AttemptKind.Succeeded)
                    {
                        return attemptResult.Result
                            ?? new WeeklyBriefProviderResult.Failed(Retryable: true);
                    }

                    if (attemptResult.Kind == AttemptKind.Failed)
                    {
                        return attemptResult.Result
                            ?? new WeeklyBriefProviderResult.Failed(Retryable: true);
                    }

                    if (attempt >= maxAttempts)
                    {
                        break;
                    }

                    await DelayBackoffAsync(attempt, cancellationToken);
                }
                catch (OperationCanceledException) when (
                    cancellationToken.IsCancellationRequested
                )
                {
                    throw;
                }
                catch (Exception ex) when (AzureOpenAIChatCompletions.IsTransientException(ex))
                {
                    _logger.LogWarning(
                        ex,
                        "Transient weekly brief failure (attempt {Attempt}/{MaxAttempts})",
                        attempt,
                        maxAttempts
                    );

                    if (attempt >= maxAttempts)
                    {
                        break;
                    }

                    await DelayBackoffAsync(attempt, cancellationToken);
                }
            }

            return new WeeklyBriefProviderResult.Failed(Retryable: true);
        }

        private async Task<AttemptResult> AttemptGenerateWithToolsAsync(
            WeeklyBriefProviderInput input,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(_settings.Endpoint)
                || string.IsNullOrWhiteSpace(_settings.ApiKey)
                || string.IsNullOrWhiteSpace(_settings.DeploymentName))
            {
                _logger.LogError(
                    "Azure OpenAI weekly brief is misconfigured (endpoint, api key, or deployment)."
                );
                return AttemptResult.Failed(
                    new WeeklyBriefProviderResult.Failed(Retryable: true)
                );
            }

            var client = _httpClientFactory.CreateClient(
                WeeklyBriefStructuredOutput.HttpClientName
            );
            var forced = new List<AssistantToolCallRequest>
            {
                new(
                    "forced_metrics",
                    SurfaceReadToolCatalog.ReadWeeklyBriefMetrics,
                    "{}"
                ),
            };

            var loop = await AzureOpenAIOneWaveToolLoop.RunAsync(
                new AzureOpenAIOneWaveRequest
                {
                    SurfaceName = "weekly brief",
                    HttpClient = client,
                    Endpoint = _settings.Endpoint,
                    ApiKey = _settings.ApiKey,
                    DeploymentName = _settings.DeploymentName,
                    ApiVersion = _settings.ApiVersion,
                    SeedMessages = WeeklyBriefStructuredOutput.BuildToolSeedMessages(
                        input,
                        _settings.PromptSchemaVersion
                    ),
                    ToolsArray = SurfaceReadToolCatalog.WeeklyBriefTools(),
                    SchemaName = WeeklyBriefStructuredOutput.SchemaName,
                    FinalSchema = WeeklyBriefStructuredOutput.BuildSchema(),
                    ExecuteTools = (calls, _) => Task.FromResult(
                        SurfaceReadToolHost.ExecuteWeeklyBrief(calls, input)
                    ),
                    ForcedToolCalls = forced,
                    Logger = _logger,
                },
                cancellationToken
            );

            return MapLoopResult(loop);
        }

        private AttemptResult MapLoopResult(AzureOpenAIOneWaveResult loop)
            => loop switch
            {
                AzureOpenAIOneWaveResult.Succeeded succeeded =>
                    WeeklyBriefStructuredOutput.TryParseModelContent(
                        succeeded.Content,
                        out var output,
                        out var enrichment,
                        out var invalidOutput
                    )
                        ? AttemptResult.Succeeded(
                            new WeeklyBriefProviderResult.Succeeded(output!, enrichment)
                        )
                        : invalidOutput
                            ? AttemptResult.InvalidOutput()
                            : AttemptResult.Failed(
                                new WeeklyBriefProviderResult.Failed(Retryable: true)
                            ),
                AzureOpenAIOneWaveResult.Transient => AttemptResult.Transient(),
                AzureOpenAIOneWaveResult.InvalidOutput => AttemptResult.InvalidOutput(),
                _ => AttemptResult.Failed(
                    new WeeklyBriefProviderResult.Failed(Retryable: true)
                ),
            };

        private async Task DelayBackoffAsync(
            int attempt,
            CancellationToken cancellationToken
        )
        {
            var delayMs = Math.Max(0, _settings.InitialBackoffMilliseconds) * attempt;
            if (delayMs <= 0)
            {
                return;
            }

            await Task.Delay(delayMs, cancellationToken);
        }

        private enum AttemptKind
        {
            Succeeded,
            Transient,
            InvalidOutput,
            Failed,
        }

        private readonly record struct AttemptResult(
            AttemptKind Kind,
            WeeklyBriefProviderResult? Result
        )
        {
            public static AttemptResult Succeeded(WeeklyBriefProviderResult result)
                => new(AttemptKind.Succeeded, result);

            public static AttemptResult Transient()
                => new(AttemptKind.Transient, null);

            public static AttemptResult InvalidOutput()
                => new(AttemptKind.InvalidOutput, null);

            public static AttemptResult Failed(WeeklyBriefProviderResult result)
                => new(AttemptKind.Failed, result);
        }
    }
}
