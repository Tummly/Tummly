using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Azure OpenAI Structured Outputs contract for Campaign recommendations.
    /// Reuses FeedbackClassification Endpoint/ApiKey/Deployment settings.
    /// </summary>
    public static class CampaignRecommendationStructuredOutput
    {
        public const string SchemaName = "campaign_recommendation";

        public const string HttpClientName = "AzureOpenAICampaignRecommendation";

        public const string PromptSchemaRevision = "2026-08-08";

        private static readonly HashSet<string> AllowedTypes =
            new(StringComparer.Ordinal)
            {
                "thank-recent-guests",
                "re-engage",
                "recovery-follow-up",
                "none",
            };

        private static readonly JsonSerializerOptions RequestJsonOptions = new()
        {
            WriteIndented = false,
        };

        public static string BuildRequestJson(
            string deploymentName,
            CampaignRecommendationProviderInput input,
            string promptSchemaVersion
        )
        {
            var metrics = input.Metrics;
            var userPayload = new JsonObject
            {
                ["locationName"] = input.LocationName,
                ["overviewDatePreset"] = input.OverviewDatePreset,
                ["fromUtc"] = input.FromUtc?.ToString("O"),
                ["toUtc"] = input.ToUtc?.ToString("O"),
                ["metrics"] = new JsonObject
                {
                    ["marketingEligible"] = metrics.MarketingEligible,
                    ["allGuests"] = metrics.AllGuests,
                    ["newGuests"] = metrics.NewGuests,
                    ["needsRecovery"] = metrics.NeedsRecovery,
                    ["positiveFeedback"] = metrics.PositiveFeedback,
                    ["dormantGuests"] = metrics.DormantGuests,
                },
            };

            var request = new JsonObject
            {
                ["model"] = deploymentName,
                ["messages"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["role"] = "system",
                        ["content"] = BuildSystemPrompt(promptSchemaVersion),
                    },
                    new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = userPayload.ToJsonString(RequestJsonOptions),
                    },
                },
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

        public static JsonObject BuildSchema()
            => new()
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["required"] = new JsonArray
                {
                    "type",
                    "title",
                    "opportunity",
                    "eligibleAudience",
                    "whyBullets",
                    "suggestedChannel",
                    "estimatedUsage",
                    "draftPrefill",
                },
                ["properties"] = new JsonObject
                {
                    ["type"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray
                        {
                            "thank-recent-guests",
                            "re-engage",
                            "recovery-follow-up",
                            "none",
                        },
                    },
                    ["title"] = NullableString(),
                    ["opportunity"] = NullableString(),
                    ["eligibleAudience"] = NullableString(),
                    ["whyBullets"] = new JsonObject
                    {
                        ["anyOf"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["type"] = "array",
                                ["items"] = new JsonObject
                                {
                                    ["type"] = "string",
                                },
                            },
                            new JsonObject { ["type"] = "null" },
                        },
                    },
                    ["suggestedChannel"] = new JsonObject
                    {
                        ["anyOf"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["type"] = "string",
                                ["enum"] = new JsonArray { "email", "sms" },
                            },
                            new JsonObject { ["type"] = "null" },
                        },
                    },
                    ["estimatedUsage"] = NullableString(),
                    ["draftPrefill"] = new JsonObject
                    {
                        ["anyOf"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["type"] = "object",
                                ["additionalProperties"] = false,
                                ["required"] = new JsonArray
                                {
                                    "goalId",
                                    "audienceKey",
                                    "channel",
                                    "offerStance",
                                    "campaignName",
                                    "messageSubject",
                                    "messageBody",
                                },
                                ["properties"] = new JsonObject
                                {
                                    ["goalId"] = new JsonObject
                                    {
                                        ["type"] = "string",
                                    },
                                    ["audienceKey"] = new JsonObject
                                    {
                                        ["type"] = "string",
                                    },
                                    ["channel"] = new JsonObject
                                    {
                                        ["type"] = "string",
                                        ["enum"] = new JsonArray
                                        {
                                            "email",
                                            "sms",
                                        },
                                    },
                                    ["offerStance"] = new JsonObject
                                    {
                                        ["type"] = "string",
                                    },
                                    ["campaignName"] = new JsonObject
                                    {
                                        ["type"] = "string",
                                    },
                                    ["messageSubject"] = NullableString(),
                                    ["messageBody"] = new JsonObject
                                    {
                                        ["type"] = "string",
                                    },
                                },
                            },
                            new JsonObject { ["type"] = "null" },
                        },
                    },
                },
            };

        public static string BuildSystemPrompt(string promptSchemaVersion)
            => $"""
                You recommend one next Campaign for a UK hospitality operator.
                Prompt/schema version: {promptSchemaVersion}.
                Revision: {PromptSchemaRevision}.

                Return Structured Outputs only.
                Choose type from the closed allow-list only:
                thank-recent-guests, re-engage, recovery-follow-up, or none.
                Never emit quiet-time, promote-something-new, or unredeemed-offer.
                Use only the fed metrics counts - never invent guest counts.
                Do not include guest names, emails, phones, or feedback text.
                When type is none, set title/opportunity/eligibleAudience/whyBullets/
                suggestedChannel/estimatedUsage/draftPrefill to null.
                When type is not none, fill all copy fields and draftPrefill.
                draftPrefill must map goalId to a Campaign wizard goal id and
                audienceKey to a live Smart Group or all-eligible-guests.
                For sms channel, messageSubject must be null.
                No schedule, send, approve, recipient lists, or template ids.
                """;

        public static bool TryParseModelContent(
            string? content,
            out CampaignRecommendationModelOutput? output,
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
                if (!root.TryGetProperty("type", out var typeElement)
                    || typeElement.ValueKind != JsonValueKind.String)
                {
                    invalidOutput = true;
                    return false;
                }

                var type = typeElement.GetString()?.Trim() ?? string.Empty;
                if (!AllowedTypes.Contains(type))
                {
                    invalidOutput = true;
                    return false;
                }

                if (string.Equals(type, "none", StringComparison.Ordinal))
                {
                    output = new CampaignRecommendationModelOutput(
                        Type: "none",
                        Title: null,
                        Opportunity: null,
                        EligibleAudience: null,
                        WhyBullets: null,
                        SuggestedChannel: null,
                        EstimatedUsage: null,
                        DraftPrefill: null
                    );
                    return true;
                }

                var title = ReadRequiredString(root, "title");
                var opportunity = ReadRequiredString(root, "opportunity");
                var eligibleAudience = ReadRequiredString(root, "eligibleAudience");
                var estimatedUsage = ReadRequiredString(root, "estimatedUsage");
                if (
                    title is null
                    || opportunity is null
                    || eligibleAudience is null
                    || estimatedUsage is null
                )
                {
                    invalidOutput = true;
                    return false;
                }

                if (!TryReadWhyBullets(root, out var whyBullets)
                    || whyBullets.Count == 0)
                {
                    invalidOutput = true;
                    return false;
                }

                if (!TryReadChannel(root, "suggestedChannel", out var channel)
                    || channel is null)
                {
                    invalidOutput = true;
                    return false;
                }

                if (!TryReadDraftPrefill(root, channel, out var draftPrefill)
                    || draftPrefill is null)
                {
                    invalidOutput = true;
                    return false;
                }

                output = new CampaignRecommendationModelOutput(
                    Type: type,
                    Title: FeedbackRecoveryDraftStructuredOutput.SanitizeGuestProse(
                        title
                    ).Trim(),
                    Opportunity:
                        FeedbackRecoveryDraftStructuredOutput.SanitizeGuestProse(
                            opportunity
                        ).Trim(),
                    EligibleAudience:
                        FeedbackRecoveryDraftStructuredOutput.SanitizeGuestProse(
                            eligibleAudience
                        ).Trim(),
                    WhyBullets: whyBullets
                        .Select(bullet =>
                            FeedbackRecoveryDraftStructuredOutput
                                .SanitizeGuestProse(bullet)
                                .Trim()
                        )
                        .Where(bullet => bullet.Length > 0)
                        .ToArray(),
                    SuggestedChannel: channel,
                    EstimatedUsage:
                        FeedbackRecoveryDraftStructuredOutput.SanitizeGuestProse(
                            estimatedUsage
                        ).Trim(),
                    DraftPrefill: draftPrefill
                );

                if (output.WhyBullets.Count == 0)
                {
                    invalidOutput = true;
                    output = null;
                    return false;
                }

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

        public static bool IsAllowedType(string type)
            => AllowedTypes.Contains(type);

        private static JsonObject NullableString()
            => new()
            {
                ["anyOf"] = new JsonArray
                {
                    new JsonObject { ["type"] = "string" },
                    new JsonObject { ["type"] = "null" },
                },
            };

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
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static bool TryReadWhyBullets(
            JsonElement root,
            out List<string> bullets
        )
        {
            bullets = [];
            if (!root.TryGetProperty("whyBullets", out var element)
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

                var value = item.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    bullets.Add(value);
                }
            }

            return true;
        }

        private static bool TryReadChannel(
            JsonElement root,
            string propertyName,
            out string? channel
        )
        {
            channel = null;
            if (!root.TryGetProperty(propertyName, out var element)
                || element.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var value = element.GetString();
            if (value is not ("email" or "sms"))
            {
                return false;
            }

            channel = value;
            return true;
        }

        private static bool TryReadDraftPrefill(
            JsonElement root,
            string suggestedChannel,
            out CampaignRecommendationDraftPrefillOutput? prefill
        )
        {
            prefill = null;
            if (!root.TryGetProperty("draftPrefill", out var element)
                || element.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var goalId = ReadRequiredString(element, "goalId");
            var audienceKey = ReadRequiredString(element, "audienceKey");
            var offerStance = ReadRequiredString(element, "offerStance");
            var campaignName = ReadRequiredString(element, "campaignName");
            var messageBody = ReadRequiredString(element, "messageBody");
            if (
                goalId is null
                || audienceKey is null
                || offerStance is null
                || campaignName is null
                || messageBody is null
            )
            {
                return false;
            }

            if (!TryReadChannel(element, "channel", out var channel)
                || channel is null)
            {
                return false;
            }

            string? messageSubject = null;
            if (element.TryGetProperty("messageSubject", out var subjectElement))
            {
                if (subjectElement.ValueKind == JsonValueKind.String)
                {
                    messageSubject = subjectElement.GetString();
                }
                else if (subjectElement.ValueKind != JsonValueKind.Null)
                {
                    return false;
                }
            }

            if (string.Equals(channel, "sms", StringComparison.Ordinal))
            {
                messageSubject = null;
            }
            else if (string.IsNullOrWhiteSpace(messageSubject))
            {
                return false;
            }

            // Prefer suggested channel when they disagree.
            if (!string.Equals(channel, suggestedChannel, StringComparison.Ordinal))
            {
                channel = suggestedChannel;
                if (string.Equals(channel, "sms", StringComparison.Ordinal))
                {
                    messageSubject = null;
                }
            }

            prefill = new CampaignRecommendationDraftPrefillOutput(
                GoalId: FeedbackRecoveryDraftStructuredOutput
                    .SanitizeGuestProse(goalId)
                    .Trim(),
                AudienceKey: audienceKey.Trim(),
                Channel: channel,
                OfferStance: offerStance.Trim(),
                CampaignName: FeedbackRecoveryDraftStructuredOutput
                    .SanitizeGuestProse(campaignName)
                    .Trim(),
                MessageSubject: messageSubject is null
                    ? null
                    : FeedbackRecoveryDraftStructuredOutput
                        .SanitizeGuestProse(messageSubject)
                        .Trim(),
                MessageBody: FeedbackRecoveryDraftStructuredOutput
                    .SanitizeGuestProse(messageBody)
                    .Trim()
            );
            return true;
        }
    }
}
