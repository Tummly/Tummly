using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Production Home-native recommendation provider: Azure OpenAI Structured Outputs.
    /// Home recommendation via one-wave surface read tools.
    /// Domain router stays server-owned. Reuses FeedbackClassification settings.
    /// </summary>
    public sealed class AzureOpenAIHomeRecommendationProvider
        : IHomeRecommendationProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly FeedbackClassificationSettings _settings;
        private readonly ILogger<AzureOpenAIHomeRecommendationProvider> _logger;

        public AzureOpenAIHomeRecommendationProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<FeedbackClassificationSettings> settings,
            ILogger<AzureOpenAIHomeRecommendationProvider> logger
        )
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<HomeRecommendationProviderResult> RecommendAsync(
            HomeRecommendationProviderInput input,
            CancellationToken cancellationToken = default
        )
        {
            var maxAttempts = Math.Max(1, _settings.MaxAttempts);

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var attemptResult = await AttemptRecommendWithToolsAsync(input, cancellationToken);

                    if (attemptResult.Kind == AttemptKind.Succeeded)
                    {
                        return attemptResult.Result
                            ?? new HomeRecommendationProviderResult.Failed(Retryable: true);
                    }

                    if (attemptResult.Kind == AttemptKind.Failed)
                    {
                        return attemptResult.Result
                            ?? new HomeRecommendationProviderResult.Failed(Retryable: true);
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
                        "Transient home recommendation failure (attempt {Attempt}/{MaxAttempts})",
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

            return new HomeRecommendationProviderResult.Failed(Retryable: true);
        }

        private async Task<AttemptResult> AttemptRecommendWithToolsAsync(
            HomeRecommendationProviderInput input,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(_settings.Endpoint)
                || string.IsNullOrWhiteSpace(_settings.ApiKey)
                || string.IsNullOrWhiteSpace(_settings.DeploymentName))
            {
                _logger.LogError(
                    "Azure OpenAI home recommendation is misconfigured (endpoint, api key, or deployment)."
                );
                return AttemptResult.Failed(
                    new HomeRecommendationProviderResult.Failed(Retryable: true)
                );
            }

            var client = _httpClientFactory.CreateClient(
                HomeRecommendationStructuredOutput.HttpClientName
            );
            var forced = new List<AssistantToolCallRequest>
            {
                new(
                    "forced_metrics",
                    SurfaceReadToolCatalog.ReadHomeRecommendationMetrics,
                    "{}"
                ),
            };

            var loop = await AzureOpenAIOneWaveToolLoop.RunAsync(
                new AzureOpenAIOneWaveRequest
                {
                    SurfaceName = "home recommendation",
                    HttpClient = client,
                    Endpoint = _settings.Endpoint,
                    ApiKey = _settings.ApiKey,
                    DeploymentName = _settings.DeploymentName,
                    ApiVersion = _settings.ApiVersion,
                    SeedMessages = HomeRecommendationStructuredOutput
                        .BuildToolSeedMessages(
                            input,
                            _settings.PromptSchemaVersion
                        ),
                    ToolsArray = SurfaceReadToolCatalog.HomeRecommendationTools(),
                    SchemaName = HomeRecommendationStructuredOutput.SchemaName,
                    FinalSchema = HomeRecommendationStructuredOutput.BuildSchema(),
                    ExecuteTools = (calls, _) => Task.FromResult(
                        SurfaceReadToolHost.ExecuteHomeRecommendation(calls, input)
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
                    HomeRecommendationStructuredOutput.TryParseModelContent(
                        succeeded.Content,
                        out var output,
                        out var invalidOutput
                    )
                        ? AttemptResult.Succeeded(
                            new HomeRecommendationProviderResult.Succeeded(output!)
                        )
                        : invalidOutput
                            ? AttemptResult.InvalidOutput()
                            : AttemptResult.Failed(
                                new HomeRecommendationProviderResult.Failed(
                                    Retryable: true
                                )
                            ),
                AzureOpenAIOneWaveResult.Transient => AttemptResult.Transient(),
                AzureOpenAIOneWaveResult.InvalidOutput => AttemptResult.InvalidOutput(),
                _ => AttemptResult.Failed(
                    new HomeRecommendationProviderResult.Failed(Retryable: true)
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
            HomeRecommendationProviderResult? Result
        )
        {
            public static AttemptResult Succeeded(HomeRecommendationProviderResult result)
                => new(AttemptKind.Succeeded, result);

            public static AttemptResult Transient()
                => new(AttemptKind.Transient, null);

            public static AttemptResult InvalidOutput()
                => new(AttemptKind.InvalidOutput, null);

            public static AttemptResult Failed(HomeRecommendationProviderResult result)
                => new(AttemptKind.Failed, result);
        }
    }
}
