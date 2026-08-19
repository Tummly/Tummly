using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class AssistantActionCatalog
    {
        public const int MaxActions = 3;

        public static readonly HashSet<string> RecoveryIntents = new(StringComparer.Ordinal)
        {
            "respond-to-guest",
            "respond-and-record-internal-action",
            "record-internal-action-only",
            "respond-with-recovery-offer",
        };

        public static readonly string[] CatalogOrder =
        [
            "review-campaign",
            "change-audience",
            "add-offer",
            "review-offer",
            "open-recovery",
            "view-feedback-set",
            "prepare-recovery",
            "view-campaigns",
            "view-offers",
            "view-offer",
            "view-guests",
            "view-guest",
            "view-capture",
        ];

        private static readonly HashSet<string> LiveSmartGroups = new(StringComparer.OrdinalIgnoreCase)
        {
            "all-guests",
            "new-guests",
            "needs-recovery",
            "positive-feedback",
            "dormant-guests",
        };

        public static IReadOnlyList<AssistantActionDto> Validate(
            IEnumerable<AssistantActionDto>? proposed,
            AssistantMessageClass answerClass,
            AssistantRetrievedEvidence evidence,
            AssistantGroundedAsk ask = AssistantGroundedAsk.Summarise
        )
        {
            if (answerClass != AssistantMessageClass.Grounded)
            {
                return [];
            }

            if (evidence.IsEmpty && !GuestListFactsUsed(ask, evidence))
            {
                return [];
            }

            var byType = new Dictionary<string, AssistantActionDto>(StringComparer.Ordinal);
            foreach (var raw in proposed ?? [])
            {
                if (raw.Type is null || !CatalogOrder.Contains(raw.Type))
                {
                    continue;
                }

                if (byType.ContainsKey(raw.Type))
                {
                    continue;
                }

                var normalized = Normalize(raw, evidence, ask);
                if (normalized is null)
                {
                    continue;
                }

                byType[raw.Type] = normalized;
            }

            if (byType.TryGetValue("view-feedback-set", out var set)
                && byType.ContainsKey("prepare-recovery")
                && SameInboxFilter(set))
            {
                byType.Remove("prepare-recovery");
            }

            if (byType.ContainsKey("view-guests") && byType.ContainsKey("view-guest"))
            {
                byType.Remove("view-guest");
            }

            if (byType.ContainsKey("view-offers") && byType.ContainsKey("view-offer"))
            {
                byType.Remove("view-offer");
            }

            return CatalogOrder
                .Where(byType.ContainsKey)
                .Select(type => byType[type])
                .Take(MaxActions)
                .ToList();
        }

        public static IReadOnlyList<AssistantActionDto> ValidateReviewCampaign(
            int? campaignId,
            AssistantMessageClass answerClass,
            string? offerStance,
            int? offerId
        )
        {
            if (answerClass != AssistantMessageClass.Grounded || campaignId is null)
            {
                return [];
            }

            var actions = new List<AssistantActionDto>
            {
                CompletingCampaignAction("review-campaign", campaignId.Value),
                CompletingCampaignAction("change-audience", campaignId.Value),
            };
            if (DraftHasNoOffer(offerStance, offerId))
            {
                actions.Add(CompletingCampaignAction("add-offer", campaignId.Value));
            }

            return actions;
        }

        public static IReadOnlyList<AssistantActionDto> ValidateReviewOffer(
            int? offerId,
            AssistantMessageClass answerClass
        )
        {
            if (answerClass != AssistantMessageClass.Grounded || offerId is null)
            {
                return [];
            }

            return
            [
                new AssistantActionDto
                {
                    Type = "review-offer",
                    Label = LabelFor(new AssistantActionDto { Type = "review-offer" }),
                    OfferId = offerId,
                },
            ];
        }

        public static IReadOnlyList<AssistantActionDto> ValidateOpenRecovery(
            IEnumerable<AssistantActionDto>? proposed,
            AssistantMessageClass answerClass
        )
        {
            if (answerClass != AssistantMessageClass.Grounded)
            {
                return [];
            }

            var candidate = (proposed ?? [])
                .FirstOrDefault(action => action.Type == "open-recovery");
            if (candidate is null)
            {
                return [];
            }

            if (candidate.FeedbackId is null
                || candidate.FeedbackId <= 0
                || candidate.Intent is null
                || !RecoveryIntents.Contains(candidate.Intent))
            {
                return [];
            }

            return
            [
                new AssistantActionDto
                {
                    Type = "open-recovery",
                    Label = LabelFor(new AssistantActionDto { Type = "open-recovery" }),
                    FeedbackId = candidate.FeedbackId,
                    Intent = candidate.Intent,
                },
            ];
        }

        public static IReadOnlyList<AssistantActionDto> DefaultActions(
            string userMessage,
            AssistantRetrievedEvidence evidence
        )
        {
            if (evidence.IsEmpty && evidence.Guests.IsEmpty)
            {
                return [];
            }

            var ask = AssistantAskIntent.ClassifyGrounded(userMessage);
            var proposed = new List<AssistantActionDto>();
            var feedback = evidence.Feedback;

            if (ask is AssistantGroundedAsk.Summarise or AssistantGroundedAsk.ListFeedback)
            {
                if (feedback.TotalCount > 0)
                {
                    proposed.Add(
                        new AssistantActionDto
                        {
                            Type = "view-feedback-set",
                            Count = feedback.TotalCount,
                        }
                    );
                }

                if (feedback.NeedsAttention > 0)
                {
                    proposed.Add(new AssistantActionDto { Type = "prepare-recovery" });
                }
            }

            if (ask == AssistantGroundedAsk.Placeholder4
                && feedback.Placeholder4GuestRows.Count > 0)
            {
                proposed.Add(
                    new AssistantActionDto
                    {
                        Type = "view-guests",
                        MarketingEligible = true,
                    }
                );
            }
            else if (ask == AssistantGroundedAsk.ListGuests
                && evidence.Guests.Rows.Count == 1)
            {
                proposed.Add(
                    new AssistantActionDto
                    {
                        Type = "view-guest",
                        GuestId = evidence.Guests.Rows[0].LocationGuestId,
                    }
                );
            }
            else if (ask == AssistantGroundedAsk.ListGuests
                && evidence.Guests.Rows.Count > 1)
            {
                proposed.Add(
                    new AssistantActionDto
                    {
                        Type = "view-guests",
                        SmartGroup = AssistantAskIntent.LiveSmartGroupFor(userMessage),
                    }
                );
            }

            var lower = userMessage.ToLowerInvariant();
            if (evidence.Campaigns.HasCampaignFacts
                || lower.Contains("campaign", StringComparison.Ordinal))
            {
                proposed.Add(new AssistantActionDto { Type = "view-campaigns" });
            }

            var namedOffer = NamedCatalogOffer(userMessage, evidence.Offers);
            if (namedOffer is not null)
            {
                proposed.Add(
                    new AssistantActionDto
                    {
                        Type = "view-offer",
                        OfferId = namedOffer.Id,
                    }
                );
            }
            else if (evidence.Offers.HasCatalogFacts
                || evidence.Offers.HasPerformanceFacts
                || lower.Contains("offer", StringComparison.Ordinal)
                || lower.Contains("claim", StringComparison.Ordinal)
                || lower.Contains("redemption", StringComparison.Ordinal))
            {
                proposed.Add(new AssistantActionDto { Type = "view-offers" });
            }

            if (evidence.Capture.HasSnapshotFacts
                && (lower.Contains("capture", StringComparison.Ordinal)
                    || lower.Contains("qr", StringComparison.Ordinal)))
            {
                proposed.Add(new AssistantActionDto { Type = "view-capture" });
            }

            return Validate(proposed, AssistantMessageClass.Grounded, evidence, ask);
        }

        public static IReadOnlyList<AssistantActionDto> DefaultFeedbackActions(
            string userMessage,
            AssistantFeedbackEvidence evidence
        )
            => DefaultActions(userMessage, AssistantRetrievedEvidence.FromFeedback(evidence));

        public static string LabelFor(AssistantActionDto action)
        {
            return action.Type switch
            {
                "review-campaign" => "Review campaign draft",
                "change-audience" => "Change audience",
                "add-offer" => "Add Offer",
                "review-offer" => "Review offer draft",
                "open-recovery" => "Review recovery",
                "view-feedback-set" => action.Count == 1
                    ? "View 1 feedback item"
                    : $"View {action.Count ?? 0} feedback items",
                "prepare-recovery" => "Prepare recovery responses",
                "view-campaigns" => "Open Campaigns",
                "view-offers" => "Open Offers",
                "view-offer" => "View offer",
                "view-guests" => "View guests",
                "view-guest" => "View guest",
                "view-capture" => "View Capture",
                _ => action.Type,
            };
        }

        private static AssistantActionDto? Normalize(
            AssistantActionDto raw,
            AssistantRetrievedEvidence evidence,
            AssistantGroundedAsk ask
        )
        {
            // Completing-turn types are attached by the server. The model must not invent them.
            if (raw.Type is "review-campaign"
                or "change-audience"
                or "add-offer"
                or "review-offer"
                or "open-recovery")
            {
                return null;
            }

            if (raw.Type == "view-feedback-set" && evidence.Feedback.TotalCount == 0)
            {
                return null;
            }

            if (raw.Type == "view-offer")
            {
                if (raw.OfferId is null
                    || !evidence.Offers.Catalog.Any(offer => offer.Id == raw.OfferId.Value))
                {
                    return null;
                }
            }

            if (raw.Type == "view-capture" && !evidence.Capture.HasSnapshotFacts)
            {
                return null;
            }

            if (raw.Type == "view-guests" && !GuestListFactsUsed(ask, evidence))
            {
                return null;
            }

            if (raw.Type == "view-guest")
            {
                var guestId = SingleLocationGuestId(ask, evidence);
                if (guestId is null)
                {
                    return null;
                }

                raw.GuestId = guestId;
            }

            var tab = NormalizeTab(raw.Tab);
            var sentiment = NormalizeSentiment(raw.Sentiment);
            var detectedTag = string.IsNullOrWhiteSpace(raw.DetectedTag)
                ? null
                : raw.DetectedTag.Trim();

            var filterCount = 0;
            if (tab is not null)
            {
                filterCount++;
            }

            if (sentiment is not null)
            {
                filterCount++;
            }

            if (detectedTag is not null)
            {
                filterCount++;
            }

            if (raw.Type == "view-feedback-set" && filterCount > 1)
            {
                tab = null;
                sentiment = null;
                detectedTag = LargestOrFirstTag(evidence.Feedback, detectedTag);
            }

            if (raw.Type == "prepare-recovery")
            {
                if (evidence.Feedback.NeedsAttention == 0)
                {
                    return null;
                }

                tab = "needs-attention";
                sentiment = null;
                detectedTag = null;
            }

            var count = raw.Type == "view-feedback-set"
                ? evidence.Feedback.TotalCount
                : raw.Count;

            var smartGroup = raw.Type == "view-guests"
                ? NormalizeSmartGroup(raw.SmartGroup, ask)
                : null;
            var marketingEligible = raw.Type == "view-guests"
                ? NormalizeMarketingEligible(raw.MarketingEligible, ask)
                : null;

            var action = new AssistantActionDto
            {
                Type = raw.Type,
                Tab = raw.Type is "view-feedback-set" or "prepare-recovery" ? tab : null,
                Sentiment = raw.Type == "view-feedback-set" ? sentiment : null,
                DetectedTag = raw.Type == "view-feedback-set" ? detectedTag : null,
                Count = count,
                OfferId = raw.Type == "view-offer" ? raw.OfferId : null,
                GuestId = raw.Type == "view-guest" ? raw.GuestId : null,
                SmartGroup = smartGroup,
                MarketingEligible = marketingEligible,
            };
            action.Label = LabelFor(action);
            return action;
        }

        private static AssistantActionDto CompletingCampaignAction(string type, int campaignId)
            => new()
            {
                Type = type,
                Label = LabelFor(new AssistantActionDto { Type = type }),
                CampaignId = campaignId,
            };

        private static bool DraftHasNoOffer(string? offerStance, int? offerId)
            => offerId is null
                && string.Equals(offerStance, "no-offer", StringComparison.OrdinalIgnoreCase);

        private static bool GuestListFactsUsed(
            AssistantGroundedAsk ask,
            AssistantRetrievedEvidence evidence
        )
        {
            return ask switch
            {
                AssistantGroundedAsk.ListGuests => evidence.Guests.Rows.Count > 0,
                AssistantGroundedAsk.Placeholder4 =>
                    evidence.Feedback.Placeholder4GuestRows.Count > 0,
                _ => false,
            };
        }

        private static int? SingleLocationGuestId(
            AssistantGroundedAsk ask,
            AssistantRetrievedEvidence evidence
        )
        {
            if (ask != AssistantGroundedAsk.ListGuests || evidence.Guests.Rows.Count != 1)
            {
                return null;
            }

            return evidence.Guests.Rows[0].LocationGuestId;
        }

        private static string? NormalizeSmartGroup(string? smartGroup, AssistantGroundedAsk ask)
        {
            if (ask == AssistantGroundedAsk.Placeholder4)
            {
                return null;
            }

            var key = smartGroup?.Trim().ToLowerInvariant();
            if (key is null || !LiveSmartGroups.Contains(key))
            {
                return null;
            }

            return key;
        }

        private static bool? NormalizeMarketingEligible(
            bool? proposed,
            AssistantGroundedAsk ask
        )
        {
            if (ask == AssistantGroundedAsk.Placeholder4)
            {
                return true;
            }

            return proposed;
        }

        private static AssistantOfferCatalogRow? NamedCatalogOffer(
            string userMessage,
            AssistantOffersEvidence offers
        )
        {
            if (offers.Catalog.Count == 0)
            {
                return null;
            }

            var matches = offers.Catalog
                .Where(offer =>
                    userMessage.Contains(offer.Title, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            if (matches.Count == 1)
            {
                return matches[0];
            }

            return null;
        }

        private static bool SameInboxFilter(AssistantActionDto set)
            => set.Tab == "needs-attention"
                && set.Sentiment is null
                && set.DetectedTag is null;

        private static string? NormalizeTab(string? tab)
        {
            var key = tab?.Trim().ToLowerInvariant();
            return key is "all" or "needs-attention" or "new" or "in-progress" or "resolved"
                ? key
                : null;
        }

        private static string? NormalizeSentiment(string? sentiment)
        {
            var key = sentiment?.Trim().ToLowerInvariant();
            return key is "positive" or "neutral" or "negative" ? key : null;
        }

        private static string? LargestOrFirstTag(
            AssistantFeedbackEvidence evidence,
            string? preferred
        )
        {
            if (preferred is not null
                && evidence.TagCounts.Any(tag =>
                    tag.Tag.Equals(preferred, StringComparison.OrdinalIgnoreCase)))
            {
                return preferred;
            }

            return evidence.TagCounts
                .OrderByDescending(tag => tag.Count)
                .ThenBy(tag => tag.Tag, StringComparer.Ordinal)
                .Select(tag => tag.Tag)
                .FirstOrDefault();
        }
    }
}
