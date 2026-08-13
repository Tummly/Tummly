using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Azure OpenAI Structured Outputs contract for the Assistant live answer.
    /// Fifth use of FeedbackClassification settings. No stream. No retrieve yet.
    /// </summary>
    public static class AssistantLiveAnswerStructuredOutput
    {
        public const string SchemaName = "assistant_live_answer";

        public const string HttpClientName = "AzureOpenAIAssistantLiveAnswer";

        private static readonly JsonSerializerOptions RequestJsonOptions = new()
        {
            WriteIndented = false
        };

        public static string BuildRequestJson(
            string deploymentName,
            AssistantLiveAnswerInput input,
            string promptSchemaVersion
        )
        {
            var userPayload = new JsonObject
            {
                ["userMessage"] = input.UserMessage,
                ["ownedLocationName"] = input.OwnedLocationName,
                ["periodPhrase"] = input.PeriodPhrase,
            };

            var request = new JsonObject
            {
                ["model"] = deploymentName,
                ["messages"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["role"] = "system",
                        ["content"] = BuildSystemPrompt(promptSchemaVersion)
                    },
                    new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = userPayload.ToJsonString(RequestJsonOptions)
                    }
                },
                ["response_format"] = new JsonObject
                {
                    ["type"] = "json_schema",
                    ["json_schema"] = new JsonObject
                    {
                        ["name"] = SchemaName,
                        ["strict"] = true,
                        ["schema"] = BuildSchema()
                    }
                }
            };

            return request.ToJsonString(RequestJsonOptions);
        }

        public static JsonObject BuildSchema()
            => new()
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["required"] = new JsonArray { "answerClass", "title", "body" },
                ["properties"] = new JsonObject
                {
                    ["answerClass"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray
                        {
                            "grounded",
                            "refusal",
                            "failure",
                            "clarify"
                        }
                    },
                    ["title"] = new JsonObject
                    {
                        ["anyOf"] = new JsonArray
                        {
                            new JsonObject { ["type"] = "string" },
                            new JsonObject { ["type"] = "null" }
                        }
                    },
                    ["body"] = new JsonObject
                    {
                        ["type"] = "string"
                    }
                }
            };

        public static string BuildSystemPrompt(string promptSchemaVersion)
            => $"""
                You write one complete live answer for an operator AI Assistant.
                Prompt/schema version: {promptSchemaVersion}.

                Return Structured Outputs only. Do not stream.
                This turn has no restaurant retrieve. Return a grounded stub:
                title and body must name the Owned location and Reporting period
                from the user payload. Do not invent restaurant facts or counts.
                Do not answer Help Centre / product how-to.
                Never invent guest email, phone, or other guest PII.
                answerClass must be grounded. Title must be non-empty.
                """;

        public static bool TryExtractMessageContent(
            string responseJson,
            out string? content
        )
            => FeedbackClassificationStructuredOutput.TryExtractMessageContent(
                responseJson,
                out content
            );

        public static bool TryParseModelContent(
            string? content,
            out AssistantLiveAnswerResult? result,
            out bool invalidOutput
        )
        {
            result = null;
            invalidOutput = false;

            if (string.IsNullOrWhiteSpace(content))
            {
                invalidOutput = true;
                return false;
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(content);
            }
            catch (JsonException)
            {
                invalidOutput = true;
                return false;
            }

            using (document)
            {
                var root = document.RootElement;
                if (!root.TryGetProperty("answerClass", out var classElement)
                    || classElement.ValueKind != JsonValueKind.String
                    || !root.TryGetProperty("body", out var bodyElement)
                    || bodyElement.ValueKind != JsonValueKind.String)
                {
                    invalidOutput = true;
                    return false;
                }

                var answerClassWire = classElement.GetString();
                AssistantMessageClass answerClass;
                try
                {
                    answerClass = AssistantMessageClassExtensions.FromWireString(
                        answerClassWire ?? string.Empty
                    );
                }
                catch (ArgumentOutOfRangeException)
                {
                    invalidOutput = true;
                    return false;
                }

                var body = bodyElement.GetString()?.Trim() ?? string.Empty;
                if (body.Length == 0)
                {
                    invalidOutput = true;
                    return false;
                }

                string? title = null;
                if (root.TryGetProperty("title", out var titleElement)
                    && titleElement.ValueKind == JsonValueKind.String)
                {
                    var rawTitle = titleElement.GetString()?.Trim();
                    title = string.IsNullOrEmpty(rawTitle) ? null : rawTitle;
                }

                if (answerClass == AssistantMessageClass.Grounded && title is null)
                {
                    invalidOutput = true;
                    return false;
                }

                if (answerClass != AssistantMessageClass.Grounded)
                {
                    title = null;
                }

                result = new AssistantLiveAnswerResult.Succeeded(
                    answerClass,
                    title,
                    body
                );
                return true;
            }
        }
    }
}
