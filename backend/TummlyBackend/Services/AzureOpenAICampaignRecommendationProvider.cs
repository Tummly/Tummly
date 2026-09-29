using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Production Campaign recommendation provider: Azure OpenAI Structured Outputs.
    /// Campaign recommendation via one-wave surface read tools.
    /// Reuses FeedbackClassification Endpoint/ApiKey/Deployment settings.
    /// </summary>
    public sealed class AzureOpenAICampaignRecommendationProvider
        : ICampaignRecommendationProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly FeedbackClassificationSettings _settings;
        private readonly ILogger<AzureOpenAICampaignRecommendationProvider> _logger;

        public AzureOpenAICampaignRecommendationProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<FeedbackClassificationSettings> settings,
            ILogger<AzureOpenAICampaignRecommendationProvider> logger
        )
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<CampaignRecommendationProviderResult> RecommendAsync(
            CampaignRecommendationProviderInput input,
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
                            ?? new CampaignRecommendationProviderResult.Failed(
                                Retryable: true
                            );
                    }

                    if (attemptResult.Kind == AttemptKind.Failed)
                    {
                        return attemptResult.Result
                            ?? new CampaignRecommendationProviderResult.Failed(
                                Retryable: true
                            );
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
                        "Transient campaign recommendation failure (attempt {Attempt}/{MaxAttempts})",
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

            return new CampaignRecommendationProviderResult.Failed(Retryable: true);
        }

        private async Task<AttemptResult> AttemptRecommendWithToolsAsync(
            CampaignRecommendationProviderInput input,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(_settings.Endpoint)
                || string.IsNullOrWhiteSpace(_settings.ApiKey)
                || string.IsNullOrWhiteSpace(_settings.DeploymentName))
            {
                _logger.LogError(
                    "Azure OpenAI campaign recommendation is misconfigured (endpoint, api key, or deployment)."
                );
                return AttemptResult.Failed(
                    new CampaignRecommendationProviderResult.Failed(Retryable: true)
                );
            }

            var client = _httpClientFactory.CreateClient(
                CampaignRecommendationStructuredOutput.HttpClientName
            );
            var forced = new List<AssistantToolCallRequest>
            {
                new(
                    "forced_metrics",
                    SurfaceReadToolCatalog.ReadCampaignRecommendationMetrics,
                    "{}"
                ),
            };

            var loop = await AzureOpenAIOneWaveToolLoop.RunAsync(
                new AzureOpenAIOneWaveRequest
                {
                    SurfaceName = "campaign recommendation",
                    HttpClient = client,
                    Endpoint = _settings.Endpoint,
                    ApiKey = _settings.ApiKey,
                    DeploymentName = _settings.DeploymentName,
                    ApiVersion = _settings.ApiVersion,
                    SeedMessages = CampaignRecommendationStructuredOutput
                        .BuildToolSeedMessages(
                            input,
                            _settings.PromptSchemaVersion
                        ),
                    ToolsArray = SurfaceReadToolCatalog.CampaignRecommendationTools(),
                    SchemaName = CampaignRecommendationStructuredOutput.SchemaName,
                    FinalSchema = CampaignRecommendationStructuredOutput.BuildSchema(),
                    ExecuteTools = (calls, _) => Task.FromResult(
                        SurfaceReadToolHost.ExecuteCampaignRecommendation(calls, input)
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
                    CampaignRecommendationStructuredOutput.TryParseModelContent(
                        succeeded.Content,
                        out var output,
                        out var invalidOutput
                    )
                        ? AttemptResult.Succeeded(
                            new CampaignRecommendationProviderResult.Succeeded(output!)
                        )
                        : invalidOutput
                            ? AttemptResult.InvalidOutput()
                            : AttemptResult.Failed(
                                new CampaignRecommendationProviderResult.Failed(
                                    Retryable: true
                                )
                            ),
                AzureOpenAIOneWaveResult.Transient => AttemptResult.Transient(),
                AzureOpenAIOneWaveResult.InvalidOutput => AttemptResult.InvalidOutput(),
                _ => AttemptResult.Failed(
                    new CampaignRecommendationProviderResult.Failed(Retryable: true)
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
            CampaignRecommendationProviderResult? Result
        )
        {
            public static AttemptResult Succeeded(
                CampaignRecommendationProviderResult result
            )
                => new(AttemptKind.Succeeded, result);

            public static AttemptResult Transient()
                => new(AttemptKind.Transient, null);

            public static AttemptResult InvalidOutput()
                => new(AttemptKind.InvalidOutput, null);

            public static AttemptResult Failed(
                CampaignRecommendationProviderResult result
            )
                => new(AttemptKind.Failed, result);
        }
    }
}
