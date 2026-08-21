using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Azure OpenAI Structured Outputs contract for Home-native recommendations.
    /// Reuses FeedbackClassification Endpoint/ApiKey/Deployment settings.
    /// Schema is Home-owned — not the Campaigns recommendation schema.
    /// </summary>
    public static class HomeRecommendationStructuredOutput
    {
        public const string SchemaName = "home_recommendation";

        public const string HttpClientName = "AzureOpenAIHomeRecommendation";

        public const string PromptSchemaRevision = "2026-08-21";

        private static readonly HashSet<string> AllowedNativeTypes =
            new(StringComparer.Ordinal)
            {
                "review-open-feedback",
                "thank-or-follow-guest",
                "promote-or-fix-offer",
                "none",
            };

        private static readonly JsonSerializerOptions RequestJsonOptions = new()
        {
            WriteIndented = false,
        };

        public static string BuildRequestJson(
            string deploymentName,
            HomeRecommendationProviderInput input,
            string promptSchemaVersion
        )
        {
            var metrics = input.Metrics;
            var userPayload = new JsonObject
            {
                ["selectedType"] = input.SelectedType,
                ["locationName"] = input.LocationName,
                ["overviewDatePreset"] = input.OverviewDatePreset,
                ["fromUtc"] = input.FromUtc.ToString("O"),
                ["toUtc"] = input.ToUtc.ToString("O"),
                ["metrics"] = new JsonObject
                {
                    ["openFeedbackCount"] = metrics.OpenFeedbackCount,
                    ["needsAttentionCount"] = metrics.NeedsAttentionCount,
                    ["guestsJoinedInWindow"] = metrics.GuestsJoinedInWindow,
                    ["marketingEligible"] = metrics.MarketingEligible,
                    ["activeOffers"] = metrics.ActiveOffers,
                    ["hasNoActiveOffers"] = metrics.HasNoActiveOffers,
                    ["offerNeedsAttentionCount"] = metrics.OfferNeedsAttentionCount,
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
                    "whyBullets",
                    "action",
                },
                ["properties"] = new JsonObject
                {
                    ["type"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray
                        {
                            "review-open-feedback",
                            "thank-or-follow-guest",
                            "promote-or-fix-offer",
                            "none",
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
                    ["action"] = new JsonObject
                    {
                        ["anyOf"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["type"] = "object",
                                ["additionalProperties"] = false,
                                ["required"] = new JsonArray
                                {
                                    "kind",
                                    "feedbackId",
                                    "locationGuestId",
                                    "offerId",
                                },
                                ["properties"] = new JsonObject
                                {
                                    ["kind"] = new JsonObject
                                    {
                                        ["type"] = "string",
                                        ["enum"] = new JsonArray
                                        {
                                            "open-feedback",
                                            "open-guest",
                                            "open-offer",
                                        },
                                    },
                                    ["feedbackId"] = NullableInt(),
                                    ["locationGuestId"] = NullableInt(),
                                    ["offerId"] = NullableInt(),
                                },
                            },
                            new JsonObject { ["type"] = "null" },
                        },
                    },
                },
            };

        public static string BuildSystemPrompt(string promptSchemaVersion)
            => $"""
                You write copy for one Home Recommended next step for a UK
                hospitality operator.
                Prompt/schema version: {promptSchemaVersion}.
                Revision: {PromptSchemaRevision}.

                Return Structured Outputs only.
                The selectedType is already chosen — emit that type (or none).
                Allowed types: review-open-feedback, thank-or-follow-guest,
                promote-or-fix-offer, none.
                Use only the fed metrics counts — never invent guest counts.
                Do not include guest names, emails, phones, or feedback text.
                When type is none, set title/opportunity/whyBullets/action to null.
                When type is not none, fill title, opportunity, whyBullets, and action.
                action.kind must match the type:
                review-open-feedback → open-feedback,
                thank-or-follow-guest → open-guest,
                promote-or-fix-offer → open-offer.
                Entity ids may be null (domain list destination).
                No campaign draftPrefill, schedule, send, or credit language.
                """;

        public static bool TryParseModelContent(
            string? content,
            out HomeRecommendationModelOutput? output,
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
                if (!AllowedNativeTypes.Contains(type))
                {
                    invalidOutput = true;
                    return false;
                }

                if (string.Equals(type, "none", StringComparison.Ordinal))
                {
                    output = new HomeRecommendationModelOutput(
                        Type: "none",
                        Title: null,
                        Opportunity: null,
                        WhyBullets: null,
                        Action: null
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

                if (!TryReadAction(root, type, out var action) || action is null)
                {
                    invalidOutput = true;
                    return false;
                }

                output = new HomeRecommendationModelOutput(
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
                    Action: action
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

        public static bool IsAllowedNativeType(string type)
            => AllowedNativeTypes.Contains(type);

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

        private static bool TryReadAction(
            JsonElement root,
            string type,
            out HomeRecommendationDomainActionOutput? action
        )
        {
            action = null;
            if (!root.TryGetProperty("action", out var element)
                || element.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!element.TryGetProperty("kind", out var kindElement)
                || kindElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var kind = kindElement.GetString()?.Trim() ?? string.Empty;
            if (!HomeRecommendationContract.IsAllowedDomainActionKind(kind))
            {
                return false;
            }

            var expectedKind = type switch
            {
                "review-open-feedback" => "open-feedback",
                "thank-or-follow-guest" => "open-guest",
                "promote-or-fix-offer" => "open-offer",
                _ => null,
            };

            if (expectedKind is null
                || !string.Equals(kind, expectedKind, StringComparison.Ordinal))
            {
                return false;
            }

            action = new HomeRecommendationDomainActionOutput(
                Kind: kind,
                FeedbackId: ReadNullableInt(element, "feedbackId"),
                LocationGuestId: ReadNullableInt(element, "locationGuestId"),
                OfferId: ReadNullableInt(element, "offerId")
            );
            return true;
        }

        private static int? ReadNullableInt(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var element)
                || element.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (element.ValueKind == JsonValueKind.Number
                && element.TryGetInt32(out var value))
            {
                return value;
            }

            return null;
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

        private static JsonObject NullableInt()
            => new()
            {
                ["anyOf"] = new JsonArray
                {
                    new JsonObject { ["type"] = "integer" },
                    new JsonObject { ["type"] = "null" },
                },
            };
    }
}
