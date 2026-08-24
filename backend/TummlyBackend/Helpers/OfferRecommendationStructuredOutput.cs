using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Azure OpenAI Structured Outputs contract for Offer recommendation.
    /// Reuses FeedbackClassification Endpoint/ApiKey/Deployment settings.
    /// </summary>
    public static class OfferRecommendationStructuredOutput
    {
        public const string SchemaName = "offer_recommendation";

        public const string HttpClientName = "AzureOpenAIOfferRecommendation";

        public const string PromptSchemaRevision = "2026-08-24";

        private static readonly HashSet<string> AllowedTypes =
            new(StringComparer.Ordinal)
            {
                OfferRecommendationContract.TypePromote,
                OfferRecommendationContract.TypeFix,
                OfferRecommendationContract.TypeNone,
            };

        private static readonly JsonSerializerOptions RequestJsonOptions = new()
        {
            WriteIndented = false,
        };

        public static string BuildRequestJson(
            string deploymentName,
            OfferRecommendationProviderInput input,
            string promptSchemaVersion
        )
        {
            var userPayload = new JsonObject
            {
                ["selectedType"] = input.SelectedType,
                ["locationName"] = input.LocationName,
                ["reportingPeriod"] = input.ReportingPeriod,
                ["fromUtc"] = input.FromUtc.ToString("O"),
                ["toUtc"] = input.ToUtc.ToString("O"),
                ["offerId"] = input.OfferId,
                ["offerTitle"] = input.OfferTitle,
                ["marketingEligible"] = input.MarketingEligible,
                ["claimsInPeriod"] = input.ClaimsInPeriod,
                ["needsAttention"] = input.NeedsAttention,
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
                    "whyBullets",
                    "suggestedChannel",
                    "campaignName",
                    "messageSubject",
                    "messageBody",
                },
                ["properties"] = new JsonObject
                {
                    ["type"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray
                        {
                            OfferRecommendationContract.TypePromote,
                            OfferRecommendationContract.TypeFix,
                            OfferRecommendationContract.TypeNone,
                        },
                    },
                    ["title"] = NullableString(),
                    ["opportunity"] = NullableString(),
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
                    ["campaignName"] = NullableString(),
                    ["messageSubject"] = NullableString(),
                    ["messageBody"] = NullableString(),
                },
            };

        public static string BuildSystemPrompt(string promptSchemaVersion)
            => $"""
                You write copy for one Offer recommendation for a UK
                hospitality operator.
                Prompt/schema version: {promptSchemaVersion}.
                Revision: {PromptSchemaRevision}.

                Return Structured Outputs only.
                The selectedType is already chosen — emit that type.
                Do not change the type. Do not emit none unless selectedType is none.
                Allowed types: promote-this-offer, fix-this-offer, none.
                Use only the fed counts — never invent guest counts.
                Do not include guest names, emails, phones, or feedback text.
                When type is none, set copy fields to null.
                When type is not none, fill title, opportunity, and whyBullets.
                For promote-this-offer, set suggestedChannel to email or sms
                and fill campaignName, messageSubject, and messageBody.
                For fix-this-offer, set campaign fields to null.
                No schedule, send, or credit language.
                """;

        public static bool TryParseModelContent(
            string? content,
            out OfferRecommendationModelOutput? output,
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

                if (string.Equals(type, OfferRecommendationContract.TypeNone, StringComparison.Ordinal))
                {
                    output = new OfferRecommendationModelOutput(
                        Type: OfferRecommendationContract.TypeNone,
                        Title: null,
                        Opportunity: null,
                        WhyBullets: null,
                        SuggestedChannel: null,
                        CampaignName: null,
                        MessageSubject: null,
                        MessageBody: null
                    );
                    return true;
                }

                var title = ReadRequiredString(root, "title");
                var opportunity = ReadRequiredString(root, "opportunity");
                if (title is null || opportunity is null)
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

                output = new OfferRecommendationModelOutput(
                    Type: type,
                    Title: FeedbackRecoveryDraftStructuredOutput
                        .SanitizeGuestProse(title)
                        .Trim(),
                    Opportunity: FeedbackRecoveryDraftStructuredOutput
                        .SanitizeGuestProse(opportunity)
                        .Trim(),
                    WhyBullets: whyBullets
                        .Select(bullet =>
                            FeedbackRecoveryDraftStructuredOutput
                                .SanitizeGuestProse(bullet)
                                .Trim()
                        )
                        .Where(bullet => bullet.Length > 0)
                        .ToArray(),
                    SuggestedChannel: ReadOptionalString(root, "suggestedChannel"),
                    CampaignName: ReadOptionalString(root, "campaignName"),
                    MessageSubject: ReadOptionalString(root, "messageSubject"),
                    MessageBody: ReadOptionalString(root, "messageBody")
                );

                if (output.WhyBullets is null || output.WhyBullets.Count == 0)
                {
                    output = null;
                    invalidOutput = true;
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

        private static string? ReadRequiredString(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var element)
                || element.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var value = element.GetString()?.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static string? ReadOptionalString(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var element)
                || element.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (element.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var value = element.GetString()?.Trim();
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

                var bullet = item.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(bullet))
                {
                    bullets.Add(bullet);
                }
            }

            return true;
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
