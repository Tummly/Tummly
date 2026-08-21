using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Azure OpenAI Structured Outputs contract for Weekly brief generation.
    /// Schema is Weekly-brief-owned — not Home recommendation or Campaigns schemas.
    /// Prompt inputs are aggregate metrics only (no guest PII, no raw feedback text).
    /// </summary>
    public static class WeeklyBriefStructuredOutput
    {
        public const string SchemaName = "weekly_brief";

        public const string HttpClientName = "AzureOpenAIWeeklyBrief";

        /// <summary>
        /// Shared schema version for store + API + Azure Structured Outputs.
        /// </summary>
        public const string SchemaVersion = "v1";

        public const string PromptSchemaRevision = "2026-08-21";

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

        private static readonly HashSet<string> EmptySectionSummaries =
            new(StringComparer.Ordinal)
            {
                EmptyCaptureSummary,
                EmptyFeedbackSummary,
                EmptyOffersSummary,
                EmptyCampaignsSummary,
            };

        private static readonly JsonSerializerOptions RequestJsonOptions = new()
        {
            WriteIndented = false,
        };

        public static bool TryParseModelContent(
            string? content,
            out WeeklyBriefBody? body,
            out bool invalidOutput
        )
        {
            body = null;
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
        }

        public static JsonObject BuildSchema()
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

        public static string BuildSystemPrompt(string promptSchemaVersion)
            => $"""
                You write one Weekly brief for a UK hospitality operator Home page.
                Prompt/schema version: {promptSchemaVersion}.
                Schema version: {SchemaVersion}.
                Revision: {PromptSchemaRevision}.

                Return Structured Outputs only.
                Use only the fed aggregate metrics and theme rollups — never invent counts.
                Do not include guest names, emails, phones, or feedback comment bodies.
                Do not include Home Recommended next-step types.
                watchNext must have {WatchNextMinLength} to {WatchNextMaxLength} short
                advisory lines (text only).
                For each section with no signal: set hasData false and use the matching
                empty summary from the allow-list:
                "{EmptyCaptureSummary}"
                "{EmptyFeedbackSummary}"
                "{EmptyOffersSummary}"
                "{EmptyCampaignsSummary}"
                When hasData is true, summarise that domain from the metrics bag.
                Set echoedCounts to null; the server attaches echoed counts from metrics.
                """;

        public static string BuildRequestJson(
            string deploymentName,
            WeeklyBriefProviderInput input,
            string promptSchemaVersion
        )
        {
            var metrics = input.Metrics;
            var themeCounts = new JsonObject();
            foreach (var pair in metrics.FeedbackThemeCounts)
            {
                themeCounts[pair.Key] = pair.Value;
            }

            var userPayload = new JsonObject
            {
                ["schemaVersion"] = SchemaVersion,
                ["locationName"] = input.LocationName,
                ["weekKey"] = input.WeekKey,
                ["coverageStartUtc"] = input.CoverageStartUtc.ToString("O"),
                ["coverageEndUtcExclusive"] =
                    input.CoverageEndUtcExclusive.ToString("O"),
                ["metrics"] = new JsonObject
                {
                    ["guestsJoined"] = metrics.GuestsJoined,
                    ["captureEvents"] = metrics.CaptureEvents,
                    ["feedbackCount"] = metrics.FeedbackCount,
                    ["positiveFeedbackCount"] = metrics.PositiveFeedbackCount,
                    ["neutralFeedbackCount"] = metrics.NeutralFeedbackCount,
                    ["negativeFeedbackCount"] = metrics.NegativeFeedbackCount,
                    ["needsAttentionCount"] = metrics.NeedsAttentionCount,
                    ["feedbackThemeCounts"] = themeCounts,
                    ["activeOffers"] = metrics.ActiveOffers,
                    ["claimsInWeek"] = metrics.ClaimsInWeek,
                    ["redemptionsInWeek"] = metrics.RedemptionsInWeek,
                    ["campaignsSentInWeek"] = metrics.CampaignsSentInWeek,
                    ["campaignRecipientsReached"] =
                        metrics.CampaignRecipientsReached,
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

        public static bool IsAllowedEmptySectionSummary(string summary)
            => EmptySectionSummaries.Contains(summary);

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

            if (!hasData && !IsAllowedEmptySectionSummary(summary))
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

    /// <summary>
    /// Provider input for Weekly brief Structured Outputs (ticket 03 wires the caller).
    /// </summary>
    public sealed record WeeklyBriefProviderInput(
        string LocationName,
        string WeekKey,
        DateTime CoverageStartUtc,
        DateTime CoverageEndUtcExclusive,
        WeeklyBriefMetrics Metrics
    );
}
