using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Azure OpenAI Structured Outputs contract for the Assistant live answer.
    /// Fifth use of FeedbackClassification settings. No stream. Feedback retrieve
    /// is passed in the user payload.
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
                ["caveat"] = input.Caveat,
                ["droppedUnknownSentence"] = input.DroppedUnknownSentence,
                ["compareLocations"] = CompareLocationsPayload(input),
                ["feedbackTotalCount"] = input.Evidence.TotalCount,
                ["feedbackSampleCount"] = input.Evidence.SampleCount,
                ["succeededPositive"] = input.Evidence.SucceededPositive,
                ["succeededNeutral"] = input.Evidence.SucceededNeutral,
                ["succeededNegative"] = input.Evidence.SucceededNegative,
                ["needsAttention"] = input.Evidence.NeedsAttention,
                ["discloseSample"] = input.Evidence.DisclosesSample,
                ["tagCounts"] = new JsonArray(
                    input.Evidence.TagCounts
                        .Select(tag => new JsonObject
                        {
                            ["tag"] = tag.Tag,
                            ["count"] = tag.Count,
                        })
                        .ToArray<JsonNode?>()
                ),
                ["rows"] = new JsonArray(
                    input.Evidence.Rows
                        .Select(row => new JsonObject
                        {
                            ["id"] = row.Id,
                            ["createdAt"] = row.CreatedAt.ToString("O"),
                            ["guestName"] = row.GuestName,
                            ["sentiment"] = row.Sentiment,
                            ["classificationStatus"] = row.ClassificationStatus,
                            ["detectedTags"] = new JsonArray(
                                row.DetectedTags.Select(tag => (JsonNode)tag).ToArray()
                            ),
                            ["workflowStatus"] = row.WorkflowStatus,
                            ["needsAttention"] = row.NeedsAttention,
                            ["contactType"] = row.ContactType,
                            ["excerpt"] = row.Excerpt,
                            ["feedbackReference"] = row.FeedbackReference,
                        })
                        .ToArray<JsonNode?>()
                ),
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
                ["required"] = new JsonArray { "answerClass", "title", "body", "actions" },
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
                    },
                    ["actions"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["additionalProperties"] = false,
                            ["required"] = new JsonArray
                            {
                                "type",
                                "tab",
                                "sentiment",
                                "detectedTag",
                                "count",
                            },
                            ["properties"] = new JsonObject
                            {
                                ["type"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["enum"] = new JsonArray(
                                        AssistantActionCatalog.CatalogOrder
                                            .Select(type => (JsonNode)type)
                                            .ToArray()
                                    ),
                                },
                                ["tab"] = NullableString(),
                                ["sentiment"] = NullableString(),
                                ["detectedTag"] = NullableString(),
                                ["count"] = new JsonObject
                                {
                                    ["anyOf"] = new JsonArray
                                    {
                                        new JsonObject { ["type"] = "integer" },
                                        new JsonObject { ["type"] = "null" },
                                    },
                                },
                            },
                        },
                    },
                }
            };

        public static string BuildSystemPrompt(string promptSchemaVersion)
            => $"""
                You write one complete live answer for an operator AI Assistant.
                Prompt/schema version: {promptSchemaVersion}.

                Return Structured Outputs only. Do not stream.
                Every restaurant claim must come from the Feedback evidence in the
                user payload. Re-retrieve is already done. Prior assistant text is
                not evidence. Vague time words map to the current Reporting period.
                Title and body must use periodPhrase. Do not write a hard-coded
                "this week".

                Ground only on Feedback, including AI classification. Do not answer
                Help Centre or product how-to. Do not add general restaurant advice.
                Never invent guest email, phone, or other guest PII.
                Never invent counts. Put counts in the body. No citation footer.
                If discloseSample is true, the body must say themes come from
                sampleCount of feedbackTotalCount.

                Empty evidence is a grounded empty answer: title and body name the
                Owned location and Reporting period. No Actions.

                Mutate asks (create, send, or change records) are a refusal: body
                only, no Actions, no claim the record changed.
                Mixed ask: ground the in-scope Feedback part and add one refuse
                sentence for the out part. Class is grounded if any in-scope facts
                were retrieved.

                Actions: choose typed rows only. Do not invent labels or destinations.
                Max three. Catalog order. At most one per type. Navigate only.
                view-feedback-set and prepare-recovery are Feedback evidence Actions.
                view-campaigns and view-offers may appear as next-step when the
                answer recommends that flow.

                When compareLocations has two or more rows, write one Compare turn
                over periodPhrase. Use each row's evidence only for that location.
                Include droppedUnknownSentence and caveat when present. Actions
                must use the saved Analysis scope evidence only (the top-level
                counts), not extra compare locations.
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
            AssistantFeedbackEvidence evidence,
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

                var proposed = ParseActions(root);
                var actions = AssistantActionCatalog.Validate(
                    proposed,
                    answerClass,
                    evidence
                );

                result = new AssistantLiveAnswerResult.Succeeded(
                    answerClass,
                    title,
                    body,
                    actions
                );
                return true;
            }
        }

        private static JsonArray CompareLocationsPayload(AssistantLiveAnswerInput input)
        {
            var rows = input.CompareLocations ?? [];
            return new JsonArray(
                rows.Select(row => new JsonObject
                {
                    ["ownedLocationName"] = row.LocationName,
                    ["capturePaused"] =
                        row.CaptureStatus == CaptureLocationStatus.Paused,
                    ["feedbackTotalCount"] = row.Evidence.TotalCount,
                    ["feedbackSampleCount"] = row.Evidence.SampleCount,
                    ["succeededPositive"] = row.Evidence.SucceededPositive,
                    ["succeededNeutral"] = row.Evidence.SucceededNeutral,
                    ["succeededNegative"] = row.Evidence.SucceededNegative,
                    ["needsAttention"] = row.Evidence.NeedsAttention,
                    ["discloseSample"] = row.Evidence.DisclosesSample,
                }).ToArray<JsonNode?>()
            );
        }

        private static JsonObject NullableString()
            => new()
            {
                ["anyOf"] = new JsonArray
                {
                    new JsonObject { ["type"] = "string" },
                    new JsonObject { ["type"] = "null" },
                },
            };

        private static List<AssistantActionDto> ParseActions(JsonElement root)
        {
            if (!root.TryGetProperty("actions", out var actionsElement)
                || actionsElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var actions = new List<AssistantActionDto>();
            foreach (var item in actionsElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object
                    || !item.TryGetProperty("type", out var typeElement)
                    || typeElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                actions.Add(
                    new AssistantActionDto
                    {
                        Type = typeElement.GetString() ?? string.Empty,
                        Tab = ReadNullableString(item, "tab"),
                        Sentiment = ReadNullableString(item, "sentiment"),
                        DetectedTag = ReadNullableString(item, "detectedTag"),
                        Count = ReadNullableInt(item, "count"),
                    }
                );
            }

            return actions;
        }

        private static string? ReadNullableString(JsonElement item, string name)
        {
            if (!item.TryGetProperty(name, out var element)
                || element.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var value = element.GetString()?.Trim();
            return string.IsNullOrEmpty(value) ? null : value;
        }

        private static int? ReadNullableInt(JsonElement item, string name)
        {
            if (!item.TryGetProperty(name, out var element)
                || element.ValueKind != JsonValueKind.Number
                || !element.TryGetInt32(out var value))
            {
                return null;
            }

            return value;
        }
    }
}
