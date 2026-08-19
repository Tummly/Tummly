namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Server-owned completing live answer copy for Create Campaign Draft.
    /// Overwrites Fake/model body. Eligible count is never invented.
    /// </summary>
    public static class AssistantCampaignDraftPersistCopy
    {
        public const string SuccessTitle = "Campaign Draft saved";
        public const string FailureTitle = "Campaign Draft not saved";

        public static string SuccessBody(
            string locationName,
            string channelLabel,
            string audienceLabel,
            int? emailEligible,
            string campaignName
        )
        {
            var countLine = emailEligible is int count
                ? $"{audienceLabel} ({count} Email-eligible)"
                : $"{audienceLabel} (eligible count unavailable)";

            return
                $"I saved a Campaign Draft for {locationName}.\n\n"
                + $"- **Location:** {locationName}\n"
                + $"- **Channel:** {channelLabel}\n"
                + $"- **Audience:** {countLine}\n"
                + "- **Offer:** No Offer\n"
                + $"- **Name:** {campaignName}\n"
                + "- **Status:** Draft\n\n"
                + "Nothing was sent or scheduled.";
        }

        public static string FailureBody(string failedStep)
            => "I could not save this Campaign Draft. "
                + $"The {failedStep} step failed. "
                + "Retry this send, or create the Campaign in Campaigns.";
    }
}
