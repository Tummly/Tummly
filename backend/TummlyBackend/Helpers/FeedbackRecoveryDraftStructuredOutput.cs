using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Azure OpenAI Structured Outputs contract for Feedback recovery drafts.
    /// Prompt text is implementation-default (not locked in product copy).
    /// </summary>
    public static class FeedbackRecoveryDraftStructuredOutput
    {
        public const string SchemaName = "feedback_recovery_draft";

        public const string HttpClientName = "AzureOpenAIFeedbackRecoveryDraft";

        private static readonly JsonSerializerOptions RequestJsonOptions = new()
        {
            WriteIndented = false
        };

        public static string BuildRequestJson(
            string deploymentName,
            FeedbackRecoveryDraftInput input,
            string promptSchemaVersion
        )
        {
            var userPayload = new JsonObject
            {
                ["feedbackComment"] = input.FeedbackComment,
                ["sentiment"] = input.Sentiment,
                ["issueTags"] = new JsonArray(
                    input.IssueTags
                        .Select(t => (JsonNode?)JsonValue.Create(t))
                        .ToArray()
                ),
                ["guestDisplayName"] = input.GuestDisplayName,
                ["locationName"] = input.LocationName,
                ["channel"] = input.Channel,
                ["purpose"] = input.Purpose,
                ["tone"] = input.Tone,
                ["includeNotes"] = input.IncludeNotes,
                ["mode"] = input.Mode,
                ["currentBody"] = input.CurrentBody,
                ["currentSubject"] = input.CurrentSubject,
                ["confirmedInternalActionCategory"] =
                    input.ConfirmedInternalActionCategory,
                ["confirmedInternalActionNote"] =
                    input.ConfirmedInternalActionNote,
            };

            if (input.ConfirmedOffer is { } offer)
            {
                userPayload["confirmedOffer"] = new JsonObject
                {
                    ["offerType"] = offer.OfferType,
                    ["title"] = offer.Title,
                    ["description"] = offer.Description,
                    ["validity"] = offer.Validity,
                    ["expiryDate"] = offer.ExpiryDate,
                    ["discountPercentage"] = offer.DiscountPercentage.HasValue
                        ? JsonValue.Create(offer.DiscountPercentage.Value)
                        : null,
                    ["discountAmount"] = offer.DiscountAmount.HasValue
                        ? JsonValue.Create(offer.DiscountAmount.Value)
                        : null,
                    ["freeItemText"] = offer.FreeItemText,
                    ["purchaseRequirement"] = offer.PurchaseRequirement,
                    ["minimumSpend"] = offer.MinimumSpend.HasValue
                        ? JsonValue.Create(offer.MinimumSpend.Value)
                        : null,
                    ["additionalExclusions"] = offer.AdditionalExclusions,
                    ["replacementItemText"] = offer.ReplacementItemText,
                };
            }

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
                ["required"] = new JsonArray { "body", "subject", "channel" },
                ["properties"] = new JsonObject
                {
                    ["body"] = new JsonObject
                    {
                        ["type"] = "string"
                    },
                    ["subject"] = new JsonObject
                    {
                        ["anyOf"] = new JsonArray
                        {
                            new JsonObject { ["type"] = "string" },
                            new JsonObject { ["type"] = "null" }
                        }
                    },
                    ["channel"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray { "email", "sms" }
                    }
                }
            };

        public static string BuildSystemPrompt(string promptSchemaVersion)
            => $"""
                You draft a private UK hospitality guest response for an operator.
                Prompt/schema version: {promptSchemaVersion}.

                Return Structured Outputs only.
                Write one-shot editable prose for the operator — not a classification.
                Match channel (email vs sms), purpose, and tone from the user payload.
                When confirmedInternalActionCategory/Note are present, reflect that
                confirmed internal follow-up in the guest-facing prose without inventing
                other operational claims.
                For sms, subject must be null. For email, subject must be non-empty.
                Never invent or include raw guest email or phone numbers.
                When mode is rewrite, improve currentBody/currentSubject rather than
                ignoring them.
                """;

        public static bool TryParseModelContent(
            string? content,
            string requestedChannel,
            out FeedbackRecoveryDraftResult? result,
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
                if (!root.TryGetProperty("body", out var bodyElement)
                    || bodyElement.ValueKind != JsonValueKind.String)
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

                string? subject = null;
                if (root.TryGetProperty("subject", out var subjectElement))
                {
                    if (subjectElement.ValueKind == JsonValueKind.String)
                    {
                        subject = subjectElement.GetString();
                    }
                    else if (subjectElement.ValueKind != JsonValueKind.Null)
                    {
                        invalidOutput = true;
                        return false;
                    }
                }

                var channel = requestedChannel;
                if (root.TryGetProperty("channel", out var channelElement)
                    && channelElement.ValueKind == JsonValueKind.String
                    && channelElement.GetString() is { } echoed
                    && (echoed == "email" || echoed == "sms"))
                {
                    channel = echoed;
                }

                if (string.Equals(channel, "sms", StringComparison.Ordinal))
                {
                    subject = null;
                }
                else if (string.IsNullOrWhiteSpace(subject))
                {
                    invalidOutput = true;
                    return false;
                }

                result = new FeedbackRecoveryDraftResult.Succeeded(
                    body,
                    subject?.Trim(),
                    channel
                );
                return true;
            }
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
}
