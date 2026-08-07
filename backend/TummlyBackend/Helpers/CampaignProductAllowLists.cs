namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Closed product sets for Campaign Draft fields and recommendation draftPrefill
    /// (wizard Goal / Audience / Channel / Offer ids — same spirit as recommendation type strip).
    /// </summary>
    public static class CampaignProductAllowLists
    {
        private static readonly HashSet<string> GoalIds =
            new(StringComparer.Ordinal)
            {
                "thank-recent-guests",
                "boost-quieter-time",
                "re-engage-inactive",
                "promote-something-new",
                "follow-up-completed-recovery",
                "custom-campaign",
            };

        private static readonly HashSet<string> AudienceKeys =
            new(StringComparer.Ordinal)
            {
                "all-eligible-guests",
                "new-guests",
                "positive-feedback",
                "offer-not-redeemed",
                "recent-redeemers",
                "no-recent-tummly-activity",
                "completed-recovery-follow-up",
                "saved-group",
                "dormant-guests",
            };

        private static readonly HashSet<string> Channels =
            new(StringComparer.Ordinal) { "email", "sms" };

        private static readonly HashSet<string> OfferStances =
            new(StringComparer.Ordinal)
            {
                "no-offer",
                "existing-offer",
                "create-new-offer",
            };

        public static bool IsAllowedGoalId(string goalId)
            => GoalIds.Contains(goalId);

        public static bool IsAllowedAudienceKey(string audienceKey)
            => AudienceKeys.Contains(audienceKey);

        public static bool IsAllowedChannel(string channel)
            => Channels.Contains(channel);

        public static bool IsAllowedOfferStance(string offerStance)
            => OfferStances.Contains(offerStance);

        public static void EnsureOptionalGoalId(string? goalId)
            => EnsureOptional("goalId", goalId, IsAllowedGoalId);

        public static void EnsureOptionalAudienceKey(string? audienceKey)
            => EnsureOptional("audienceKey", audienceKey, IsAllowedAudienceKey);

        public static void EnsureOptionalChannel(string? channel)
            => EnsureOptional("channel", channel, IsAllowedChannel);

        public static void EnsureOptionalOfferStance(string? offerStance)
            => EnsureOptional("offerStance", offerStance, IsAllowedOfferStance);

        private static void EnsureOptional(
            string fieldName,
            string? value,
            Func<string, bool> isAllowed
        )
        {
            if (value != null && !isAllowed(value))
            {
                throw new ArgumentException(
                    $"{fieldName} '{value}' is not in the product allow-list."
                );
            }
        }
    }
}
