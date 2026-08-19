namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Server-composed Campaign Draft name for a completing Create Campaign Draft
    /// turn. Distinct from the Assistant conversation title.
    /// </summary>
    public static class AssistantCampaignDraftName
    {
        public const string GoalDefault = "Re-engage inactive guests";

        public static string Compose(
            string goalId,
            string channel,
            string locationName,
            string audienceKey
        )
        {
            if (string.IsNullOrWhiteSpace(locationName))
            {
                return GoalDefault;
            }

            var place = locationName.Trim();
            var channelWord = string.Equals(channel, "sms", StringComparison.OrdinalIgnoreCase)
                ? "SMS"
                : "Email";

            return goalId switch
            {
                "re-engage-inactive" when audienceKey
                    == AssistantCampaignDraftBind.AudienceAllEligible
                    => $"Bring back {channelWord}-eligible guests at {place}",
                "re-engage-inactive"
                    => $"Re-engage {AudiencePhrase(audienceKey)} by {channelWord} at {place}",
                "thank-recent-guests"
                    => $"Thank {AudiencePhrase(audienceKey)} by {channelWord} at {place}",
                "promote-something-new"
                    => $"Promote something new by {channelWord} at {place}",
                "boost-quieter-time"
                    => $"Boost a quieter time by {channelWord} at {place}",
                "follow-up-completed-recovery"
                    => $"Follow up after completed recovery by {channelWord} at {place}",
                _ => $"Campaign by {channelWord} at {place}",
            };
        }

        private static string AudiencePhrase(string audienceKey)
            => AssistantCampaignDraftBind.AudienceLabels.TryGetValue(
                audienceKey,
                out var label
            )
                ? label.ToLowerInvariant()
                : "guests";
    }
}
