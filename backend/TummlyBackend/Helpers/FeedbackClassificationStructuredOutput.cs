using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Azure OpenAI Structured Outputs contract for Feedback classification.
    /// </summary>
    public static class FeedbackClassificationStructuredOutput
    {
        public const string SchemaName = "feedback_classification";

        public const string HttpClientName = "AzureOpenAIFeedbackClassification";

        private static readonly string[] SentimentValues =
        [
            "positive",
            "neutral",
            "negative"
        ];

        private static readonly string[] DetectedIssueValues =
            Enum.GetNames<DetectedIssue>();

        private static readonly JsonSerializerOptions RequestJsonOptions = new()
        {
            WriteIndented = false
        };

        public static string BuildRequestJson(
            string deploymentName,
            string comment,
            string promptSchemaVersion,
            string? region = null
        )
        {
            var request = new JsonObject
            {
                ["model"] = deploymentName,
                ["messages"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["role"] = "system",
                        ["content"] = BuildSystemPrompt(
                            promptSchemaVersion,
                            region
                        )
                    },
                    new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = comment
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
        {
            var sentimentEnum = new JsonArray();
            foreach (var value in SentimentValues)
            {
                sentimentEnum.Add(value);
            }

            var issueEnum = new JsonArray();
            foreach (var value in DetectedIssueValues)
            {
                issueEnum.Add(value);
            }

            return new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["required"] = new JsonArray
                {
                    "outcome",
                    "sentiment",
                    "detectedIssues"
                },
                ["properties"] = new JsonObject
                {
                    ["outcome"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray
                        {
                            "classified",
                            "unsupported_language"
                        }
                    },
                    ["sentiment"] = new JsonObject
                    {
                        ["anyOf"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["type"] = "string",
                                ["enum"] = sentimentEnum
                            },
                            new JsonObject
                            {
                                ["type"] = "null"
                            }
                        }
                    },
                    ["detectedIssues"] = new JsonObject
                    {
                        ["anyOf"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["type"] = "array",
                                ["items"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["enum"] = issueEnum
                                }
                            },
                            new JsonObject
                            {
                                ["type"] = "null"
                            }
                        }
                    }
                }
            };
        }

        public static string BuildSystemPrompt(
            string promptSchemaVersion,
            string? region = null
        )
        {
            var regionLine = string.IsNullOrWhiteSpace(region)
                ? string.Empty
                : $"Configured Azure region/residency: {region.Trim()}.";

            return $"""
                You classify UK hospitality guest Feedback comments.
                Prompt/schema version: {promptSchemaVersion}.
                {regionLine}

                Return Structured Outputs only.
                outcome must be "classified" or "unsupported_language".

                Use unsupported_language when the comment is clearly not English,
                or English is not the clear majority. Do not guess tags then.

                Prefer classified for short or emoji-only comments
                (often Neutral sentiment with no detected issues).

                When classified:
                - sentiment is positive, neutral, or negative
                - detectedIssues is a multi-label set of problem themes only
                  (may be empty). Themes are independent of sentiment.
                - Other is exclusive: never combine Other with any other theme.

                Detected-issue keys: {string.Join(", ", DetectedIssueValues)}.
                """;
        }

        public static bool TryParseModelContent(
            string? content,
            out FeedbackClassificationResult? result,
            out bool unsupportedLanguage,
            out bool invalidOutput
        )
        {
            result = null;
            unsupportedLanguage = false;
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
                if (!root.TryGetProperty("outcome", out var outcomeElement)
                    || outcomeElement.ValueKind != JsonValueKind.String)
                {
                    invalidOutput = true;
                    return false;
                }

                var outcome = outcomeElement.GetString();
                if (outcome == "unsupported_language")
                {
                    unsupportedLanguage = true;
                    result = new FeedbackClassificationResult.Failed();
                    return true;
                }

                if (outcome != "classified")
                {
                    invalidOutput = true;
                    return false;
                }

                if (!TryParseSentiment(root, out var sentiment)
                    || !TryParseDetectedIssues(root, out var issues))
                {
                    invalidOutput = true;
                    return false;
                }

                if (issues.Contains(DetectedIssue.Other) && issues.Count > 1)
                {
                    invalidOutput = true;
                    return false;
                }

                result = new FeedbackClassificationResult.Succeeded(
                    sentiment,
                    issues
                );
                return true;
            }
        }

        public static bool TryExtractMessageContent(
            string responseJson,
            out string? content
        )
        {
            content = null;

            try
            {
                using var document = JsonDocument.Parse(responseJson);
                var root = document.RootElement;
                if (!root.TryGetProperty("choices", out var choices)
                    || choices.ValueKind != JsonValueKind.Array
                    || choices.GetArrayLength() == 0)
                {
                    return false;
                }

                var message = choices[0].GetProperty("message");
                if (!message.TryGetProperty("content", out var contentElement)
                    || contentElement.ValueKind != JsonValueKind.String)
                {
                    return false;
                }

                content = contentElement.GetString();
                return content is not null;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static bool TryParseSentiment(
            JsonElement root,
            out FeedbackSentiment sentiment
        )
        {
            sentiment = default;

            if (!root.TryGetProperty("sentiment", out var element)
                || element.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            sentiment = element.GetString() switch
            {
                "positive" => FeedbackSentiment.Positive,
                "neutral" => FeedbackSentiment.Neutral,
                "negative" => FeedbackSentiment.Negative,
                _ => default
            };

            return element.GetString() is "positive" or "neutral" or "negative";
        }

        private static bool TryParseDetectedIssues(
            JsonElement root,
            out IReadOnlyList<DetectedIssue> issues
        )
        {
            issues = Array.Empty<DetectedIssue>();

            if (!root.TryGetProperty("detectedIssues", out var element))
            {
                return false;
            }

            if (element.ValueKind == JsonValueKind.Null)
            {
                return false;
            }

            if (element.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var parsed = new List<DetectedIssue>();
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String
                    || !Enum.TryParse<DetectedIssue>(
                        item.GetString(),
                        ignoreCase: false,
                        out var issue
                    ))
                {
                    return false;
                }

                if (!parsed.Contains(issue))
                {
                    parsed.Add(issue);
                }
            }

            issues = parsed;
            return true;
        }
    }
}
