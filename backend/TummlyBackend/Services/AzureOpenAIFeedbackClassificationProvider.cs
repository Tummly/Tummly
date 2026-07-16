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
    /// Production classification provider: Azure OpenAI Structured Outputs on a mini-tier deployment.
    /// </summary>
    public sealed class AzureOpenAIFeedbackClassificationProvider
        : IFeedbackClassificationProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly FeedbackClassificationSettings _settings;
        private readonly ILogger<AzureOpenAIFeedbackClassificationProvider> _logger;

        public AzureOpenAIFeedbackClassificationProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<FeedbackClassificationSettings> settings,
            ILogger<AzureOpenAIFeedbackClassificationProvider> logger
        )
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<FeedbackClassificationResult> ClassifyAsync(
            string comment,
            CancellationToken cancellationToken = default
        )
        {
            var maxAttempts = Math.Max(1, _settings.MaxAttempts);

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var attemptResult = await AttemptClassifyAsync(
                        comment,
                        cancellationToken
                    );

                    if (attemptResult.Kind == AttemptKind.Succeeded
                        || attemptResult.Kind == AttemptKind.UnsupportedLanguage)
                    {
                        return attemptResult.Result
                            ?? new FeedbackClassificationResult.Failed();
                    }

                    if (attemptResult.Kind == AttemptKind.Transient
                        || attemptResult.Kind == AttemptKind.InvalidOutput)
                    {
                        if (attempt >= maxAttempts)
                        {
                            break;
                        }

                        await DelayBackoffAsync(attempt, cancellationToken);
                        continue;
                    }

                    return new FeedbackClassificationResult.Failed();
                }
                catch (OperationCanceledException) when (
                    cancellationToken.IsCancellationRequested
                )
                {
                    // Caller cancellation only — HttpClient timeouts are TaskCanceledException
                    // without this token and must retry as transient.
                    throw;
                }
                catch (Exception ex) when (IsTransientException(ex))
                {
                    _logger.LogWarning(
                        ex,
                        "Transient classification failure (attempt {Attempt}/{MaxAttempts})",
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

            return new FeedbackClassificationResult.Failed();
        }

        private async Task<AttemptResult> AttemptClassifyAsync(
            string comment,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(_settings.Endpoint)
                || string.IsNullOrWhiteSpace(_settings.ApiKey)
                || string.IsNullOrWhiteSpace(_settings.DeploymentName))
            {
                _logger.LogError(
                    "Azure OpenAI classification is misconfigured (endpoint, api key, or deployment)."
                );
                return AttemptResult.Failed();
            }

            var client = _httpClientFactory.CreateClient(
                FeedbackClassificationStructuredOutput.HttpClientName
            );

            var requestUri = BuildChatCompletionsUri();
            var body = FeedbackClassificationStructuredOutput.BuildRequestJson(
                _settings.DeploymentName,
                comment,
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
                    "Azure OpenAI returned {StatusCode} for classification",
                    (int)response.StatusCode
                );
                return AttemptResult.Transient();
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Azure OpenAI classification failed with {StatusCode}",
                    (int)response.StatusCode
                );
                return AttemptResult.Failed();
            }

            var responseJson = await response.Content.ReadAsStringAsync(
                cancellationToken
            );

            if (!FeedbackClassificationStructuredOutput.TryExtractMessageContent(
                    responseJson,
                    out var content
                ))
            {
                return AttemptResult.InvalidOutput();
            }

            if (!FeedbackClassificationStructuredOutput.TryParseModelContent(
                    content,
                    out var result,
                    out var unsupportedLanguage,
                    out var invalidOutput
                ))
            {
                return invalidOutput
                    ? AttemptResult.InvalidOutput()
                    : AttemptResult.Failed();
            }

            if (unsupportedLanguage)
            {
                return AttemptResult.UnsupportedLanguage(result!);
            }

            return AttemptResult.Succeeded(result!);
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
            UnsupportedLanguage,
            Transient,
            InvalidOutput,
            Failed
        }

        private readonly record struct AttemptResult(
            AttemptKind Kind,
            FeedbackClassificationResult? Result
        )
        {
            public static AttemptResult Succeeded(
                FeedbackClassificationResult result
            )
                => new(AttemptKind.Succeeded, result);

            public static AttemptResult UnsupportedLanguage(
                FeedbackClassificationResult result
            )
                => new(AttemptKind.UnsupportedLanguage, result);

            public static AttemptResult Transient()
                => new(AttemptKind.Transient, null);

            public static AttemptResult InvalidOutput()
                => new(AttemptKind.InvalidOutput, null);

            public static AttemptResult Failed()
                => new(AttemptKind.Failed, null);
        }
    }
}
