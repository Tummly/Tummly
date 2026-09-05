using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Production advisory Reason provider: Azure OpenAI Structured Outputs.
    /// Reuses FeedbackClassification Endpoint/ApiKey/Deployment settings.
    /// </summary>
    public sealed class AzureOpenAIAssistantAdvisoryReasonProvider
        : IAssistantAdvisoryReasonProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly FeedbackClassificationSettings _settings;
        private readonly ILogger<AzureOpenAIAssistantAdvisoryReasonProvider> _logger;

        public AzureOpenAIAssistantAdvisoryReasonProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<FeedbackClassificationSettings> settings,
            ILogger<AzureOpenAIAssistantAdvisoryReasonProvider> logger
        )
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<AssistantAdvisoryReasonResult> CompleteAsync(
            AssistantAdvisoryReasonInput input,
            CancellationToken cancellationToken = default
        )
        {
            var maxAttempts = Math.Max(1, _settings.MaxAttempts);

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var attemptResult = await AttemptCompleteAsync(
                        input,
                        cancellationToken
                    );

                    if (attemptResult.Kind == AttemptKind.Succeeded)
                    {
                        return attemptResult.Result
                            ?? new AssistantAdvisoryReasonResult.Failed(
                                Retryable: true
                            );
                    }

                    if (attemptResult.Kind == AttemptKind.Failed)
                    {
                        return attemptResult.Result
                            ?? new AssistantAdvisoryReasonResult.Failed(
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
                catch (Exception ex) when (IsTransientException(ex))
                {
                    _logger.LogWarning(
                        ex,
                        "Transient advisory Reason failure (attempt {Attempt}/{MaxAttempts})",
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

            return new AssistantAdvisoryReasonResult.Failed(Retryable: true);
        }

        private async Task<AttemptResult> AttemptCompleteAsync(
            AssistantAdvisoryReasonInput input,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(_settings.Endpoint)
                || string.IsNullOrWhiteSpace(_settings.ApiKey)
                || string.IsNullOrWhiteSpace(_settings.DeploymentName))
            {
                _logger.LogError(
                    "Azure OpenAI advisory Reason is misconfigured "
                    + "(endpoint, api key, or deployment)."
                );
                return AttemptResult.Failed(
                    new AssistantAdvisoryReasonResult.Failed(Retryable: true)
                );
            }

            var client = _httpClientFactory.CreateClient(
                AssistantAdvisoryReasonStructuredOutput.HttpClientName
            );

            var requestUri = BuildChatCompletionsUri();
            var body = AssistantAdvisoryReasonStructuredOutput.BuildRequestJson(
                _settings.DeploymentName,
                input,
                _settings.PromptSchemaVersion
            );

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.TryAddWithoutValidation("api-key", _settings.ApiKey);
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
            request.Content = new StringContent(
                body,
                Encoding.UTF8,
                "application/json"
            );

            using var response = await client.SendAsync(
                request,
                cancellationToken
            );

            if (IsTransientStatusCode(response.StatusCode))
            {
                _logger.LogWarning(
                    "Azure OpenAI returned {StatusCode} for advisory Reason",
                    (int)response.StatusCode
                );
                return AttemptResult.Transient();
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Azure OpenAI advisory Reason failed with {StatusCode}",
                    (int)response.StatusCode
                );
                return AttemptResult.Failed(
                    new AssistantAdvisoryReasonResult.Failed(Retryable: true)
                );
            }

            var responseJson = await response.Content.ReadAsStringAsync(
                cancellationToken
            );

            if (!AssistantAdvisoryReasonStructuredOutput.TryExtractMessageContent(
                    responseJson,
                    out var content
                ))
            {
                return AttemptResult.InvalidOutput();
            }

            if (!AssistantAdvisoryReasonStructuredOutput.TryParseModelContent(
                    content,
                    out var output,
                    out var invalidOutput
                ))
            {
                return invalidOutput
                    ? AttemptResult.InvalidOutput()
                    : AttemptResult.Failed(
                        new AssistantAdvisoryReasonResult.Failed(Retryable: true)
                    );
            }

            return AttemptResult.Succeeded(
                new AssistantAdvisoryReasonResult.Succeeded(output!)
            );
        }

        private Uri BuildChatCompletionsUri()
        {
            var endpoint = _settings.Endpoint.TrimEnd('/') + "/";
            var relative =
                $"openai/deployments/{Uri.EscapeDataString(_settings.DeploymentName)}"
                + $"/chat/completions?api-version={Uri.EscapeDataString(_settings.ApiVersion)}";

            return new Uri(new Uri(endpoint), relative);
        }

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

        private static bool IsTransientStatusCode(HttpStatusCode statusCode)
            => statusCode == HttpStatusCode.RequestTimeout
                || statusCode == HttpStatusCode.TooManyRequests
                || (int)statusCode >= 500;

        private static bool IsTransientException(Exception ex)
            => ex is HttpRequestException or TaskCanceledException;

        private enum AttemptKind
        {
            Succeeded,
            Transient,
            InvalidOutput,
            Failed,
        }

        private readonly record struct AttemptResult(
            AttemptKind Kind,
            AssistantAdvisoryReasonResult? Result
        )
        {
            public static AttemptResult Succeeded(
                AssistantAdvisoryReasonResult result
            )
                => new(AttemptKind.Succeeded, result);

            public static AttemptResult Transient()
                => new(AttemptKind.Transient, null);

            public static AttemptResult InvalidOutput()
                => new(AttemptKind.InvalidOutput, null);

            public static AttemptResult Failed(
                AssistantAdvisoryReasonResult result
            )
                => new(AttemptKind.Failed, result);
        }
    }
}
