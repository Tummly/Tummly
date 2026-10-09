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

        /// <summary>
        /// Bumped when offer-grounding / rewrite quality prompt rules change.
        /// </summary>
        public const string OfferGroundingRevision = "2026-09-25";

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
            var request = new JsonObject
            {
                ["model"] = deploymentName,
                ["messages"] = BuildOneShotMessages(input, promptSchemaVersion),
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

        /// <summary>
        /// Seed messages for the tool-wave path: channel/goal/mode only.
        /// Location and offer facts come from read tools.
        /// </summary>
        public static JsonArray BuildToolSeedMessages(
            CampaignMessageDraftInput input,
            string promptSchemaVersion
        )
        {
            var userPayload = new JsonObject
            {
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
                ["hasConfirmedOffer"] = input.ConfirmedOffer is not null,
            };

            return new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "system",
                    ["content"] = BuildToolSystemPrompt(promptSchemaVersion),
                },
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = userPayload.ToJsonString(RequestJsonOptions),
                },
            };
        }

        private static JsonArray BuildOneShotMessages(
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

            return new JsonArray
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
            };
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
                Offer grounding revision: {OfferGroundingRevision}.

                Return Structured Outputs only.
                Write one-shot editable prose for the operator - not a classification.
                Match channel (email vs sms), goal, audience, offer stance, and tone
                from the user payload.
                For sms, subject must be null. For email, subject must be non-empty.
                Never invent or include guest email, phone, or other guest PII.

                Offer facts:
                When confirmedOffer is present, use only those facts for any discount,
                free item, replacement, title, description, validity, or expiry wording.
                Do not invent offer terms, percentages, amounts, items, or validity.
                Never invent a redemption code, claim code, promo code, or QR claim.
                When offerStance is no-offer or confirmedOffer is absent, do not invent
                an offer, discount, or claim code.
                Do not invent a website, a phone number, a reservation system,
                a private-event offer, or a social account.

                Audience voice:
                audienceKey negative-feedback means guests whose private Feedback
                was negative. Subject and body must say the last visit was not
                right and that the team wants to make it right. Be brief and
                sincere. Do not quote the Feedback. Do not invent what went wrong.
                Do not write a cheerful invitation about latest dishes, atmosphere,
                celebrations, or social updates.
                audienceKey positive-feedback thanks the guest for the visit.
                audienceKey dormant-guests says it has been a while and invites
                them back. audienceKey new-guests welcomes a first visit.
                audienceKey all-eligible-guests may be a general invitation, still
                without invented contact channels.

                When mode is prepare, draft both body and subject (subject null for sms).
                When mode is rewrite_subject, rewrite only the subject from
                currentSubject (and context). Improve clarity, tone fit, and pull;
                Do not only paraphrase. Preserve exact offer facts and numbers from
                confirmedOffer and from currentSubject. Return the improved subject;
                return currentBody unchanged as body.
                When mode is rewrite_message, rewrite only the body from
                currentBody (and context). Improve clarity, tone fit, and channel fit;
                Do not only paraphrase. Preserve exact offer facts and numbers from
                confirmedOffer and from currentBody. Never invent a redemption code.
                Return the improved body; for email return currentSubject unchanged
                as subject (null for sms).

                Punctuation (body and subject):
                Use plain ASCII only: apostrophe ('), hyphen (-), double quote ("),
                and three dots (...) for ellipsis.
                Do not use curly quotes, smart quotes, em dashes, en dashes,
                or other Unicode punctuation.
                Do not emit control characters.
                """;

        public static string BuildToolSystemPrompt(string promptSchemaVersion)
            => $"""
                {BuildSystemPrompt(promptSchemaVersion)}

                Tool path: call read_location_display_name before drafting.
                When hasConfirmedOffer is true, also call read_confirmed_offer_facts
                and ground offer wording only on that tool result.
                Do not invent location names or offer terms.
                Never invent or include guest email, phone, or redemption codes.
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
