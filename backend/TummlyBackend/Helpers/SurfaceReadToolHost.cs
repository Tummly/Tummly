using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Executes surface read tools against server-bound facts already loaded by
    /// the caller. Model cannot widen scope or fetch PII.
    /// </summary>
    public static class SurfaceReadToolHost
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
        };

        public static IReadOnlyList<AssistantToolCallResult> ExecuteCampaignDraft(
            IReadOnlyList<AssistantToolCallRequest> calls,
            CampaignMessageDraftInput input
        )
        {
            var results = new List<AssistantToolCallResult>(calls.Count);
            foreach (var call in calls)
            {
                results.Add(
                    call.Name switch
                    {
                        SurfaceReadToolCatalog.ReadLocationDisplayName =>
                            Ok(
                                call,
                                new JsonObject
                                {
                                    ["locationName"] = input.LocationName,
                                }
                            ),
                        SurfaceReadToolCatalog.ReadConfirmedOfferFacts =>
                            input.ConfirmedOffer is { } offer
                                ? Ok(call, ConfirmedOfferPayload(offer))
                                : Unavailable(call, "No confirmed offer on this draft."),
                        _ => Unavailable(call, "Unknown tool."),
                    }
                );
            }

            return results;
        }

        public static IReadOnlyList<AssistantToolCallResult> ExecuteRecoveryDraft(
            IReadOnlyList<AssistantToolCallRequest> calls,
            FeedbackRecoveryDraftInput input
        )
        {
            var results = new List<AssistantToolCallResult>(calls.Count);
            foreach (var call in calls)
            {
                results.Add(
                    call.Name switch
                    {
                        SurfaceReadToolCatalog.ReadLocationDisplayName =>
                            Ok(
                                call,
                                new JsonObject
                                {
                                    ["locationName"] = input.LocationName,
                                }
                            ),
                        SurfaceReadToolCatalog.ReadRecoveryFeedbackFacts =>
                            Ok(
                                call,
                                new JsonObject
                                {
                                    ["feedbackComment"] = input.FeedbackComment,
                                    ["sentiment"] = input.Sentiment,
                                    ["issueTags"] = new JsonArray(
                                        input.IssueTags
                                            .Select(tag => (JsonNode?)tag)
                                            .ToArray()
                                    ),
                                    ["guestDisplayName"] = input.GuestDisplayName,
                                    ["confirmedInternalActionCategory"] =
                                        input.ConfirmedInternalActionCategory,
                                    ["confirmedInternalActionNote"] =
                                        input.ConfirmedInternalActionNote,
                                }
                            ),
                        SurfaceReadToolCatalog.ReadConfirmedOfferFacts =>
                            input.ConfirmedOffer is { } offer
                                ? Ok(call, RecoveryOfferPayload(offer))
                                : Unavailable(call, "No confirmed offer on this draft."),
                        _ => Unavailable(call, "Unknown tool."),
                    }
                );
            }

            return results;
        }

        public static IReadOnlyList<AssistantToolCallResult> ExecuteWeeklyBrief(
            IReadOnlyList<AssistantToolCallRequest> calls,
            WeeklyBriefProviderInput input
        )
        {
            var results = new List<AssistantToolCallResult>(calls.Count);
            foreach (var call in calls)
            {
                if (call.Name != SurfaceReadToolCatalog.ReadWeeklyBriefMetrics)
                {
                    results.Add(Unavailable(call, "Unknown tool."));
                    continue;
                }

                var metrics = input.Metrics;
                var detectedTagCounts = new JsonObject();
                foreach (var pair in metrics.DetectedTagCounts)
                {
                    detectedTagCounts[pair.Key] = pair.Value;
                }

                results.Add(
                    Ok(
                        call,
                        new JsonObject
                        {
                            ["locationName"] = input.LocationName,
                            ["weekKey"] = input.WeekKey,
                            ["coverageStartUtc"] = input.CoverageStartUtc.ToString("O"),
                            ["coverageEndUtcExclusive"] =
                                input.CoverageEndUtcExclusive.ToString("O"),
                            ["insightCandidates"] =
                                WeeklyBriefStructuredOutput.ToInsightCandidatesJson(
                                    input.InsightCandidates
                                ),
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
                        }
                    )
                );
            }

            return results;
        }

        public static IReadOnlyList<AssistantToolCallResult> ExecuteHomeRecommendation(
            IReadOnlyList<AssistantToolCallRequest> calls,
            HomeRecommendationProviderInput input
        )
        {
            var results = new List<AssistantToolCallResult>(calls.Count);
            foreach (var call in calls)
            {
                if (call.Name != SurfaceReadToolCatalog.ReadHomeRecommendationMetrics)
                {
                    results.Add(Unavailable(call, "Unknown tool."));
                    continue;
                }

                var metrics = input.Metrics;
                results.Add(
                    Ok(
                        call,
                        new JsonObject
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
                                ["offerNeedsAttentionCount"] =
                                    metrics.OfferNeedsAttentionCount,
                            },
                        }
                    )
                );
            }

            return results;
        }

        public static IReadOnlyList<AssistantToolCallResult> ExecuteCampaignRecommendation(
            IReadOnlyList<AssistantToolCallRequest> calls,
            CampaignRecommendationProviderInput input
        )
        {
            var results = new List<AssistantToolCallResult>(calls.Count);
            foreach (var call in calls)
            {
                if (call.Name != SurfaceReadToolCatalog.ReadCampaignRecommendationMetrics)
                {
                    results.Add(Unavailable(call, "Unknown tool."));
                    continue;
                }

                var metrics = input.Metrics;
                results.Add(
                    Ok(
                        call,
                        new JsonObject
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
                        }
                    )
                );
            }

            return results;
        }

        public static IReadOnlyList<AssistantToolCallResult> ExecuteOfferRecommendation(
            IReadOnlyList<AssistantToolCallRequest> calls,
            OfferRecommendationProviderInput input
        )
        {
            var results = new List<AssistantToolCallResult>(calls.Count);
            foreach (var call in calls)
            {
                if (call.Name != SurfaceReadToolCatalog.ReadOfferRecommendationMetrics)
                {
                    results.Add(Unavailable(call, "Unknown tool."));
                    continue;
                }

                results.Add(
                    Ok(
                        call,
                        new JsonObject
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
                        }
                    )
                );
            }

            return results;
        }

        private static JsonObject ConfirmedOfferPayload(
            CampaignMessageDraftConfirmedOffer offer
        )
            => new()
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

        private static JsonObject RecoveryOfferPayload(
            FeedbackRecoveryDraftConfirmedOffer offer
        )
            => new()
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

        private static AssistantToolCallResult Ok(
            AssistantToolCallRequest call,
            JsonObject payload
        )
            => new(
                call.Id,
                call.Name,
                payload.ToJsonString(JsonOptions)
            );

        private static AssistantToolCallResult Unavailable(
            AssistantToolCallRequest call,
            string reason
        )
            => new(
                call.Id,
                call.Name,
                new JsonObject
                {
                    ["ok"] = false,
                    ["reason"] = reason,
                }.ToJsonString(JsonOptions)
            );
    }
}
