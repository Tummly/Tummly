using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// One parallel Azure tool wave, then a forced Structured Outputs final
    /// round. Shared by Campaign/Recovery drafts, Weekly Brief, and
    /// recommendation providers. Assistant live answer keeps its own early-exit
    /// rules but reuses <see cref="AzureOpenAIChatCompletions"/>.
    /// </summary>
    public static class AzureOpenAIOneWaveToolLoop
    {
        public static async Task<AzureOpenAIOneWaveResult> RunAsync(
            AzureOpenAIOneWaveRequest request,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.ExecuteTools is null)
            {
                request.Logger.LogError(
                    "{Surface} tool loop enabled but ExecuteTools is null.",
                    request.SurfaceName
                );
                return new AzureOpenAIOneWaveResult.Failed(Retryable: true);
            }

            var requestUri = AzureOpenAIChatCompletions.BuildChatCompletionsUri(
                request.Endpoint,
                request.DeploymentName,
                request.ApiVersion
            );
            var messages = request.SeedMessages;

            var round1Json = BuildRoundJson(
                request,
                messages,
                allowTools: true
            );
            var round1 = await AzureOpenAIChatCompletions.SendAsync(
                request.HttpClient,
                requestUri,
                request.ApiKey,
                round1Json,
                cancellationToken
            );
            if (round1 is AzureOpenAIChatSendResult.Transient transient1)
            {
                request.Logger.LogWarning(
                    "Azure OpenAI returned {StatusCode} for {Surface} tool round",
                    transient1.StatusCode,
                    request.SurfaceName
                );
                return new AzureOpenAIOneWaveResult.Transient();
            }

            if (round1 is not AzureOpenAIChatSendResult.Succeeded succeeded1)
            {
                var failedStatus = round1 is AzureOpenAIChatSendResult.Failed failed1
                    ? failed1.StatusCode
                    : 0;
                request.Logger.LogError(
                    "Azure OpenAI {Surface} tool round failed with {StatusCode}",
                    request.SurfaceName,
                    failedStatus
                );
                return new AzureOpenAIOneWaveResult.Failed(Retryable: true);
            }

            IReadOnlyList<AssistantToolCallRequest> toolCalls;
            if (AzureOpenAIChatCompletions.TryExtractToolCalls(
                    succeeded1.ResponseJson,
                    out var modelCalls
                )
                && modelCalls.Count > 0)
            {
                toolCalls = modelCalls;
                AzureOpenAIChatCompletions.AppendAssistantToolCallsMessage(
                    messages,
                    succeeded1.ResponseJson
                );
            }
            else
            {
                toolCalls = request.ForcedToolCalls ?? [];
                if (toolCalls.Count == 0)
                {
                    // No tools called and none forced — still run structured final.
                    return await RunFinalRoundAsync(
                        request,
                        requestUri,
                        messages,
                        cancellationToken
                    );
                }

                AzureOpenAIChatCompletions.AppendForcedToolCallsMessage(
                    messages,
                    toolCalls
                );
            }

            var toolResults = await request.ExecuteTools(toolCalls, cancellationToken);
            AzureOpenAIChatCompletions.AppendToolResultMessages(messages, toolResults);

            return await RunFinalRoundAsync(
                request,
                requestUri,
                messages,
                cancellationToken
            );
        }

        private static async Task<AzureOpenAIOneWaveResult> RunFinalRoundAsync(
            AzureOpenAIOneWaveRequest request,
            Uri requestUri,
            JsonArray messages,
            CancellationToken cancellationToken
        )
        {
            var round2Json = BuildRoundJson(
                request,
                messages,
                allowTools: false
            );
            var round2 = await AzureOpenAIChatCompletions.SendAsync(
                request.HttpClient,
                requestUri,
                request.ApiKey,
                round2Json,
                cancellationToken
            );
            if (round2 is AzureOpenAIChatSendResult.Transient transient2)
            {
                request.Logger.LogWarning(
                    "Azure OpenAI returned {StatusCode} for {Surface} final round",
                    transient2.StatusCode,
                    request.SurfaceName
                );
                return new AzureOpenAIOneWaveResult.Transient();
            }

            if (round2 is not AzureOpenAIChatSendResult.Succeeded succeeded2)
            {
                var failedStatus = round2 is AzureOpenAIChatSendResult.Failed failed2
                    ? failed2.StatusCode
                    : 0;
                request.Logger.LogError(
                    "Azure OpenAI {Surface} final round failed with {StatusCode}",
                    request.SurfaceName,
                    failedStatus
                );
                return new AzureOpenAIOneWaveResult.Failed(Retryable: true);
            }

            if (!AzureOpenAIChatCompletions.TryExtractMessageContent(
                    succeeded2.ResponseJson,
                    out var content
                )
                || string.IsNullOrWhiteSpace(content))
            {
                return new AzureOpenAIOneWaveResult.InvalidOutput();
            }

            return new AzureOpenAIOneWaveResult.Succeeded(content!);
        }

        private static string BuildRoundJson(
            AzureOpenAIOneWaveRequest request,
            JsonArray messages,
            bool allowTools
        )
            => BuildRoundRequestJson(
                request.DeploymentName,
                messages,
                allowTools,
                request.ToolsArray,
                request.SchemaName,
                request.FinalSchema,
                request.MaxCompletionTokens
            );

        /// <summary>
        /// Builds the chat-completions body for one round. Public for contract tests
        /// (final round must not send <c>tool_choice</c> without <c>tools</c>).
        /// </summary>
        public static string BuildRoundRequestJson(
            string deploymentName,
            JsonArray messages,
            bool allowTools,
            JsonArray toolsArray,
            string schemaName,
            JsonObject finalSchema,
            int? maxCompletionTokens = null
        )
        {
            var body = new JsonObject
            {
                ["model"] = deploymentName,
                ["messages"] = messages.DeepClone(),
            };

            if (maxCompletionTokens is int maxTokens)
            {
                body["max_completion_tokens"] = maxTokens;
            }

            if (allowTools)
            {
                body["tools"] = toolsArray.DeepClone();
                body["tool_choice"] = "auto";
                body["parallel_tool_calls"] = true;
            }
            else
            {
                // Do not send tool_choice without tools — Azure/OpenAI returns 400
                // ("tool_choice is only allowed when tools are specified").
                body["response_format"] = new JsonObject
                {
                    ["type"] = "json_schema",
                    ["json_schema"] = new JsonObject
                    {
                        ["name"] = schemaName,
                        ["strict"] = true,
                        ["schema"] = finalSchema.DeepClone(),
                    },
                };
            }

            return body.ToJsonString(AzureOpenAIChatCompletions.RequestJsonOptions);
        }
    }

    public sealed class AzureOpenAIOneWaveRequest
    {
        public required string SurfaceName { get; init; }

        public required HttpClient HttpClient { get; init; }

        public required string Endpoint { get; init; }

        public required string ApiKey { get; init; }

        public required string DeploymentName { get; init; }

        public required string ApiVersion { get; init; }

        public required JsonArray SeedMessages { get; init; }

        public required JsonArray ToolsArray { get; init; }

        public required string SchemaName { get; init; }

        public required JsonObject FinalSchema { get; init; }

        public required Func<
            IReadOnlyList<AssistantToolCallRequest>,
            CancellationToken,
            Task<IReadOnlyList<AssistantToolCallResult>>
        > ExecuteTools
        {
            get;
            init;
        }

        public required ILogger Logger { get; init; }

        public IReadOnlyList<AssistantToolCallRequest>? ForcedToolCalls { get; init; }

        public int? MaxCompletionTokens { get; init; }
    }

    public abstract record AzureOpenAIOneWaveResult
    {
        private AzureOpenAIOneWaveResult()
        {
        }

        public sealed record Succeeded(string Content) : AzureOpenAIOneWaveResult;

        public sealed record Transient() : AzureOpenAIOneWaveResult;

        public sealed record InvalidOutput() : AzureOpenAIOneWaveResult;

        public sealed record Failed(bool Retryable = true) : AzureOpenAIOneWaveResult;
    }
}
