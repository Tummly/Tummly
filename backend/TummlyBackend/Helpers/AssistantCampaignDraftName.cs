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
            string userMessage,
            string channel,
            string locationName
        )
        {
            if (string.IsNullOrWhiteSpace(locationName))
            {
                return GoalDefault;
            }

            var lower = userMessage.Trim().ToLowerInvariant();
            if (lower.Contains("bring back", StringComparison.Ordinal)
                && string.Equals(channel, "email", StringComparison.OrdinalIgnoreCase))
            {
                return $"Bring back Email-eligible guests at {locationName.Trim()}";
            }

            return GoalDefault;
        }
    }
}
