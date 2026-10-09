using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Production live-answer provider: Azure OpenAI Structured Outputs.
    /// Retrieve-tool path: one native tool wave, one corrective tool wave,
    /// then a forced structured final answer.
    /// </summary>
    public sealed class AzureOpenAIAssistantLiveAnswerProvider
        : IAssistantLiveAnswerProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly FeedbackClassificationSettings _settings;
        private readonly ILogger<AzureOpenAIAssistantLiveAnswerProvider> _logger;

        public AzureOpenAIAssistantLiveAnswerProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<FeedbackClassificationSettings> settings,
            ILogger<AzureOpenAIAssistantLiveAnswerProvider> logger
        )
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<AssistantLiveAnswerResult> CompleteAsync(
            AssistantLiveAnswerInput input,
            CancellationToken cancellationToken = default
        )
            => await CompleteWithRetrieveToolsAsync(input, cancellationToken);

        private async Task<AssistantLiveAnswerResult> CompleteWithRetrieveToolsAsync(
            AssistantLiveAnswerInput input,
            CancellationToken cancellationToken
        )
        {
            var deploymentName = AssistantModelDeployment();
            if (string.IsNullOrWhiteSpace(_settings.Endpoint)
                || string.IsNullOrWhiteSpace(_settings.ApiKey)
                || string.IsNullOrWhiteSpace(deploymentName))
            {
                _logger.LogError(
                    "Azure OpenAI Assistant live answer is misconfigured (endpoint, api key, or deployment)."
                );
                return new AssistantLiveAnswerResult.Failed(Retryable: true);
            }

            var client = _httpClientFactory.CreateClient(
                AssistantLiveAnswerStructuredOutput.HttpClientName
            );
            var requestUri = BuildChatCompletionsUri(deploymentName);
            if (input.ExecuteRetrieveTools is null)
            {
                // Follow-ups such as an Offer expiry fill send history and no
                // retrieve executor. Answer from the thread instead of failing.
                return await CompleteHistoryOnlyAsync(
                    client,
                    requestUri,
                    deploymentName,
                    input,
                    cancellationToken
                );
            }
            var messages = AssistantLiveAnswerStructuredOutput
                .BuildRetrieveToolsSeedMessages(
                    input,
                    _settings.PromptSchemaVersion
                );

            // Round 1: tools allowed (no structured schema yet).
            var round1Json = AssistantLiveAnswerStructuredOutput
                .BuildRetrieveToolsRoundJson(
                    deploymentName,
                    input,
                    _settings.PromptSchemaVersion,
                    messages,
                    allowTools: true
                );
            var round1Response = await SendChatAsync(
                client,
                requestUri,
                round1Json,
                cancellationToken
            );
            if (round1Response.Kind != AttemptKind.Succeeded
                || round1Response.ResponseJson is null)
            {
                return round1Response.Result
                    ?? new AssistantLiveAnswerResult.Failed(Retryable: true);
            }

            var evidenceForParse = input.ReadToolEvidence?.Invoke() ?? input.Evidence;
            if (AssistantLiveAnswerStructuredOutput.TryExtractToolCalls(
                    round1Response.ResponseJson,
                    out var toolCalls
                ))
            {
                var forcedCompareAll = false;
                if (input.CompareAll
                    && !toolCalls.Any(call =>
                        string.Equals(
                            call.Name,
                            AssistantRetrieveToolCatalog.CompareAllLocations,
                            StringComparison.Ordinal
                        )))
                {
                    // Model picked domain reads under All scope (e.g. read_guests),
                    // which block without OwnedLocationId. Force compare-all.
                    toolCalls =
                    [
                        new AssistantToolCallRequest(
                            "forced_compare_all",
                            AssistantRetrieveToolCatalog.CompareAllLocations,
                            "{}"
                        ),
                    ];
                    forcedCompareAll = true;
                }

                if (input.OnRetrieveProgress is not null)
                {
                    await input.OnRetrieveProgress(
                        AssistantTurnProgressSteps.Retrieving,
                        cancellationToken
                    );
                }

                var toolResults = await input.ExecuteRetrieveTools(
                    toolCalls,
                    cancellationToken
                );
                evidenceForParse = input.ReadToolEvidence?.Invoke() ?? evidenceForParse;
                if (forcedCompareAll)
                {
                    messages.Add(
                        new JsonObject
                        {
                            ["role"] = "assistant",
                            ["content"] = null,
                            ["tool_calls"] = new JsonArray(
                                toolCalls
                                    .Select(call => (JsonNode?)new JsonObject
                                    {
                                        ["id"] = call.Id,
                                        ["type"] = "function",
                                        ["function"] = new JsonObject
                                        {
                                            ["name"] = call.Name,
                                            ["arguments"] = call.ArgumentsJson,
                                        },
                                    })
                                    .ToArray()
                            ),
                        }
                    );
                }
                else
                {
                    AssistantLiveAnswerStructuredOutput.AppendAssistantToolCallsMessage(
                        messages,
                        round1Response.ResponseJson
                    );
                }

                AssistantLiveAnswerStructuredOutput.AppendToolResultMessages(
                    messages,
                    toolResults
                );

                var corrective = await TrySecondToolWaveAsync(
                    client,
                    requestUri,
                    deploymentName,
                    messages,
                    input,
                    evidenceForParse,
                    cancellationToken
                );
                evidenceForParse = corrective.Evidence;
                if (corrective.Result is not null)
                {
                    return corrective.Result;
                }
            }
            else if (AssistantLiveAnswerStructuredOutput.TryExtractMessageContent(
                    round1Response.ResponseJson,
                    out var earlyContent
                )
                && AssistantLiveAnswerStructuredOutput.TryParseModelContent(
                    earlyContent,
                    evidenceForParse,
                    input.UserMessage,
                    out var earlyResult,
                    out _
                )
                && earlyResult is AssistantLiveAnswerResult.Succeeded earlySucceeded
                && (earlySucceeded.Class is AssistantMessageClass.Clarify
                        or AssistantMessageClass.Refusal
                        or AssistantMessageClass.Failure
                    || (earlySucceeded.Class == AssistantMessageClass.Grounded
                        && !AssistantAskIntent.HasRetrieveAsk(input.UserMessage)
                        && !AssistantAttentionAsk.IsAttentionRetrieve(input.UserMessage)
                        && !input.NamedCompare
                        && !input.CompareAll
                        && !AssistantTaskClassification.LooksLikeCreateTurn(
                            input.UserMessage
                        )
                        && !AssistantTaskClassification.LooksLikeRecoveryPath(
                            input.UserMessage
                        ))))
            {
                // Clarify/refuse, or grounded capability/greeting without tools.
                // Do not force domain reads when the ask is not a retrieve.
                // Attention asks always take the tool wave.
                return earlyResult;
            }
            else
            {
                // Model skipped tools on a retrieve ask — force empty tool wave
                // then final answer so grounding still comes from server tools.
                if (input.OnRetrieveProgress is not null)
                {
                    await input.OnRetrieveProgress(
                        AssistantTurnProgressSteps.Retrieving,
                        cancellationToken
                    );
                }

                var forcedCalls = ForcedRetrieveToolCalls(input)
                    .Select(
                        (name, index) => new AssistantToolCallRequest(
                            $"forced_{index}_{name}",
                            name,
                            "{}"
                        )
                    )
                    .ToList();
                if (forcedCalls.Count == 0)
                {
                    // Non-retrieve ask skipped tools and had no parseable early
                    // answer — final structured round without tool stuffing.
                    if (input.OnRetrieveProgress is not null)
                    {
                        await input.OnRetrieveProgress(
                            AssistantTurnProgressSteps.Preparing,
                            cancellationToken
                        );
                    }

                    var finalOnlyJson = AssistantLiveAnswerStructuredOutput
                        .BuildRetrieveToolsRoundJson(
                            deploymentName,
                            input,
                            _settings.PromptSchemaVersion,
                            messages,
                            allowTools: false
                        );
                    var finalOnly = await SendChatAsync(
                        client,
                        requestUri,
                        finalOnlyJson,
                        cancellationToken
                    );
                    if (finalOnly.Kind != AttemptKind.Succeeded
                        || finalOnly.ResponseJson is null)
                    {
                        return finalOnly.Result
                            ?? new AssistantLiveAnswerResult.Failed(Retryable: true);
                    }

                    if (!AssistantLiveAnswerStructuredOutput.TryExtractMessageContent(
                            finalOnly.ResponseJson,
                            out var finalOnlyContent
                        )
                        || !AssistantLiveAnswerStructuredOutput.TryParseModelContent(
                            finalOnlyContent,
                            evidenceForParse,
                            input.UserMessage,
                            out var finalOnlyResult,
                            out _
                        ))
                    {
                        return new AssistantLiveAnswerResult.Failed(Retryable: true);
                    }

                    return finalOnlyResult
                        ?? new AssistantLiveAnswerResult.Failed(Retryable: true);
                }

                var toolResults = await input.ExecuteRetrieveTools(
                    forcedCalls,
                    cancellationToken
                );
                evidenceForParse = input.ReadToolEvidence?.Invoke() ?? evidenceForParse;
                messages.Add(
                    new JsonObject
                    {
                        ["role"] = "assistant",
                        ["content"] = null,
                        ["tool_calls"] = new JsonArray(
                            forcedCalls
                                .Select(call => (JsonNode?)new JsonObject
                                {
                                    ["id"] = call.Id,
                                    ["type"] = "function",
                                    ["function"] = new JsonObject
                                    {
                                        ["name"] = call.Name,
                                        ["arguments"] = call.ArgumentsJson,
                                    },
                                })
                                .ToArray()
                        ),
                    }
                );
                AssistantLiveAnswerStructuredOutput.AppendToolResultMessages(
                    messages,
                    toolResults
                );
            }

            if (input.OnRetrieveProgress is not null)
            {
                await input.OnRetrieveProgress(
                    AssistantTurnProgressSteps.Preparing,
                    cancellationToken
                );
            }

            // Final round: tools disabled; structured answer required.
            // A third tool wave is ignored so a missed domain can be corrected
            // once and cannot loop.
            var round2Json = AssistantLiveAnswerStructuredOutput
                .BuildRetrieveToolsRoundJson(
                    deploymentName,
                    input,
                    _settings.PromptSchemaVersion,
                    messages,
                    allowTools: false
                );
            var round2Response = await SendChatAsync(
                client,
                requestUri,
                round2Json,
                cancellationToken
            );
            if (round2Response.Kind != AttemptKind.Succeeded
                || round2Response.ResponseJson is null)
            {
                return round2Response.Result
                    ?? new AssistantLiveAnswerResult.Failed(Retryable: true);
            }

            if (!AssistantLiveAnswerStructuredOutput.TryExtractMessageContent(
                    round2Response.ResponseJson,
                    out var content
                ))
            {
                return new AssistantLiveAnswerResult.Failed(Retryable: true);
            }

            if (!AssistantLiveAnswerStructuredOutput.TryParseModelContent(
                    content,
                    evidenceForParse,
                    input.UserMessage,
                    out var result,
                    out var invalidOutput
                ))
            {
                return invalidOutput
                    ? new AssistantLiveAnswerResult.Failed(Retryable: true)
                    : new AssistantLiveAnswerResult.Failed(Retryable: true);
            }

            // Cap: ignore tool_calls on the structured final round.
            if (AssistantLiveAnswerStructuredOutput.TryExtractToolCalls(
                    round2Response.ResponseJson,
                    out _
                )
                && result is null)
            {
                return new AssistantLiveAnswerResult.Failed(Retryable: true);
            }

            return result ?? new AssistantLiveAnswerResult.Failed(Retryable: true);
        }

        private async Task<AssistantLiveAnswerResult> CompleteHistoryOnlyAsync(
            HttpClient client,
            Uri requestUri,
            string deploymentName,
            AssistantLiveAnswerInput input,
            CancellationToken cancellationToken
        )
        {
            var messages = AssistantLiveAnswerStructuredOutput
                .BuildRetrieveToolsSeedMessages(
                    input,
                    _settings.PromptSchemaVersion
                );
            var json = AssistantLiveAnswerStructuredOutput.BuildRetrieveToolsRoundJson(
                deploymentName,
                input,
                _settings.PromptSchemaVersion,
                messages,
                allowTools: false
            );
            var response = await SendChatAsync(
                client,
                requestUri,
                json,
                cancellationToken
            );
            if (response.Kind != AttemptKind.Succeeded
                || response.ResponseJson is null)
            {
                return response.Result
                    ?? new AssistantLiveAnswerResult.Failed(Retryable: true);
            }

            if (!AssistantLiveAnswerStructuredOutput.TryExtractMessageContent(
                    response.ResponseJson,
                    out var content
                )
                || !AssistantLiveAnswerStructuredOutput.TryParseModelContent(
                    content,
                    input.Evidence,
                    input.UserMessage,
                    out var result,
                    out _
                ))
            {
                return new AssistantLiveAnswerResult.Failed(Retryable: true);
            }

            return result ?? new AssistantLiveAnswerResult.Failed(Retryable: true);
        }

        private string AssistantModelDeployment()
            => string.IsNullOrWhiteSpace(_settings.AssistantDeploymentName)
                ? _settings.DeploymentName
                : _settings.AssistantDeploymentName.Trim();

        /// <summary>
        /// One corrective tool wave after the model's first lookups. A parsed
        /// answer with no further tool calls is returned as-is. Another tool
        /// call is executed, then the caller forces the structured answer.
        /// </summary>
        private async Task<(
            AssistantRetrievedEvidence Evidence,
            AssistantLiveAnswerResult? Result
        )> TrySecondToolWaveAsync(
            HttpClient client,
            Uri requestUri,
            string deploymentName,
            JsonArray messages,
            AssistantLiveAnswerInput input,
            AssistantRetrievedEvidence evidence,
            CancellationToken cancellationToken
        )
        {
            var roundJson = AssistantLiveAnswerStructuredOutput
                .BuildRetrieveToolsRoundJson(
                    deploymentName,
                    input,
                    _settings.PromptSchemaVersion,
                    messages,
                    allowTools: true
                );
            var response = await SendChatAsync(
                client,
                requestUri,
                roundJson,
                cancellationToken
            );
            if (response.Kind != AttemptKind.Succeeded
                || response.ResponseJson is null)
            {
                return (
                    evidence,
                    response.Result
                        ?? new AssistantLiveAnswerResult.Failed(Retryable: true)
                );
            }

            if (AssistantLiveAnswerStructuredOutput.TryExtractToolCalls(
                    response.ResponseJson,
                    out var toolCalls
                ))
            {
                if (input.OnRetrieveProgress is not null)
                {
                    await input.OnRetrieveProgress(
                        AssistantTurnProgressSteps.Retrieving,
                        cancellationToken
                    );
                }

                var toolResults = await input.ExecuteRetrieveTools!(
                    toolCalls,
                    cancellationToken
                );
                evidence = input.ReadToolEvidence?.Invoke() ?? evidence;
                AssistantLiveAnswerStructuredOutput.AppendAssistantToolCallsMessage(
                    messages,
                    response.ResponseJson
                );
                AssistantLiveAnswerStructuredOutput.AppendToolResultMessages(
                    messages,
                    toolResults
                );
                return (evidence, null);
            }

            if (AssistantLiveAnswerStructuredOutput.TryExtractMessageContent(
                    response.ResponseJson,
                    out var content
                )
                && AssistantLiveAnswerStructuredOutput.TryParseModelContent(
                    content,
                    evidence,
                    input.UserMessage,
                    out var parsed,
                    out _
                )
                && parsed is not null)
            {
                return (evidence, parsed);
            }

            return (evidence, null);
        }

        private static IEnumerable<string> ForcedRetrieveToolCalls(
            AssistantLiveAnswerInput input
        )
        {
            if (input.CompareAll)
            {
                return [AssistantRetrieveToolCatalog.CompareAllLocations];
            }

            if (input.NamedCompare)
            {
                return [AssistantRetrieveToolCatalog.CompareLocations];
            }

            if (!AssistantAskIntent.HasRetrieveAsk(input.UserMessage)
                && !AssistantAttentionAsk.IsAttentionRetrieve(input.UserMessage)
                && !AssistantTaskClassification.LooksLikeCreateTurn(input.UserMessage)
                && !AssistantTaskClassification.LooksLikeRecoveryPath(input.UserMessage))
            {
                return [];
            }

            if (AssistantAttentionAsk.IsAttentionRetrieve(input.UserMessage))
            {
                return AssistantRetrieveToolCatalog.DomainReads;
            }

            return AssistantRetrieveToolCatalog.DomainReadsForFocus(
                AssistantAskFocus.Detect(input.UserMessage)
            );
        }

        private async Task<AttemptResult> SendChatAsync(
            HttpClient client,
            Uri requestUri,
            string body,
            CancellationToken cancellationToken
        )
        {
            const int maxWaits = 3;
            for (var attempt = 0; ; attempt++)
            {
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

                if (IsTransientStatusCode(response.StatusCode) && attempt < maxWaits)
                {
                    var wait = AzureOpenAIChatCompletions.WaitAfterTransient(
                        response,
                        attempt
                    );
                    _logger.LogWarning(
                        "Azure OpenAI returned {StatusCode} for Assistant live answer. Waiting {Seconds} seconds.",
                        (int)response.StatusCode,
                        wait.TotalSeconds
                    );
                    await Task.Delay(wait, cancellationToken);
                    continue;
                }

                if (IsTransientStatusCode(response.StatusCode))
                {
                    _logger.LogWarning(
                        "Azure OpenAI returned {StatusCode} for Assistant live answer",
                        (int)response.StatusCode
                    );
                    return AttemptResult.Transient();
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Azure OpenAI Assistant live answer failed with {StatusCode}",
                        (int)response.StatusCode
                    );
                    return AttemptResult.Failed(
                        new AssistantLiveAnswerResult.Failed(Retryable: true)
                    );
                }

                var responseJson = await response.Content.ReadAsStringAsync(
                    cancellationToken
                );
                return AttemptResult.SucceededJson(responseJson);
            }
        }

        private Uri BuildChatCompletionsUri(string deploymentName)
        {
            var endpoint = _settings.Endpoint.TrimEnd('/') + "/";
            var relative =
                $"openai/deployments/{Uri.EscapeDataString(deploymentName)}"
                + $"/chat/completions?api-version={Uri.EscapeDataString(_settings.ApiVersion)}";

            return new Uri(new Uri(endpoint), relative);
        }


        private static bool IsTransientStatusCode(HttpStatusCode statusCode)
            => statusCode == HttpStatusCode.RequestTimeout
                || statusCode == HttpStatusCode.TooManyRequests
                || (int)statusCode >= 500;


        private enum AttemptKind
        {
            Succeeded,
            Transient,
            InvalidOutput,
            Failed
        }

        private readonly record struct AttemptResult(
            AttemptKind Kind,
            AssistantLiveAnswerResult? Result,
            string? ResponseJson
        )
        {
            public static AttemptResult Succeeded(
                AssistantLiveAnswerResult result
            )
                => new(AttemptKind.Succeeded, result, null);

            public static AttemptResult SucceededJson(string responseJson)
                => new(AttemptKind.Succeeded, null, responseJson);

            public static AttemptResult Transient()
                => new(AttemptKind.Transient, null, null);

            public static AttemptResult InvalidOutput()
                => new(AttemptKind.InvalidOutput, null, null);

            public static AttemptResult Failed(
                AssistantLiveAnswerResult result
            )
                => new(AttemptKind.Failed, result, null);
        }
    }
}
