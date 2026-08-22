using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Helpers;

namespace TummlyBackend.Helpers
{
    public static class AssistantAttentionActions
    {
        public static IReadOnlyList<AssistantActionDto> ForNeedsAttention(
            IReadOnlyList<AssistantHomeNeedsAttentionItem> items
        )
        {
            var hasFeedback = items.Any(item => item.SourceKind == "feedback");
            var feedbackCount = 0;
            if (hasFeedback)
            {
                var title = items.First(item => item.SourceKind == "feedback").Title;
                feedbackCount = ParseFeedbackCount(title);
            }

            var hasCampaigns = items.Any(item => item.SourceKind == "campaign");
            var offerIds = items
                .Where(item => item.SourceKind == "offer" && item.OfferId is int)
                .Select(item => item.OfferId!.Value)
                .Distinct()
                .ToList();

            return TakeCatalog(
                BuildNeedsAttention(hasFeedback, feedbackCount, hasCampaigns, offerIds)
            );
        }

        public static IReadOnlyList<AssistantActionDto> ForRecommendedNextStep(
            string type,
            int? guestId,
            int? offerId
        )
        {
            var actions = type switch
            {
                "review-open-feedback" =>
                [
                    CatalogAction("view-feedback-set"),
                ],
                "thank-or-follow-guest" => guestId is int id
                    ? [CatalogAction("view-guest", guestId: id)]
                    : [CatalogAction("view-guests")],
                "promote-or-fix-offer" => offerId is int id
                    ? [CatalogAction("view-offer", offerId: id)]
                    : [CatalogAction("view-offers")],
                "thank-recent-guests" or "re-engage" or "recovery-follow-up" or "none"
                    => Array.Empty<AssistantActionDto>(),
                _ => Array.Empty<AssistantActionDto>(),
            };
            return TakeCatalog(actions);
        }

        public static IReadOnlyList<AssistantActionDto> Union(
            IReadOnlyList<AssistantActionDto> left,
            IReadOnlyList<AssistantActionDto> right
        )
        {
            var byType = new Dictionary<string, AssistantActionDto>(StringComparer.Ordinal);
            foreach (var action in left.Concat(right))
            {
                if (action.Type.Length == 0 || byType.ContainsKey(action.Type))
                {
                    continue;
                }

                byType[action.Type] = action;
            }

            if (byType.ContainsKey("view-offers") && byType.ContainsKey("view-offer"))
            {
                byType.Remove("view-offer");
            }

            if (byType.ContainsKey("view-guests") && byType.ContainsKey("view-guest"))
            {
                byType.Remove("view-guest");
            }

            return AssistantActionCatalog.CatalogOrder
                .Where(byType.ContainsKey)
                .Select(type => byType[type])
                .Take(AssistantActionCatalog.MaxActions)
                .ToList();
        }

        private static List<AssistantActionDto> BuildNeedsAttention(
            bool hasFeedback,
            int feedbackCount,
            bool hasCampaigns,
            IReadOnlyList<int> offerIds
        )
        {
            var actions = new List<AssistantActionDto>();
            if (hasFeedback)
            {
                actions.Add(
                    CatalogAction(
                        "view-feedback-set",
                        tab: "needs-attention",
                        count: feedbackCount
                    )
                );
            }

            if (hasCampaigns)
            {
                actions.Add(CatalogAction("view-campaigns"));
            }

            if (offerIds.Count == 1)
            {
                actions.Add(CatalogAction("view-offer", offerId: offerIds[0]));
            }
            else if (offerIds.Count >= 2)
            {
                actions.Add(CatalogAction("view-offers"));
            }

            return actions;
        }

        private static IReadOnlyList<AssistantActionDto> TakeCatalog(
            IReadOnlyList<AssistantActionDto> actions
        )
            => AssistantActionCatalog.CatalogOrder
                .Join(
                    actions,
                    type => type,
                    action => action.Type,
                    (_, action) => action,
                    StringComparer.Ordinal
                )
                .Take(AssistantActionCatalog.MaxActions)
                .ToList();

        private static AssistantActionDto CatalogAction(
            string type,
            string? tab = null,
            int? count = null,
            int? offerId = null,
            int? guestId = null
        )
        {
            var action = new AssistantActionDto
            {
                Type = type,
                Tab = tab,
                Count = count,
                OfferId = offerId,
                GuestId = guestId,
            };
            action.Label = type == "view-feedback-set" && count is null
                ? "View feedback"
                : AssistantActionCatalog.LabelFor(action);
            return action;
        }

        private static int ParseFeedbackCount(string title)
        {
            var digits = new string(title.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out var count) ? count : 1;
        }
    }
}
