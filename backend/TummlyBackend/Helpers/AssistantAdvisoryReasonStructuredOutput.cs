using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Azure OpenAI Structured Outputs for advisory Clear → Reason.
    /// Own prompt and schema — not the live-answer contract.
    /// </summary>
    public static class AssistantAdvisoryReasonStructuredOutput
    {
        public const string SchemaName = "assistant_advisory_reason";

        public const string HttpClientName = "AzureOpenAIAssistantAdvisoryReason";

        public const string PromptSchemaRevision = "2026-09-05";

        public const string AdviceOnlyAction = "advice-only";

        private static readonly HashSet<string> AllowedAnswerTypes =
            new(StringComparer.Ordinal)
            {
                "direct",
                "advisory",
                "product_expert",
                "clarify",
            };

        private static readonly HashSet<string> AllowedConfidence =
            new(StringComparer.Ordinal) { "high", "medium", "low" };

        private static readonly HashSet<string> AllowedActions =
            new(StringComparer.Ordinal)
            {
                AdviceOnlyAction,
                AssistantTask.Retrieve,
                AssistantTask.CreateCampaignDraft,
                AssistantTask.CreateCampaignWithOffer,
                AssistantTask.OfferPath,
                AssistantTask.RecoveryPath,
                AssistantTask.Refuse,
            };

        private static readonly JsonSerializerOptions RequestJsonOptions = new()
        {
            WriteIndented = false,
        };

        private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static string BuildRequestJson(
            string deploymentName,
            AssistantAdvisoryReasonInput input,
            string promptSchemaVersion
        )
        {
            var userPayload = new JsonObject
            {
                ["userMessage"] = input.UserMessage,
                ["snapshot"] = JsonNode.Parse(
                    JsonSerializer.Serialize(input.Snapshot, SnapshotJsonOptions)
                ),
            };

            var request = new JsonObject
            {
                ["model"] = deploymentName,
                ["messages"] = BuildMessages(
                    BuildSystemPrompt(promptSchemaVersion),
                    input,
                    userPayload
                ),
                ["response_format"] = new JsonObject
                {
                    ["type"] = "json_schema",
                    ["json_schema"] = new JsonObject
                    {
                        ["name"] = SchemaName,
                        ["strict"] = true,
                        ["schema"] = BuildSchema(),
                    },
                },
            };

            return request.ToJsonString(RequestJsonOptions);
        }

        private static JsonArray BuildMessages(
            string systemPrompt,
            AssistantAdvisoryReasonInput input,
            JsonObject userPayload
        )
        {
            var messages = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "system",
                    ["content"] = systemPrompt,
                },
            };

            if (input.History is { Count: > 0 } history)
            {
                foreach (var turn in history)
                {
                    messages.Add(
                        new JsonObject
                        {
                            ["role"] = turn.Role.ToWireString(),
                            ["content"] = turn.Body,
                        }
                    );
                }
            }

            messages.Add(
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = userPayload.ToJsonString(RequestJsonOptions),
                }
            );

            return messages;
        }

        public static JsonObject BuildSchema()
            => new()
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["required"] = new JsonArray
                {
                    "answer_type",
                    "summary",
                    "clarifying_question",
                    "recommendations",
                    "evidence_used",
                },
                ["properties"] = new JsonObject
                {
                    ["answer_type"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray(
                            "direct",
                            "advisory",
                            "product_expert",
                            "clarify"
                        ),
                    },
                    ["summary"] = new JsonObject { ["type"] = "string" },
                    ["clarifying_question"] = NullableString(),
                    ["recommendations"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["additionalProperties"] = false,
                            ["required"] = new JsonArray
                            {
                                "action",
                                "headline",
                                "reason",
                                "evidence_ref",
                                "confidence",
                            },
                            ["properties"] = new JsonObject
                            {
                                ["action"] = new JsonObject
                                {
                                    ["type"] = "string",
                                },
                                ["headline"] = new JsonObject
                                {
                                    ["type"] = "string",
                                },
                                ["reason"] = new JsonObject
                                {
                                    ["type"] = "string",
                                },
                                ["evidence_ref"] = new JsonObject
                                {
                                    ["type"] = "array",
                                    ["items"] = new JsonObject
                                    {
                                        ["type"] = "string",
                                    },
                                },
                                ["confidence"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["enum"] = new JsonArray(
                                        "high",
                                        "medium",
                                        "low"
                                    ),
                                },
                            },
                        },
                    },
                    ["evidence_used"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject { ["type"] = "string" },
                    },
                },
            };

        public static string BuildSystemPrompt(string promptSchemaVersion)
            => $"""
                You are the Tummly advisory Reason model for UK hospitality operators.
                Prompt/schema version: {promptSchemaVersion}.
                Revision: {PromptSchemaRevision}.

                Diagnose from the fed snapshot flags, metric deltas, and section
                summaries only. Never invent metrics, counts, or guest facts that
                are not in the snapshot. Do not include guest names, emails,
                phones, or raw feedback text.

                Return Structured Outputs only with answer_type one of:
                direct, advisory, product_expert, clarify.

                direct or product_expert: recommendations must be [].
                clarify: recommendations must be [] and clarifying_question must
                be a non-empty question.
                advisory: recommendations may list grounded next steps.
                Each recommendation action must be an AssistantTask value or
                "{AdviceOnlyAction}". Every evidence_ref must name a snapshot
                path that exists (for example Account.Covers or Feedback.Flags).
                evidence_used lists the section roots or paths you used.
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
            out AssistantAdvisoryReasonOutput? output,
            out bool invalidOutput
        )
        {
            output = null;
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
                if (!root.TryGetProperty("answer_type", out var typeElement)
                    || typeElement.ValueKind != JsonValueKind.String)
                {
                    invalidOutput = true;
                    return false;
                }

                var answerType = typeElement.GetString()?.Trim() ?? string.Empty;
                if (!AllowedAnswerTypes.Contains(answerType))
                {
                    invalidOutput = true;
                    return false;
                }

                if (!root.TryGetProperty("summary", out var summaryElement)
                    || summaryElement.ValueKind != JsonValueKind.String)
                {
                    invalidOutput = true;
                    return false;
                }

                var summary = summaryElement.GetString()?.Trim() ?? string.Empty;
                if (summary.Length == 0)
                {
                    invalidOutput = true;
                    return false;
                }

                string? clarifyingQuestion = null;
                if (root.TryGetProperty("clarifying_question", out var clarifyElement))
                {
                    if (clarifyElement.ValueKind == JsonValueKind.String)
                    {
                        var value = clarifyElement.GetString()?.Trim();
                        clarifyingQuestion = string.IsNullOrWhiteSpace(value)
                            ? null
                            : value;
                    }
                    else if (clarifyElement.ValueKind != JsonValueKind.Null)
                    {
                        invalidOutput = true;
                        return false;
                    }
                }

                if (!TryReadStringArray(root, "evidence_used", out var evidenceUsed))
                {
                    invalidOutput = true;
                    return false;
                }

                if (!TryReadRecommendations(root, out var recommendations))
                {
                    invalidOutput = true;
                    return false;
                }

                output = new AssistantAdvisoryReasonOutput(
                    AnswerType: answerType,
                    Summary: FeedbackRecoveryDraftStructuredOutput
                        .SanitizeGuestProse(summary)
                        .Trim(),
                    ClarifyingQuestion: clarifyingQuestion is null
                        ? null
                        : FeedbackRecoveryDraftStructuredOutput
                            .SanitizeGuestProse(clarifyingQuestion)
                            .Trim(),
                    Recommendations: recommendations,
                    EvidenceUsed: evidenceUsed
                );
                return true;
            }
        }

        public static bool IsAllowedAnswerType(string answerType)
            => AllowedAnswerTypes.Contains(answerType);

        public static bool IsAllowedAction(string action)
            => AllowedActions.Contains(action);

        private static bool TryReadRecommendations(
            JsonElement root,
            out List<AssistantAdvisoryReasonRecommendation> recommendations
        )
        {
            recommendations = [];
            if (!root.TryGetProperty("recommendations", out var element)
                || element.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                var action = ReadRequiredString(item, "action");
                var headline = ReadRequiredString(item, "headline");
                var reason = ReadRequiredString(item, "reason");
                var confidence = ReadRequiredString(item, "confidence");
                if (action is null
                    || headline is null
                    || reason is null
                    || confidence is null
                    || !AllowedConfidence.Contains(confidence)
                    || !AllowedActions.Contains(action)
                    || !TryReadStringArray(item, "evidence_ref", out var evidenceRef))
                {
                    return false;
                }

                recommendations.Add(
                    new AssistantAdvisoryReasonRecommendation(
                        Action: action,
                        Headline: FeedbackRecoveryDraftStructuredOutput
                            .SanitizeGuestProse(headline)
                            .Trim(),
                        Reason: FeedbackRecoveryDraftStructuredOutput
                            .SanitizeGuestProse(reason)
                            .Trim(),
                        EvidenceRef: evidenceRef,
                        Confidence: confidence
                    )
                );
            }

            return true;
        }

        private static bool TryReadStringArray(
            JsonElement root,
            string propertyName,
            out List<string> values
        )
        {
            values = [];
            if (!root.TryGetProperty(propertyName, out var element)
                || element.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                {
                    return false;
                }

                var value = item.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }

            return true;
        }

        private static string? ReadRequiredString(
            JsonElement root,
            string propertyName
        )
        {
            if (!root.TryGetProperty(propertyName, out var element)
                || element.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var value = element.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
    }
}
