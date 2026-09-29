using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Production recovery draft provider: Azure OpenAI Structured Outputs.
    /// Feedback recovery draft via one-wave surface read tools.
    /// Reuses FeedbackClassification Endpoint/ApiKey/Deployment settings.
    /// </summary>
    public sealed class AzureOpenAIFeedbackRecoveryDraftProvider
        : IFeedbackRecoveryDraftProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly FeedbackClassificationSettings _settings;
        private readonly ILogger<AzureOpenAIFeedbackRecoveryDraftProvider> _logger;

        public AzureOpenAIFeedbackRecoveryDraftProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<FeedbackClassificationSettings> settings,
            ILogger<AzureOpenAIFeedbackRecoveryDraftProvider> logger
        )
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<FeedbackRecoveryDraftResult> DraftAsync(
            FeedbackRecoveryDraftInput input,
            CancellationToken cancellationToken = default
        )
        {
            var maxAttempts = Math.Max(1, _settings.MaxAttempts);

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var attemptResult = await AttemptDraftWithToolsAsync(input, cancellationToken);

                    if (attemptResult.Kind == AttemptKind.Succeeded)
                    {
                        return attemptResult.Result
                            ?? new FeedbackRecoveryDraftResult.Failed(Retryable: true);
                    }

                    if (attemptResult.Kind == AttemptKind.Failed)
                    {
                        return attemptResult.Result
                            ?? new FeedbackRecoveryDraftResult.Failed(Retryable: true);
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
                        "Transient recovery draft failure (attempt {Attempt}/{MaxAttempts})",
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

            return new FeedbackRecoveryDraftResult.Failed(Retryable: true);
        }

        private async Task<AttemptResult> AttemptDraftWithToolsAsync(
            FeedbackRecoveryDraftInput input,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(_settings.Endpoint)
                || string.IsNullOrWhiteSpace(_settings.ApiKey)
                || string.IsNullOrWhiteSpace(_settings.DeploymentName))
            {
                _logger.LogError(
                    "Azure OpenAI recovery draft is misconfigured (endpoint, api key, or deployment)."
                );
                return AttemptResult.Failed(
                    new FeedbackRecoveryDraftResult.Failed(Retryable: true)
                );
            }

            var client = _httpClientFactory.CreateClient(
                FeedbackRecoveryDraftStructuredOutput.HttpClientName
            );
            var includeOffer = input.ConfirmedOffer is not null;
            var forced = new List<AssistantToolCallRequest>
            {
                new(
                    "forced_location",
                    SurfaceReadToolCatalog.ReadLocationDisplayName,
                    "{}"
                ),
                new(
                    "forced_feedback",
                    SurfaceReadToolCatalog.ReadRecoveryFeedbackFacts,
                    "{}"
                ),
            };
            if (includeOffer)
            {
                forced.Add(
                    new(
                        "forced_offer",
                        SurfaceReadToolCatalog.ReadConfirmedOfferFacts,
                        "{}"
                    )
                );
            }

            var loop = await AzureOpenAIOneWaveToolLoop.RunAsync(
                new AzureOpenAIOneWaveRequest
                {
                    SurfaceName = "recovery draft",
                    HttpClient = client,
                    Endpoint = _settings.Endpoint,
                    ApiKey = _settings.ApiKey,
                    DeploymentName = _settings.DeploymentName,
                    ApiVersion = _settings.ApiVersion,
                    SeedMessages = FeedbackRecoveryDraftStructuredOutput
                        .BuildToolSeedMessages(
                            input,
                            _settings.PromptSchemaVersion
                        ),
                    ToolsArray = SurfaceReadToolCatalog.RecoveryDraftTools(includeOffer),
                    SchemaName = FeedbackRecoveryDraftStructuredOutput.SchemaName,
                    FinalSchema = FeedbackRecoveryDraftStructuredOutput.BuildSchema(),
                    ExecuteTools = (calls, _) => Task.FromResult(
                        SurfaceReadToolHost.ExecuteRecoveryDraft(calls, input)
                    ),
                    ForcedToolCalls = forced,
                    Logger = _logger,
                },
                cancellationToken
            );

            return MapLoopResult(loop, input.Channel);
        }

        private AttemptResult MapLoopResult(
            AzureOpenAIOneWaveResult loop,
            string channel
        )
            => loop switch
            {
                AzureOpenAIOneWaveResult.Succeeded succeeded =>
                    FeedbackRecoveryDraftStructuredOutput.TryParseModelContent(
                        succeeded.Content,
                        channel,
                        out var result,
                        out var invalidOutput
                    )
                        ? AttemptResult.Succeeded(result!)
                        : invalidOutput
                            ? AttemptResult.InvalidOutput()
                            : AttemptResult.Failed(
                                new FeedbackRecoveryDraftResult.Failed(Retryable: true)
                            ),
                AzureOpenAIOneWaveResult.Transient => AttemptResult.Transient(),
                AzureOpenAIOneWaveResult.InvalidOutput => AttemptResult.InvalidOutput(),
                _ => AttemptResult.Failed(
                    new FeedbackRecoveryDraftResult.Failed(Retryable: true)
                ),
            };

        private async Task DelayBackoffAsync(
            int attempt,
            CancellationToken cancellationToken
        )
        {
            var delayMs = Math.Max(0, _settings.InitialBackoffMilliseconds)
                * attempt;

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
            Failed
        }

        private readonly record struct AttemptResult(
            AttemptKind Kind,
            FeedbackRecoveryDraftResult? Result
        )
        {
            public static AttemptResult Succeeded(FeedbackRecoveryDraftResult result)
                => new(AttemptKind.Succeeded, result);

            public static AttemptResult Transient()
                => new(AttemptKind.Transient, null);

            public static AttemptResult InvalidOutput()
                => new(AttemptKind.InvalidOutput, null);

            public static AttemptResult Failed(FeedbackRecoveryDraftResult result)
                => new(AttemptKind.Failed, result);
        }
    }
}
