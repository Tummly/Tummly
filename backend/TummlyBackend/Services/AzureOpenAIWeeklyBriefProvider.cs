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
    /// Production Weekly brief provider: Azure OpenAI Structured Outputs.
    /// Reuses FeedbackClassification Endpoint/ApiKey/Deployment settings.
    /// Uses the Weekly-brief-owned schema (not Campaigns / Home recommendation).
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
                    var attemptResult = await AttemptGenerateAsync(
                        input,
                        cancellationToken
                    );

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
                catch (Exception ex) when (IsTransientException(ex))
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

        private async Task<AttemptResult> AttemptGenerateAsync(
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

            var requestUri = BuildChatCompletionsUri();
            var body = WeeklyBriefStructuredOutput.BuildRequestJson(
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

            using var response = await client.SendAsync(request, cancellationToken);

            if (IsTransientStatusCode(response.StatusCode))
            {
                _logger.LogWarning(
                    "Azure OpenAI returned {StatusCode} for weekly brief",
                    (int)response.StatusCode
                );
                return AttemptResult.Transient();
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Azure OpenAI weekly brief failed with {StatusCode}",
                    (int)response.StatusCode
                );
                return AttemptResult.Failed(
                    new WeeklyBriefProviderResult.Failed(Retryable: true)
                );
            }

            var responseJson = await response.Content.ReadAsStringAsync(
                cancellationToken
            );

            if (!WeeklyBriefStructuredOutput.TryExtractMessageContent(
                    responseJson,
                    out var content
                ))
            {
                return AttemptResult.InvalidOutput();
            }

            if (!WeeklyBriefStructuredOutput.TryParseModelContent(
                    content,
                    out var output,
                    out var invalidOutput
                ))
            {
                return invalidOutput
                    ? AttemptResult.InvalidOutput()
                    : AttemptResult.Failed(
                        new WeeklyBriefProviderResult.Failed(Retryable: true)
                    );
            }

            return AttemptResult.Succeeded(
                new WeeklyBriefProviderResult.Succeeded(output!)
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
            var delayMs = Math.Max(0, _settings.InitialBackoffMilliseconds) * attempt;

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
