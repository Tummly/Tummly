using System.Globalization;
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

        public const string PromptSchemaRevision = "2026-10-07a";

        public const int WatchNextMinLength = 0;

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
            => TryParseModelContent(
                content,
                insightCandidates: null,
                out body,
                out enrichment,
                out invalidOutput
            );

        public static bool TryParseModelContent(
            string? content,
            WeeklyBriefInsightCandidateBag? insightCandidates,
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

                if (insightCandidates is not null)
                {
                    enrichment = WeeklyBriefInsightNarrativeValidation.AttachCandidates(
                        enrichment,
                        insightCandidates
                    );
                    if (
                        !WeeklyBriefInsightNarrativeValidation.TryValidate(
                            body!,
                            enrichment,
                            insightCandidates,
                            out _
                        )
                    )
                    {
                        body = null;
                        enrichment = null;
                        invalidOutput = true;
                        return false;
                    }
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

                body.watchNext: 0 to {WatchNextMaxLength} short advisory lines (text only).
                When insightCandidates is only no-material-change, watchNext MUST be [].
                When any other candidate is present, watchNext MUST have 1 to {WatchNextMaxLength}
                lines grounded in those candidates — never invent filler.
                For each body section with no signal: set hasData false and use that section's
                empty summary exactly:
                capture → "{EmptyCaptureSummary}"
                feedback → "{EmptyFeedbackSummary}"
                offers → "{EmptyOffersSummary}"
                campaigns → "{EmptyCampaignsSummary}"
                When hasData is true, summarise that domain from the metrics bag.
                Set echoedCounts to null; the server attaches echoed counts from metrics.

                Narrate only from the supplied insightCandidates bag. Do not invent candidate
                types, ids, or numbers. Use only evidence.snapshot values for counts.
                Never use because / caused by / due to / as a result of unless that candidate
                has causalEvidence true (v1: always false).

                enrichment.executiveSummary: one plain-English paragraph for Reports
                (what happened this week from the candidates and metrics).
                enrichment.feedbackSummary: narrative text + subtitle for private feedback;
                when feedbackCount and needsAttentionCount are both 0, use empty strings.
                enrichment.actionWording: optional title/subtitle for known action kinds only
                (feedback-needs-attention, underperform-qr, repeated-invalid,
                low-redemption). Omit kinds that do not apply; never invent other kinds.
                Empty array is allowed.
                enrichment.insightNarratives: exactly one block for every candidateId in the bag
                with observation, interpretation, and recommendation (string; use "" when none).
                Do not omit candidates. recommendation required for control-signal and
                funnel-drop; forbidden for no-material-change; optional for data-quality-issue.
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
                ["insightCandidates"] = ToInsightCandidatesJson(input.InsightCandidates),
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

        /// <summary>
        /// Seed messages for the tool-wave path. Metrics come from
        /// read_weekly_brief_metrics — not the user payload.
        /// </summary>
        public static JsonArray BuildToolSeedMessages(
            WeeklyBriefProviderInput input,
            string promptSchemaVersion
        )
        {
            var userPayload = new JsonObject
            {
                ["schemaVersion"] = SchemaVersion,
                ["bodySchemaVersion"] = BodySchemaVersion,
                ["weekKey"] = input.WeekKey,
                ["coverageStartUtc"] = input.CoverageStartUtc.ToString("O"),
                ["coverageEndUtcExclusive"] =
                    input.CoverageEndUtcExclusive.ToString("O"),
                ["insightCandidates"] = ToInsightCandidatesJson(input.InsightCandidates),
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

        public static string BuildToolSystemPrompt(string promptSchemaVersion)
            => $"""
                {BuildSystemPrompt(promptSchemaVersion)}

                Tool path: call read_weekly_brief_metrics before writing the brief.
                Ground every count and tag rollup only on that tool result and the
                insightCandidates bag in the user payload / tool result.
                Never invent guest PII or feedback comment bodies.
                """;

        public static JsonNode ToInsightCandidatesJson(
            WeeklyBriefInsightCandidateBag? bag
        )
        {
            if (bag is null)
            {
                return new JsonObject
                {
                    ["thresholdVersion"] = WeeklyBriefInsightCandidates.ThresholdVersion,
                    ["candidates"] = new JsonArray(),
                };
            }

            var candidates = new JsonArray();
            foreach (var candidate in bag.Candidates)
            {
                var snapshot = new JsonObject();
                foreach (var pair in candidate.Evidence.Snapshot)
                {
                    snapshot[pair.Key] = pair.Value switch
                    {
                        null => null,
                        string s => s,
                        bool b => b,
                        int i => i,
                        long l => l,
                        float f => f,
                        double d => d,
                        decimal m => m,
                        JsonNode node => node.DeepClone(),
                        _ => JsonValue.Create(Convert.ToString(
                            pair.Value,
                            CultureInfo.InvariantCulture
                        )),
                    };
                }

                candidates.Add(
                    new JsonObject
                    {
                        ["id"] = candidate.Id,
                        ["type"] = candidate.Type,
                        ["actionKind"] = candidate.ActionKind,
                        ["metricKey"] = candidate.MetricKey,
                        ["changeKind"] = candidate.ChangeKind,
                        ["deltaPercent"] = candidate.DeltaPercent,
                        ["direction"] = candidate.Direction,
                        ["themeLabel"] = candidate.ThemeLabel,
                        ["evidence"] = new JsonObject
                        {
                            ["metricKeys"] = new JsonArray(
                                candidate.Evidence.MetricKeys
                                    .Select(key => (JsonNode?)key)
                                    .ToArray()
                            ),
                            ["snapshot"] = snapshot,
                            ["causalEvidence"] = candidate.Evidence.CausalEvidence,
                        },
                    }
                );
            }

            return new JsonObject
            {
                ["thresholdVersion"] = bag.ThresholdVersion,
                ["candidates"] = candidates,
            };
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

            if (!TryReadInsightNarratives(
                    root,
                    out var insightNarratives,
                    out invalidOutput
                ))
            {
                return false;
            }

            enrichment = new WeeklyBriefEnrichment(
                ExecutiveSummary: string.IsNullOrWhiteSpace(executiveSummary)
                    ? null
                    : executiveSummary,
                FeedbackSummary: feedbackSummary,
                ActionWording: actionWording,
                InsightNarratives: insightNarratives
            );
            return true;
        }

        private static bool TryReadInsightNarratives(
            JsonElement root,
            out List<WeeklyBriefInsightNarrative> narratives,
            out bool invalidOutput
        )
        {
            narratives = [];
            invalidOutput = false;

            if (!root.TryGetProperty("insightNarratives", out var element))
            {
                // Pre-v2.1 enrichment fixtures omit the field.
                return true;
            }

            if (element.ValueKind != JsonValueKind.Array)
            {
                invalidOutput = true;
                return false;
            }

            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    invalidOutput = true;
                    return false;
                }

                if (!item.TryGetProperty("candidateId", out var idElement)
                    || idElement.ValueKind != JsonValueKind.String
                    || !item.TryGetProperty("observation", out var obsElement)
                    || obsElement.ValueKind != JsonValueKind.String
                    || !item.TryGetProperty("interpretation", out var interpElement)
                    || interpElement.ValueKind != JsonValueKind.String)
                {
                    invalidOutput = true;
                    return false;
                }

                string? recommendation = null;
                if (item.TryGetProperty("recommendation", out var recElement))
                {
                    if (recElement.ValueKind != JsonValueKind.String)
                    {
                        invalidOutput = true;
                        return false;
                    }

                    recommendation = FeedbackRecoveryDraftStructuredOutput
                        .SanitizeGuestProse(recElement.GetString() ?? string.Empty)
                        .Trim();
                    if (recommendation.Length == 0)
                    {
                        recommendation = null;
                    }
                }

                var candidateId = idElement.GetString()?.Trim() ?? string.Empty;
                var observation = FeedbackRecoveryDraftStructuredOutput
                    .SanitizeGuestProse(obsElement.GetString() ?? string.Empty)
                    .Trim();
                var interpretation = FeedbackRecoveryDraftStructuredOutput
                    .SanitizeGuestProse(interpElement.GetString() ?? string.Empty)
                    .Trim();
                if (candidateId.Length == 0
                    || observation.Length == 0
                    || interpretation.Length == 0)
                {
                    invalidOutput = true;
                    return false;
                }

                narratives.Add(
                    new WeeklyBriefInsightNarrative(
                        candidateId,
                        observation,
                        interpretation,
                        recommendation
                    )
                );
            }

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
                    "insightNarratives",
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
                                            .UnderperformQr,
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
                    ["insightNarratives"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["additionalProperties"] = false,
                            ["required"] = new JsonArray
                            {
                                "candidateId",
                                "observation",
                                "interpretation",
                                "recommendation",
                            },
                            ["properties"] = new JsonObject
                            {
                                ["candidateId"] = new JsonObject
                                {
                                    ["type"] = "string",
                                },
                                ["observation"] = new JsonObject
                                {
                                    ["type"] = "string",
                                },
                                ["interpretation"] = new JsonObject
                                {
                                    ["type"] = "string",
                                },
                                ["recommendation"] = new JsonObject
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
