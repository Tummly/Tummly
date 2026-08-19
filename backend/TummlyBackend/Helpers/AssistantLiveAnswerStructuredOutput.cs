using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Azure OpenAI Structured Outputs contract for the Assistant live answer.
    /// Fifth use of FeedbackClassification settings. No stream. Retrieved
    /// allow-list domains are passed in the user payload.
    /// </summary>
    public static class AssistantLiveAnswerStructuredOutput
    {
        public const string SchemaName = "assistant_live_answer";

        public const string HttpClientName = "AzureOpenAIAssistantLiveAnswer";

        private static readonly JsonSerializerOptions RequestJsonOptions = new()
        {
            WriteIndented = false
        };

        public static string BuildRequestJson(
            string deploymentName,
            AssistantLiveAnswerInput input,
            string promptSchemaVersion
        )
        {
            var userPayload = DomainPayload(input.Evidence);
            userPayload["userMessage"] = input.UserMessage;
            userPayload["ownedLocationName"] = input.OwnedLocationName;
            userPayload["periodPhrase"] = input.PeriodPhrase;
            userPayload["caveat"] = input.Caveat;
            userPayload["droppedUnknownSentence"] = input.DroppedUnknownSentence;
            userPayload["compareLocations"] = CompareLocationsPayload(input);
            userPayload["suppressMixedRefusal"] = input.SuppressMixedRefusal;

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
                ["required"] = new JsonArray
                {
                    "answerClass",
                    "title",
                    "body",
                    "actions",
                    "assistantTask"
                },
                ["properties"] = new JsonObject
                {
                    ["answerClass"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray
                        {
                            "grounded",
                            "refusal",
                            "failure",
                            "clarify"
                        }
                    },
                    ["assistantTask"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray(
                            AssistantTask.All
                                .Select(task => (JsonNode)task)
                                .ToArray()
                        )
                    },
                    ["title"] = new JsonObject
                    {
                        ["anyOf"] = new JsonArray
                        {
                            new JsonObject { ["type"] = "string" },
                            new JsonObject { ["type"] = "null" }
                        }
                    },
                    ["body"] = new JsonObject
                    {
                        ["type"] = "string"
                    },
                    ["actions"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["additionalProperties"] = false,
                            ["required"] = new JsonArray
                            {
                                "type",
                                "tab",
                                "sentiment",
                                "detectedTag",
                                "count",
                                "offerId",
                                "guestId",
                                "smartGroup",
                                "marketingEligible",
                                "campaignId",
                            },
                            ["properties"] = new JsonObject
                            {
                                ["type"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["enum"] = new JsonArray(
                                        AssistantActionCatalog.CatalogOrder
                                            .Select(type => (JsonNode)type)
                                            .ToArray()
                                    ),
                                },
                                ["tab"] = NullableString(),
                                ["sentiment"] = NullableString(),
                                ["detectedTag"] = NullableString(),
                                ["count"] = NullableInteger(),
                                ["offerId"] = NullableInteger(),
                                ["guestId"] = NullableInteger(),
                                ["smartGroup"] = NullableString(),
                                ["marketingEligible"] = NullableBoolean(),
                                ["campaignId"] = NullableInteger(),
                            },
                        },
                    },
                }
            };

        public static string BuildSystemPrompt(string promptSchemaVersion)
            => $"""
                You write one complete live answer for an operator AI Assistant.
                Prompt/schema version: {promptSchemaVersion}.

                Return Structured Outputs only. Do not stream.
                Emit exactly one assistantTask: retrieve, create-campaign-draft,
                offer-path, recovery-path, or refuse. Do not invent eligible
                counts. The server binds tools and may overwrite the body on
                create-campaign-draft.

                Every restaurant claim must come from retrieved evidence in the
                user payload. Re-retrieve is already done. Prior assistant text is
                not evidence. Vague time words map to the current Reporting period.
                Title and body must use periodPhrase for windowed facts. Do not
                write a hard-coded "this week".

                Grounded body formatting uses this Markdown allow-list only:
                short level 2 or level 3 headings (## or ###), bold (**text**),
                top-level unordered lists (- item), and top-level ordered lists
                (1. item). Use headings for distinct sections and bold for short
                labels or important values so the answer is easy to scan. Keep
                ordinary prose muted by using plain text. Do not use links, images,
                tables, blockquotes, code, raw HTML, nested lists, or level 1
                headings. Do not put Markdown in title. Refusal, failure, and
                clarify bodies must be plain text without Markdown.

                Allow-list domains: Feedback (including AI classification), offers
                (catalog, Offers Performance, per-offer metrics, linked Campaigns,
                claim/redemption logs in the Reporting period), Campaigns (list,
                summary, eligibility, detail metadata, and message subject/body
                only when the question needs campaign copy),
                Capture location snapshot KPIs (qrScans, feedbackSubmitted,
                marketingOptIns, previous window, per-QR rows), and Home Performance
                overview KPIs (feedbackSubmitted, guestsJoined, qrScans).
                Ground on Location Guest current-state facts in the user payload
                when the operator asks to list guests. Do not say Location Guests
                are inside the Reporting period. If guestsDiscloseSample is true,
                say Location Guest names come from guestsSampleCount of
                guestsTotalCount. Home guestsJoined is a count only.

                Never invent guest email, phone, GuestContact, notes, or ids.
                Never quote email, mobile, Feedback GuestContact, Location Guest
                notes, Feedback internal notes, or per-Feedback opt-out checkboxes.
                Never invent counts. Put counts in the body. No citation footer.

                Do not ground on stubs: Home Offer redemptions, Capture offerClaims.
                Use Offers Performance for claim and redemption counts instead.
                Do not read: CSV export, notes, Campaign templates, Home Latest
                activity, QR configuration, Digital guest links, Capture Archive,
                thank-you attach, Preview-options, Capture overview, Settings,
                Billing, AI credits, or Help Centre.

                Windowed facts (Offers Performance, logs, Capture KPIs, Home KPIs,
                Feedback, accepted Campaign messages) use the Reporting period.
                Current-state facts (catalog, in-flight Campaigns, live eligibility)
                may be used but must not be described as inside the period.

                If offersDiscloseSample is true, say catalog facts come from
                offersCatalogSampleCount of offersCatalogTotalCount.
                If campaignsDiscloseSample is true, say Campaign facts come from
                campaignsListSampleCount of campaignsListTotalCount.
                If discloseSample is true, say Feedback themes come from
                sampleCount of feedbackTotalCount.

                Name Location Guests or Feedback only when the operator asks to
                show or list them. Cap 5 named rows, then "and N more", plus a
                Guest or Feedback Action. Summarise and needs-attention stay
                counts and themes: at most 3 quoted excerpts and no Name list.
                Windowed Feedback facts use Analysis scope. Marketing eligible,
                Guest tags, and Marketing status are current-state: do not say
                those facts are inside the Reporting period.

                Placeholder 4 (poor or negative Feedback and opted in / Marketing
                eligible) is Succeeded Negative Feedback in the Reporting period
                at the scoped Owned location, intersect current Marketing eligible
                (placeholder4GuestRows). Do not use Needs recovery. Do not say
                consent is inside the period.

                Unlinked Feedback (isLinked false) may appear on Feedback lists
                with snapshot Name. That person must not appear on a Location
                Guest list (guestRows). guestRows are Location Guests at the
                scoped Owned location, not only guests linked to the Feedback
                sample.

                Empty evidence is a grounded empty answer: title and body name the
                Owned location and Reporting period. No Actions.

                Mutate asks (create, send, or change records) are a refusal: body
                only, no Actions, no claim the record changed.
                Mixed ask: ground the in-scope allow-list part and add one refuse
                sentence for the out part. Class is grounded if any in-scope facts
                were retrieved. If suppressMixedRefusal is true, return only the
                grounded retrieve part. The service adds interview and refusal copy.

                Actions: choose typed rows only. Do not invent labels or destinations.
                Max three. Catalog order. At most one per type. Navigate only.
                view-feedback-set and prepare-recovery are Feedback evidence Actions.
                view-guests and view-guest are Guest evidence Actions: use them
                only when the answer used Location Guest list facts. view-guest
                only when the answer is about exactly one Location Guest. Do not
                attach view-guests and view-guest together. For Placeholder 4 use
                view-guests with current Marketing eligible; omit Smart group
                rather than Needs recovery or All guests.
                view-offers evidence needs catalog or Offers Performance facts.
                view-offer needs exactly one named catalog offer id; never with
                view-offers. view-campaigns evidence needs Campaign facts; next-step
                view-campaigns and view-offers may appear when the answer recommends
                that flow. view-capture needs Capture snapshot facts and opens the
                Capture location page, not Archive or Capture overview.

                When compareLocations has two or more rows, write one Compare turn
                over periodPhrase. Each compare row has the same six allow-list
                domains as the saved location: Feedback, Location Guests, offers,
                Campaigns, Capture, and Home. Use each row's evidence only for
                that location. Include droppedUnknownSentence and caveat when
                present. Actions must use the saved Analysis scope evidence only
                (the top-level counts), not extra compare locations.
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
            AssistantRetrievedEvidence evidence,
            string userMessage,
            out AssistantLiveAnswerResult? result,
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
                if (!root.TryGetProperty("answerClass", out var classElement)
                    || classElement.ValueKind != JsonValueKind.String
                    || !root.TryGetProperty("body", out var bodyElement)
                    || bodyElement.ValueKind != JsonValueKind.String)
                {
                    invalidOutput = true;
                    return false;
                }

                var answerClassWire = classElement.GetString();
                AssistantMessageClass answerClass;
                try
                {
                    answerClass = AssistantMessageClassExtensions.FromWireString(
                        answerClassWire ?? string.Empty
                    );
                }
                catch (ArgumentOutOfRangeException)
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

                string? title = null;
                if (root.TryGetProperty("title", out var titleElement)
                    && titleElement.ValueKind == JsonValueKind.String)
                {
                    var rawTitle = titleElement.GetString()?.Trim();
                    title = string.IsNullOrEmpty(rawTitle) ? null : rawTitle;
                }

                if (answerClass == AssistantMessageClass.Grounded && title is null)
                {
                    invalidOutput = true;
                    return false;
                }

                if (answerClass != AssistantMessageClass.Grounded)
                {
                    title = null;
                }

                var proposed = ParseActions(root);
                var actions = AssistantActionCatalog.Validate(
                    proposed,
                    answerClass,
                    evidence,
                    AssistantAskIntent.ClassifyGrounded(userMessage)
                );
                var assistantTask = AssistantTask.Retrieve;
                if (root.TryGetProperty("assistantTask", out var taskElement)
                    && taskElement.ValueKind == JsonValueKind.String)
                {
                    assistantTask = AssistantTask.Normalize(taskElement.GetString());
                }

                result = new AssistantLiveAnswerResult.Succeeded(
                    answerClass,
                    title,
                    body,
                    actions,
                    assistantTask
                );
                return true;
            }
        }


        private static JsonArray CompareLocationsPayload(AssistantLiveAnswerInput input)
        {
            var rows = input.CompareLocations ?? [];
            return new JsonArray(
                rows.Select(row =>
                {
                    var node = DomainPayload(row.Evidence);
                    node["ownedLocationName"] = row.LocationName;
                    node["capturePaused"] =
                        row.CaptureStatus == CaptureLocationStatus.Paused;
                    return (JsonNode?)node;
                }).ToArray()
            );
        }

        private static JsonObject DomainPayload(AssistantRetrievedEvidence evidence)
        {
            var feedback = evidence.Feedback;
            var offers = evidence.Offers;
            var campaigns = evidence.Campaigns;
            var capture = evidence.Capture;
            var home = evidence.Home;

            return new JsonObject
            {
                ["feedbackTotalCount"] = feedback.TotalCount,
                ["feedbackSampleCount"] = feedback.SampleCount,
                ["succeededPositive"] = feedback.SucceededPositive,
                ["succeededNeutral"] = feedback.SucceededNeutral,
                ["succeededNegative"] = feedback.SucceededNegative,
                ["needsAttention"] = feedback.NeedsAttention,
                ["discloseSample"] = feedback.DisclosesSample,
                ["tagCounts"] = new JsonArray(
                    feedback.TagCounts
                        .Select(tag => new JsonObject
                        {
                            ["tag"] = tag.Tag,
                            ["count"] = tag.Count,
                        })
                        .ToArray<JsonNode?>()
                ),
                ["rows"] = new JsonArray(
                    feedback.Rows
                        .Select(row => PromptFeedbackRow(row))
                        .ToArray<JsonNode?>()
                ),
                ["guestRows"] = new JsonArray(
                    evidence.Guests.Rows
                        .Select(row => PromptGuestRow(row))
                        .ToArray<JsonNode?>()
                ),
                ["guestsTotalCount"] = evidence.Guests.TotalCount,
                ["guestsSampleCount"] = evidence.Guests.SampleCount,
                ["guestsDiscloseSample"] = evidence.Guests.DisclosesSample,
                ["placeholder4GuestRows"] = new JsonArray(
                    feedback.Placeholder4GuestRows
                        .Select(row => PromptGuestRow(row))
                        .ToArray<JsonNode?>()
                ),
                ["offersCatalogTotalCount"] = offers.CatalogTotalCount,
                ["offersCatalogSampleCount"] = offers.CatalogSampleCount,
                ["offersDiscloseSample"] = offers.DisclosesSample,
                ["offersActiveOffers"] = offers.ActiveOffers,
                ["offersIssued"] = offers.OffersIssued,
                ["offersClaims"] = offers.Claims,
                ["offersRedemptions"] = offers.Redemptions,
                ["offersClaimToRedemptionRate"] = NullableNumber(offers.ClaimToRedemptionRate),
                ["offersCatalog"] = new JsonArray(
                    offers.Catalog
                        .Select(row => new JsonObject
                        {
                            ["id"] = row.Id,
                            ["title"] = row.Title,
                            ["status"] = row.Status,
                            ["createdAt"] = row.CreatedAt.ToString("O"),
                        })
                        .ToArray<JsonNode?>()
                ),
                ["offersPerOfferMetrics"] = new JsonArray(
                    offers.PerOfferMetrics
                        .Select(row => new JsonObject
                        {
                            ["offerId"] = row.OfferId,
                            ["title"] = row.Title,
                            ["claims"] = row.Claims,
                            ["redemptions"] = row.Redemptions,
                            ["redemptionRate"] = NullableNumber(row.RedemptionRate),
                            ["expiredUnused"] = row.ExpiredUnused,
                            ["failedAttempts"] = row.FailedAttempts,
                        })
                        .ToArray<JsonNode?>()
                ),
                ["offersLinkedCampaigns"] = new JsonArray(
                    offers.LinkedCampaigns
                        .Select(row => new JsonObject
                        {
                            ["offerId"] = row.OfferId,
                            ["campaignId"] = row.CampaignId,
                            ["campaignName"] = row.CampaignName,
                            ["status"] = row.Status,
                        })
                        .ToArray<JsonNode?>()
                ),
                ["offersClaimLogs"] = new JsonArray(
                    offers.ClaimLogs
                        .Select(row => LogNode(row))
                        .ToArray<JsonNode?>()
                ),
                ["offersRedemptionLogs"] = new JsonArray(
                    offers.RedemptionLogs
                        .Select(row => LogNode(row))
                        .ToArray<JsonNode?>()
                ),
                ["campaignsListTotalCount"] = campaigns.ListTotalCount,
                ["campaignsListSampleCount"] = campaigns.ListSampleCount,
                ["campaignsDiscloseSample"] = campaigns.DisclosesSample,
                ["campaignsInFlightScheduled"] = campaigns.InFlightScheduled,
                ["campaignsInFlightSending"] = campaigns.InFlightSending,
                ["campaignsMessagesSentAccepted"] = campaigns.MessagesSentAccepted,
                ["campaignsRows"] = new JsonArray(
                    campaigns.Rows
                        .Select(row => new JsonObject
                        {
                            ["id"] = row.Id,
                            ["name"] = row.Name,
                            ["status"] = row.Status,
                            ["createdAt"] = row.CreatedAt.ToString("O"),
                            ["updatedAt"] = row.UpdatedAt.ToString("O"),
                            ["offerId"] = NullableNumber(row.OfferId),
                        })
                        .ToArray<JsonNode?>()
                ),
                ["campaignsEligibility"] = new JsonArray(
                    campaigns.Eligibility
                        .Select(row => new JsonObject
                        {
                            ["campaignId"] = row.CampaignId,
                            ["audienceKey"] = row.AudienceKey,
                            ["evaluable"] = row.Evaluable,
                            ["matched"] = NullableNumber(row.Matched),
                            ["currentlyEligible"] = NullableNumber(row.CurrentlyEligible),
                            ["excluded"] = NullableNumber(row.Excluded),
                        })
                        .ToArray<JsonNode?>()
                ),
                ["campaignsDetails"] = new JsonArray(
                    campaigns.Details
                        .Select(row => new JsonObject
                        {
                            ["id"] = row.Id,
                            ["name"] = row.Name,
                            ["status"] = row.Status,
                            ["messageSubject"] = row.MessageSubject,
                            ["messageBody"] = row.MessageBody,
                            ["audienceKey"] = row.AudienceKey,
                            ["channel"] = row.Channel,
                        })
                        .ToArray<JsonNode?>()
                ),
                ["captureQrScans"] = capture.QrScans,
                ["captureQrScansPrevious"] = capture.QrScansPrevious,
                ["captureFeedbackSubmitted"] = capture.FeedbackSubmitted,
                ["captureFeedbackSubmittedPrevious"] = capture.FeedbackSubmittedPrevious,
                ["captureMarketingOptIns"] = capture.MarketingOptIns,
                ["captureMarketingOptInsPrevious"] = capture.MarketingOptInsPrevious,
                ["captureQrRows"] = new JsonArray(
                    capture.QrRows
                        .Select(row => new JsonObject
                        {
                            ["qrCodeId"] = row.QrCodeId,
                            ["qrType"] = row.QrType,
                            ["status"] = row.Status,
                            ["qrScans"] = row.QrScans,
                            ["feedbackSubmitted"] = row.FeedbackSubmitted,
                            ["marketingOptIns"] = row.MarketingOptIns,
                        })
                        .ToArray<JsonNode?>()
                ),
                ["homeFeedbackSubmitted"] = home.FeedbackSubmitted,
                ["homeFeedbackSubmittedPrevious"] = home.FeedbackSubmittedPrevious,
                ["homeGuestsJoined"] = home.GuestsJoined,
                ["homeGuestsJoinedPrevious"] = home.GuestsJoinedPrevious,
                ["homeQrScans"] = home.QrScans,
                ["homeQrScansPrevious"] = home.QrScansPrevious,
            };
        }

        private static JsonObject PromptFeedbackRow(AssistantFeedbackEvidenceRow row)
            => new()
            {
                ["id"] = row.Id,
                ["createdAt"] = row.CreatedAt.ToString("O"),
                ["guestName"] = row.GuestName,
                ["sentiment"] = row.Sentiment,
                ["classificationStatus"] = row.ClassificationStatus,
                ["detectedTags"] = new JsonArray(
                    row.DetectedTags.Select(tag => (JsonNode)tag).ToArray()
                ),
                ["workflowStatus"] = row.WorkflowStatus,
                ["needsAttention"] = row.NeedsAttention,
                ["qrSource"] = row.QrSource,
                ["contactType"] = row.ContactType,
                ["excerpt"] = row.Excerpt,
                ["feedbackReference"] = row.FeedbackReference,
                ["marketingStatus"] = row.MarketingStatus,
                ["guestTags"] = new JsonArray(
                    row.GuestTags.Select(tag => (JsonNode)tag).ToArray()
                ),
                ["isLinked"] = row.IsLinked,
            };

        private static JsonObject PromptGuestRow(AssistantGuestEvidenceRow row)
            => new()
            {
                ["name"] = row.Name,
                ["marketingStatus"] = row.MarketingStatus,
                ["guestTags"] = new JsonArray(
                    row.GuestTags.Select(tag => (JsonNode)tag).ToArray()
                ),
                ["isMarketingEligible"] = row.IsMarketingEligible,
            };

        private static JsonNode? NullableNumber(int? value)
            => value is int number ? JsonValue.Create(number) : null;

        private static JsonNode? NullableNumber(double? value)
            => value is double number ? JsonValue.Create(number) : null;

        private static JsonObject LogNode(AssistantOfferLogRow row)
            => new()
            {
                ["offerId"] = row.OfferId,
                ["title"] = row.Title,
                ["atUtc"] = row.AtUtc.ToString("O"),
                ["claimCode"] = row.ClaimCode,
            };

        private static JsonObject NullableString()
            => new()
            {
                ["anyOf"] = new JsonArray
                {
                    new JsonObject { ["type"] = "string" },
                    new JsonObject { ["type"] = "null" },
                },
            };

        private static JsonObject NullableInteger()
            => new()
            {
                ["anyOf"] = new JsonArray
                {
                    new JsonObject { ["type"] = "integer" },
                    new JsonObject { ["type"] = "null" },
                },
            };

        private static JsonObject NullableBoolean()
            => new()
            {
                ["anyOf"] = new JsonArray
                {
                    new JsonObject { ["type"] = "boolean" },
                    new JsonObject { ["type"] = "null" },
                },
            };

        private static List<AssistantActionDto> ParseActions(JsonElement root)
        {
            if (!root.TryGetProperty("actions", out var actionsElement)
                || actionsElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var actions = new List<AssistantActionDto>();
            foreach (var item in actionsElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object
                    || !item.TryGetProperty("type", out var typeElement)
                    || typeElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                actions.Add(
                    new AssistantActionDto
                    {
                        Type = typeElement.GetString() ?? string.Empty,
                        Tab = ReadNullableString(item, "tab"),
                        Sentiment = ReadNullableString(item, "sentiment"),
                        DetectedTag = ReadNullableString(item, "detectedTag"),
                        Count = ReadNullableInt(item, "count"),
                        OfferId = ReadNullableInt(item, "offerId"),
                        GuestId = ReadNullableInt(item, "guestId"),
                        SmartGroup = ReadNullableString(item, "smartGroup"),
                        MarketingEligible = ReadNullableBool(item, "marketingEligible"),
                        CampaignId = ReadNullableInt(item, "campaignId"),
                    }
                );
            }

            return actions;
        }

        private static string? ReadNullableString(JsonElement item, string name)
        {
            if (!item.TryGetProperty(name, out var element)
                || element.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var value = element.GetString()?.Trim();
            return string.IsNullOrEmpty(value) ? null : value;
        }

        private static int? ReadNullableInt(JsonElement item, string name)
        {
            if (!item.TryGetProperty(name, out var element)
                || element.ValueKind != JsonValueKind.Number
                || !element.TryGetInt32(out var value))
            {
                return null;
            }

            return value;
        }

        private static bool? ReadNullableBool(JsonElement item, string name)
        {
            if (!item.TryGetProperty(name, out var element)
                || (element.ValueKind != JsonValueKind.True
                    && element.ValueKind != JsonValueKind.False))
            {
                return null;
            }

            return element.GetBoolean();
        }
    }
}
