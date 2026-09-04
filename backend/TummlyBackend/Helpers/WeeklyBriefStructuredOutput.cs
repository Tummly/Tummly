using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Azure OpenAI Structured Outputs contract for Weekly brief generation.
    /// Schema is Weekly-brief-owned — not Home recommendation or Campaigns schemas.
    /// Prompt inputs are aggregate metrics only (no guest PII, no raw feedback text).
    /// Wrapper v2: <c>{ body, enrichment }</c> — Home stores only <see cref="WeeklyBriefBody"/>.
    /// </summary>
    public static class WeeklyBriefStructuredOutput
    {
        public const string SchemaName = "weekly_brief";

        public const string HttpClientName = "AzureOpenAIWeeklyBrief";

        /// <summary>
        /// Azure Structured Outputs wrapper schema version (body + enrichment).
        /// Home durable <see cref="WeeklyBriefBody"/> remains the v1 body shape.
        /// </summary>
        public const string SchemaVersion = "v2";

        /// <summary>Body object schema version nested under the wrapper.</summary>
        public const string BodySchemaVersion = "v1";

        public const string PromptSchemaRevision = "2026-09-04a";

        public const int WatchNextMinLength = 1;

        public const int WatchNextMaxLength = 3;

        public const string EmptyCaptureSummary =
            "No guest capture activity this week.";

        public const string EmptyFeedbackSummary =
            "No feedback this week.";

        public const string EmptyOffersSummary =
            "No offer activity this week.";

        public const string EmptyCampaignsSummary =
            "No campaign activity this week.";

        private static readonly JsonSerializerOptions RequestJsonOptions = new()
        {
            WriteIndented = false,
        };

        public static bool TryParseModelContent(
            string? content,
            out WeeklyBriefBody? body,
            out WeeklyBriefEnrichment? enrichment,
            out bool invalidOutput
        )
        {
            body = null;
            enrichment = null;
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
                    || bodyElement.ValueKind != JsonValueKind.Object)
                {
                    invalidOutput = true;
                    return false;
                }

                if (!TryParseBodyElement(bodyElement, out body, out invalidOutput))
                {
                    body = null;
                    return false;
                }

                if (!root.TryGetProperty("enrichment", out var enrichmentElement)
                    || enrichmentElement.ValueKind != JsonValueKind.Object)
                {
                    body = null;
                    invalidOutput = true;
                    return false;
                }

                if (!TryParseEnrichmentElement(
                        enrichmentElement,
                        out enrichment,
                        out invalidOutput
                    ))
                {
                    body = null;
                    enrichment = null;
                    return false;
                }

                return true;
            }
        }

        public static JsonObject BuildSchema()
            => new()
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["required"] = new JsonArray { "body", "enrichment" },
                ["properties"] = new JsonObject
                {
                    ["body"] = BuildBodySchema(),
                    ["enrichment"] = EnrichmentSchema(),
                },
            };

        public static string BuildSystemPrompt(string promptSchemaVersion)
            => $"""
                You write one Weekly brief for a UK hospitality operator.
                Prompt/schema version: {promptSchemaVersion}.
                Schema version: {SchemaVersion} (wrapper). Body schema: {BodySchemaVersion}.
                Revision: {PromptSchemaRevision}.

                Return Structured Outputs only as an object with body and enrichment.
                Use only the fed aggregate metrics and Detected Tag rollups — never invent counts.
                Do not include guest names, emails, phones, or feedback comment bodies.
                Do not include Home Recommended next-step types.

                body.watchNext must have {WatchNextMinLength} to {WatchNextMaxLength} short
                advisory lines (text only).
                For each body section with no signal: set hasData false and use that section's
                empty summary exactly:
                capture → "{EmptyCaptureSummary}"
                feedback → "{EmptyFeedbackSummary}"
                offers → "{EmptyOffersSummary}"
                campaigns → "{EmptyCampaignsSummary}"
                When hasData is true, summarise that domain from the metrics bag.
                Set echoedCounts to null; the server attaches echoed counts from metrics.

                enrichment.executiveSummary: one plain-English paragraph for Reports
                (what happened this week from the metrics and tags).
                enrichment.feedbackSummary: narrative text + subtitle for private feedback;
                when feedbackCount and needsAttentionCount are both 0, use empty strings.
                enrichment.actionWording: optional title/subtitle for known action kinds only
                (feedback-needs-attention, repeated-invalid, low-redemption). Omit kinds
                that do not apply; never invent other kinds. Empty array is allowed.
                """;

        public static string BuildRequestJson(
            string deploymentName,
            WeeklyBriefProviderInput input,
            string promptSchemaVersion
        )
        {
            var metrics = input.Metrics;
            var detectedTagCounts = new JsonObject();
            foreach (var pair in metrics.DetectedTagCounts)
            {
                detectedTagCounts[pair.Key] = pair.Value;
            }

            var userPayload = new JsonObject
            {
                ["schemaVersion"] = SchemaVersion,
                ["bodySchemaVersion"] = BodySchemaVersion,
                ["locationName"] = input.LocationName,
                ["weekKey"] = input.WeekKey,
                ["coverageStartUtc"] = input.CoverageStartUtc.ToString("O"),
                ["coverageEndUtcExclusive"] =
                    input.CoverageEndUtcExclusive.ToString("O"),
                ["metrics"] = new JsonObject
                {
                    ["guestsJoined"] = metrics.GuestsJoined,
                    ["qrScanEvents"] = metrics.QrScanEvents,
                    ["feedbackCount"] = metrics.FeedbackCount,
                    ["positiveFeedbackCount"] = metrics.PositiveFeedbackCount,
                    ["neutralFeedbackCount"] = metrics.NeutralFeedbackCount,
                    ["negativeFeedbackCount"] = metrics.NegativeFeedbackCount,
                    ["needsAttentionCount"] = metrics.NeedsAttentionCount,
                    ["detectedTagCounts"] = detectedTagCounts,
                    ["activeOffers"] = metrics.ActiveOffers,
                    ["claimsInWeek"] = metrics.ClaimsInWeek,
                    ["redemptionsInWeek"] = metrics.RedemptionsInWeek,
                    ["campaignsSentInWeek"] = metrics.CampaignsSentInWeek,
                    ["campaignRecipientsReached"] =
                        metrics.CampaignRecipientsReached,
                    ["unsubscribesInWeek"] = metrics.UnsubscribesInWeek,
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

        public static bool TryExtractMessageContent(
            string responseJson,
            out string? content
        )
            => FeedbackClassificationStructuredOutput.TryExtractMessageContent(
                responseJson,
                out content
            );

        public static bool IsAllowedEmptySectionSummary(
            string sectionName,
            string summary
        )
            => sectionName switch
            {
                "capture" => string.Equals(
                    summary,
                    EmptyCaptureSummary,
                    StringComparison.Ordinal
                ),
                "feedback" => string.Equals(
                    summary,
                    EmptyFeedbackSummary,
                    StringComparison.Ordinal
                ),
                "offers" => string.Equals(
                    summary,
                    EmptyOffersSummary,
                    StringComparison.Ordinal
                ),
                "campaigns" => string.Equals(
                    summary,
                    EmptyCampaignsSummary,
                    StringComparison.Ordinal
                ),
                _ => false,
            };

        private static bool TryParseBodyElement(
            JsonElement root,
            out WeeklyBriefBody? body,
            out bool invalidOutput
        )
        {
            body = null;
            invalidOutput = false;

            var headline = ReadRequiredString(root, "headline");
            if (headline is null)
            {
                invalidOutput = true;
                return false;
            }

            if (!TryReadSection(root, "capture", out var capture)
                || !TryReadSection(root, "feedback", out var feedback)
                || !TryReadSection(root, "offers", out var offers)
                || !TryReadSection(root, "campaigns", out var campaigns))
            {
                invalidOutput = true;
                return false;
            }

            if (!TryReadWatchNext(root, out var watchNext))
            {
                invalidOutput = true;
                return false;
            }

            body = new WeeklyBriefBody(
                Headline: FeedbackRecoveryDraftStructuredOutput
                    .SanitizeGuestProse(headline)
                    .Trim(),
                Capture: SanitizeSection(capture!),
                Feedback: SanitizeSection(feedback!),
                Offers: SanitizeSection(offers!),
                Campaigns: SanitizeSection(campaigns!),
                WatchNext: watchNext!
                    .Select(line =>
                        FeedbackRecoveryDraftStructuredOutput
                            .SanitizeGuestProse(line)
                            .Trim()
                    )
                    .Where(line => line.Length > 0)
                    .ToArray()
            );

            if (body.Headline.Length == 0
                || body.WatchNext.Count < WatchNextMinLength
                || body.WatchNext.Count > WatchNextMaxLength)
            {
                body = null;
                invalidOutput = true;
                return false;
            }

            return true;
        }

        private static bool TryParseEnrichmentElement(
            JsonElement root,
            out WeeklyBriefEnrichment? enrichment,
            out bool invalidOutput
        )
        {
            enrichment = null;
            invalidOutput = false;

            if (!root.TryGetProperty("executiveSummary", out var execElement)
                || execElement.ValueKind != JsonValueKind.String)
            {
                invalidOutput = true;
                return false;
            }

            if (!root.TryGetProperty("feedbackSummary", out var feedbackElement)
                || feedbackElement.ValueKind != JsonValueKind.Object)
            {
                invalidOutput = true;
                return false;
            }

            if (!feedbackElement.TryGetProperty("text", out var textElement)
                || textElement.ValueKind != JsonValueKind.String
                || !feedbackElement.TryGetProperty("subtitle", out var subtitleElement)
                || subtitleElement.ValueKind != JsonValueKind.String)
            {
                invalidOutput = true;
                return false;
            }

            if (!root.TryGetProperty("actionWording", out var actionsElement)
                || actionsElement.ValueKind != JsonValueKind.Array)
            {
                invalidOutput = true;
                return false;
            }

            var actionWording = new List<WeeklyBriefEnrichmentActionWording>();
            foreach (var item in actionsElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    invalidOutput = true;
                    return false;
                }

                if (!item.TryGetProperty("kind", out var kindElement)
                    || kindElement.ValueKind != JsonValueKind.String
                    || !item.TryGetProperty("title", out var titleElement)
                    || titleElement.ValueKind != JsonValueKind.String
                    || !item.TryGetProperty("subtitle", out var actionSubtitleElement)
                    || actionSubtitleElement.ValueKind != JsonValueKind.String)
                {
                    invalidOutput = true;
                    return false;
                }

                var kind = kindElement.GetString()?.Trim() ?? string.Empty;
                if (!WeeklyBriefEnrichmentActionKinds.IsAllowed(kind))
                {
                    invalidOutput = true;
                    return false;
                }

                var title = FeedbackRecoveryDraftStructuredOutput
                    .SanitizeGuestProse(titleElement.GetString() ?? string.Empty)
                    .Trim();
                var subtitle = FeedbackRecoveryDraftStructuredOutput
                    .SanitizeGuestProse(
                        actionSubtitleElement.GetString() ?? string.Empty
                    )
                    .Trim();
                if (title.Length == 0 || subtitle.Length == 0)
                {
                    invalidOutput = true;
                    return false;
                }

                actionWording.Add(
                    new WeeklyBriefEnrichmentActionWording(kind, title, subtitle)
                );
            }

            var executiveSummary = FeedbackRecoveryDraftStructuredOutput
                .SanitizeGuestProse(execElement.GetString() ?? string.Empty)
                .Trim();
            var feedbackText = FeedbackRecoveryDraftStructuredOutput
                .SanitizeGuestProse(textElement.GetString() ?? string.Empty)
                .Trim();
            var feedbackSubtitle = FeedbackRecoveryDraftStructuredOutput
                .SanitizeGuestProse(subtitleElement.GetString() ?? string.Empty)
                .Trim();

            WeeklyBriefEnrichmentFeedbackSummary? feedbackSummary = null;
            if (feedbackText.Length > 0)
            {
                feedbackSummary = new WeeklyBriefEnrichmentFeedbackSummary(
                    feedbackText,
                    feedbackSubtitle
                );
            }

            enrichment = new WeeklyBriefEnrichment(
                ExecutiveSummary: string.IsNullOrWhiteSpace(executiveSummary)
                    ? null
                    : executiveSummary,
                FeedbackSummary: feedbackSummary,
                ActionWording: actionWording
            );
            return true;
        }

        private static JsonObject BuildBodySchema()
            => new()
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["required"] = new JsonArray
                {
                    "headline",
                    "capture",
                    "feedback",
                    "offers",
                    "campaigns",
                    "watchNext",
                },
                ["properties"] = new JsonObject
                {
                    ["headline"] = new JsonObject { ["type"] = "string" },
                    ["capture"] = SectionSchema(),
                    ["feedback"] = SectionSchema(),
                    ["offers"] = SectionSchema(),
                    ["campaigns"] = SectionSchema(),
                    ["watchNext"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["minItems"] = WatchNextMinLength,
                        ["maxItems"] = WatchNextMaxLength,
                        ["items"] = new JsonObject { ["type"] = "string" },
                    },
                },
            };

        private static JsonObject EnrichmentSchema()
            => new()
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["required"] = new JsonArray
                {
                    "executiveSummary",
                    "feedbackSummary",
                    "actionWording",
                },
                ["properties"] = new JsonObject
                {
                    ["executiveSummary"] = new JsonObject { ["type"] = "string" },
                    ["feedbackSummary"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["additionalProperties"] = false,
                        ["required"] = new JsonArray { "text", "subtitle" },
                        ["properties"] = new JsonObject
                        {
                            ["text"] = new JsonObject { ["type"] = "string" },
                            ["subtitle"] = new JsonObject { ["type"] = "string" },
                        },
                    },
                    ["actionWording"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["additionalProperties"] = false,
                            ["required"] = new JsonArray
                            {
                                "kind",
                                "title",
                                "subtitle",
                            },
                            ["properties"] = new JsonObject
                            {
                                ["kind"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["enum"] = new JsonArray
                                    {
                                        WeeklyBriefEnrichmentActionKinds
                                            .FeedbackNeedsAttention,
                                        WeeklyBriefEnrichmentActionKinds
                                            .RepeatedInvalid,
                                        WeeklyBriefEnrichmentActionKinds
                                            .LowRedemption,
                                    },
                                },
                                ["title"] = new JsonObject { ["type"] = "string" },
                                ["subtitle"] = new JsonObject
                                {
                                    ["type"] = "string",
                                },
                            },
                        },
                    },
                },
            };

        private static WeeklyBriefSection SanitizeSection(WeeklyBriefSection section)
            => section with
            {
                Summary = FeedbackRecoveryDraftStructuredOutput
                    .SanitizeGuestProse(section.Summary)
                    .Trim(),
            };

        private static bool TryReadSection(
            JsonElement root,
            string name,
            out WeeklyBriefSection? section
        )
        {
            section = null;
            if (!root.TryGetProperty(name, out var element)
                || element.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!element.TryGetProperty("hasData", out var hasDataElement)
                || (hasDataElement.ValueKind != JsonValueKind.True
                    && hasDataElement.ValueKind != JsonValueKind.False))
            {
                return false;
            }

            var hasData = hasDataElement.GetBoolean();
            if (!element.TryGetProperty("summary", out var summaryElement)
                || summaryElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var summary = summaryElement.GetString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(summary))
            {
                return false;
            }

            if (!hasData && !IsAllowedEmptySectionSummary(name, summary))
            {
                return false;
            }

            IReadOnlyDictionary<string, int>? echoed = null;
            if (element.TryGetProperty("echoedCounts", out var countsElement))
            {
                if (countsElement.ValueKind == JsonValueKind.Null)
                {
                    echoed = null;
                }
                else if (countsElement.ValueKind == JsonValueKind.Object)
                {
                    var map = new Dictionary<string, int>(StringComparer.Ordinal);
                    foreach (var prop in countsElement.EnumerateObject())
                    {
                        if (prop.Value.ValueKind != JsonValueKind.Number
                            || !prop.Value.TryGetInt32(out var count))
                        {
                            return false;
                        }

                        map[prop.Name] = count;
                    }

                    echoed = map;
                }
                else
                {
                    return false;
                }
            }

            section = new WeeklyBriefSection(hasData, summary, echoed);
            return true;
        }

        private static bool TryReadWatchNext(
            JsonElement root,
            out List<string>? watchNext
        )
        {
            watchNext = null;
            if (!root.TryGetProperty("watchNext", out var element)
                || element.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var lines = new List<string>();
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                {
                    return false;
                }

                var line = item.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lines.Add(line);
                }
            }

            if (lines.Count < WatchNextMinLength
                || lines.Count > WatchNextMaxLength)
            {
                return false;
            }

            watchNext = lines;
            return true;
        }

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

        private static JsonObject SectionSchema()
            => new()
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["required"] = new JsonArray
                {
                    "hasData",
                    "summary",
                    "echoedCounts",
                },
                ["properties"] = new JsonObject
                {
                    ["hasData"] = new JsonObject { ["type"] = "boolean" },
                    ["summary"] = new JsonObject { ["type"] = "string" },
                    // Azure strict mode forbids free-form maps; model emits null.
                    // Store/API may attach echoedCounts from the metrics bag.
                    ["echoedCounts"] = new JsonObject { ["type"] = "null" },
                },
            };
    }
}
