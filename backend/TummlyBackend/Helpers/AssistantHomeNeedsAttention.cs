namespace TummlyBackend.Helpers
{
    public sealed record AssistantHomeNeedsAttentionItem(
        string SourceKind,
        string Title,
        string Body,
        string MetaLine,
        int? CampaignId,
        int? OfferId,
        int? Count = null
    );

    /// <summary>
    /// C# twin of Home <c>buildHomeNeedsAttention</c> / mapper for Assistant
    /// Attention Retrieve. Chat lists every current item (no accordion cap).
    /// Home write CTAs are not copied onto Actions.
    /// </summary>
    public static class AssistantHomeNeedsAttention
    {
        public const string FeedbackBody = "Negative feedback is not Resolved.";

        public static string FeedbackTitle(int count)
            => count == 1
                ? "1 feedback item needs attention"
                : $"{count} feedback items need attention";

        public static string CampaignBody(string status)
            => status switch
            {
                "failed" => "This campaign failed.",
                "partially-sent" => "This campaign was only partially sent.",
                _ => "This campaign needs attention.",
            };

        public static string OfferTitle(
            bool hasOpenVoid,
            int? daysUntilExpiry,
            string catalogTitle
        )
        {
            if (hasOpenVoid)
            {
                return "Open void request";
            }

            if (daysUntilExpiry is int days)
            {
                if (days <= 0)
                {
                    return "Offer expires today";
                }

                return days == 1
                    ? "Offer expires in 1 day"
                    : $"Offer expires in {days} days";
            }

            return catalogTitle;
        }

        public static string OfferBody(
            bool hasOpenVoid,
            int pendingVoidCount,
            int? daysUntilExpiry,
            string catalogTitle,
            int claims,
            int redeemed
        )
        {
            var quoted = $"“{catalogTitle}”";
            var claimsLabel = claims == 1 ? "1 claim" : $"{claims} claims";
            var redeemedLabel = redeemed == 1
                ? "1 redemption"
                : $"{redeemed} redemptions";
            var claimsBody = $"{quoted} has {claimsLabel} and {redeemedLabel}";
            if (hasOpenVoid)
            {
                var pending = pendingVoidCount == 1
                    ? "1 pending void request"
                    : $"{pendingVoidCount} pending void requests";
                return $"{quoted} has {pending}.";
            }

            if (daysUntilExpiry is not null)
            {
                return $"{claimsBody} before expiry.";
            }

            return $"{claimsBody}.";
        }

        public static IReadOnlyList<AssistantHomeNeedsAttentionItem> Project(
            string locationName,
            DateTime nowUtc,
            int feedbackCount,
            DateTime? newestFeedbackUtc,
            IReadOnlyList<AssistantHomeNeedsAttentionCampaignFact> campaigns,
            IReadOnlyList<AssistantHomeNeedsAttentionOfferFact> offers
        )
        {
            var rows = new List<AssistantHomeNeedsAttentionItem>();
            if (feedbackCount > 0)
            {
                rows.Add(
                    new AssistantHomeNeedsAttentionItem(
                        "feedback",
                        FeedbackTitle(feedbackCount),
                        FeedbackBody,
                        MetaLine("warning", newestFeedbackUtc, locationName, nowUtc),
                        CampaignId: null,
                        OfferId: null,
                        Count: feedbackCount
                    )
                );
            }

            foreach (var campaign in campaigns
                .OrderByDescending(item => item.UpdatedAtUtc))
            {
                rows.Add(
                    new AssistantHomeNeedsAttentionItem(
                        "campaign",
                        campaign.Name,
                        CampaignBody(campaign.Status),
                        MetaLine(
                            "warning",
                            campaign.UpdatedAtUtc,
                            locationName,
                            nowUtc
                        ),
                        campaign.Id,
                        OfferId: null
                    )
                );
            }

            foreach (var offer in offers
                .OrderByDescending(item => item.MetaAtUtc ?? DateTime.MinValue))
            {
                rows.Add(
                    new AssistantHomeNeedsAttentionItem(
                        "offer",
                        offer.Title,
                        offer.Body,
                        MetaLine(
                            offer.MetaKind,
                            offer.MetaAtUtc,
                            locationName,
                            nowUtc
                        ),
                        CampaignId: null,
                        offer.Id
                    )
                );
            }

            return rows;
        }

        public static string MetaLine(
            string metaKind,
            DateTime? atUtc,
            string locationName,
            DateTime nowUtc
        )
        {
            var prefix = metaKind == "ai" ? "AI" : "Warning";
            var relative = atUtc is null
                ? ""
                : FormatRelativeTime(atUtc.Value, nowUtc);
            var parts = new[] { prefix, relative, locationName }
                .Where(part => part.Length > 0);
            return string.Join(" · ", parts);
        }

        public static string FormatRelativeTime(DateTime atUtc, DateTime nowUtc)
        {
            var then = atUtc.Kind == DateTimeKind.Utc
                ? atUtc
                : DateTime.SpecifyKind(atUtc, DateTimeKind.Utc);
            var now = nowUtc.Kind == DateTimeKind.Utc
                ? nowUtc
                : DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
            var delta = now - then;
            if (delta < TimeSpan.Zero)
            {
                delta = TimeSpan.Zero;
            }

            if (delta.TotalMinutes < 1)
            {
                return "just now";
            }

            var minutes = (int)delta.TotalMinutes;
            if (minutes < 60)
            {
                return minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago";
            }

            var hours = (int)delta.TotalHours;
            if (hours < 24)
            {
                return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
            }

            var days = (int)delta.TotalDays;
            return days == 1 ? "1 day ago" : $"{days} days ago";
        }
    }

    public sealed record AssistantHomeNeedsAttentionCampaignFact(
        int Id,
        string Name,
        string Status,
        DateTime UpdatedAtUtc
    );

    public sealed record AssistantHomeNeedsAttentionOfferFact(
        int Id,
        string Title,
        string Body,
        string MetaKind,
        DateTime? MetaAtUtc
    );
}
