using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Shared Azure OpenAI chat-completions HTTP helpers for one-wave tool
    /// loops and Structured Outputs across Assistant and other AI surfaces.
    /// </summary>
    public static class AzureOpenAIChatCompletions
    {
        public static readonly JsonSerializerOptions RequestJsonOptions = new()
        {
            WriteIndented = false,
        };

        public static Uri BuildChatCompletionsUri(
            string endpoint,
            string deploymentName,
            string apiVersion
        )
        {
            var baseEndpoint = endpoint.TrimEnd('/') + "/";
            var relative =
                $"openai/deployments/{Uri.EscapeDataString(deploymentName)}"
                + $"/chat/completions?api-version={Uri.EscapeDataString(apiVersion)}";
            return new Uri(new Uri(baseEndpoint), relative);
        }

        public static async Task<AzureOpenAIChatSendResult> SendAsync(
            HttpClient client,
            Uri requestUri,
            string apiKey,
            string body,
            CancellationToken cancellationToken
        )
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.TryAddWithoutValidation("api-key", apiKey);
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
                return new AzureOpenAIChatSendResult.Transient((int)response.StatusCode);
            }

            if (!response.IsSuccessStatusCode)
            {
                return new AzureOpenAIChatSendResult.Failed((int)response.StatusCode);
            }

            var responseJson = await response.Content.ReadAsStringAsync(
                cancellationToken
            );
            return new AzureOpenAIChatSendResult.Succeeded(responseJson);
        }

        public static bool IsTransientStatusCode(HttpStatusCode statusCode)
            => statusCode == HttpStatusCode.RequestTimeout
                || statusCode == HttpStatusCode.TooManyRequests
                || (int)statusCode >= 500;

        public static bool IsTransientException(Exception ex)
            => ex is HttpRequestException or TaskCanceledException;

        public static bool TryExtractToolCalls(
            string responseJson,
            out IReadOnlyList<AssistantToolCallRequest> toolCalls
        )
            => AssistantLiveAnswerStructuredOutput.TryExtractToolCalls(
                responseJson,
                out toolCalls
            );

        public static void AppendAssistantToolCallsMessage(
            JsonArray messages,
            string responseJson
        )
            => AssistantLiveAnswerStructuredOutput.AppendAssistantToolCallsMessage(
                messages,
                responseJson
            );

        public static void AppendToolResultMessages(
            JsonArray messages,
            IReadOnlyList<AssistantToolCallResult> results
        )
            => AssistantLiveAnswerStructuredOutput.AppendToolResultMessages(
                messages,
                results
            );

        public static void AppendForcedToolCallsMessage(
            JsonArray messages,
            IReadOnlyList<AssistantToolCallRequest> toolCalls
        )
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

        public static bool TryExtractMessageContent(
            string responseJson,
            out string? content
        )
            => FeedbackClassificationStructuredOutput.TryExtractMessageContent(
                responseJson,
                out content
            );
    }

    public abstract record AzureOpenAIChatSendResult
    {
        private AzureOpenAIChatSendResult()
        {
        }

        public sealed record Succeeded(string ResponseJson) : AzureOpenAIChatSendResult;

        public sealed record Transient(int StatusCode) : AzureOpenAIChatSendResult;

        public sealed record Failed(int StatusCode) : AzureOpenAIChatSendResult;
    }
}
