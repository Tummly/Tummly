using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Azure OpenAI Structured Outputs contract for Campaign message drafts.
    /// Reuses FeedbackClassification Endpoint/ApiKey/Deployment settings.
    /// </summary>
    public static class CampaignMessageDraftStructuredOutput
    {
        public const string SchemaName = "campaign_message_draft";

        public const string HttpClientName = "AzureOpenAICampaignMessageDraft";

        public const string ProsePunctuationRevision = "2026-08-08";

        private static readonly JsonSerializerOptions RequestJsonOptions = new()
        {
            WriteIndented = false
        };

        public static string BuildRequestJson(
            string deploymentName,
            CampaignMessageDraftInput input,
            string promptSchemaVersion
        )
        {
            var userPayload = new JsonObject
            {
                ["locationName"] = input.LocationName,
                ["channel"] = input.Channel,
                ["goalId"] = input.GoalId,
                ["audienceKey"] = input.AudienceKey,
                ["offerStance"] = input.OfferStance,
                ["campaignName"] = input.CampaignName,
                ["tone"] = input.Tone,
                ["includeNotes"] = input.IncludeNotes,
                ["mode"] = input.Mode,
                ["currentBody"] = input.CurrentBody,
                ["currentSubject"] = input.CurrentSubject,
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
                You draft a UK hospitality marketing campaign message for an operator.
                Prompt/schema version: {promptSchemaVersion}.
                Prose punctuation revision: {ProsePunctuationRevision}.

                Return Structured Outputs only.
                Write one-shot editable prose for the operator - not a classification.
                Match channel (email vs sms), goal, audience, offer stance, and tone
                from the user payload.
                For sms, subject must be null. For email, subject must be non-empty.
                Never invent or include guest email, phone, or other guest PII.
                When mode is prepare, draft both body and subject (subject null for sms).
                When mode is rewrite_subject, rewrite only the subject from
                currentSubject (and context). Return the improved subject; return
                currentBody unchanged as body.
                When mode is rewrite_message, rewrite only the body from
                currentBody (and context). Return the improved body; for email
                return currentSubject unchanged as subject (null for sms).

                Punctuation (body and subject):
                Use plain ASCII only: apostrophe ('), hyphen (-), double quote ("),
                and three dots (...) for ellipsis.
                Do not use curly quotes, smart quotes, em dashes, en dashes,
                or other Unicode punctuation.
                Do not emit control characters.
                """;

        public static bool TryParseModelContent(
            string? content,
            string requestedChannel,
            out CampaignMessageDraftProviderResult? result,
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

                var body = FeedbackRecoveryDraftStructuredOutput.SanitizeGuestProse(
                    bodyElement.GetString() ?? string.Empty
                ).Trim();
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
                else
                {
                    subject = FeedbackRecoveryDraftStructuredOutput
                        .SanitizeGuestProse(subject)
                        .Trim();
                    if (subject.Length == 0)
                    {
                        invalidOutput = true;
                        return false;
                    }
                }

                result = new CampaignMessageDraftProviderResult.Succeeded(
                    body,
                    subject,
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
