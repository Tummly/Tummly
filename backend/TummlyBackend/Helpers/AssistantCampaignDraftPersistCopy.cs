namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Server-owned completing live answer copy for Create Campaign Draft.
    /// Overwrites Fake/model body. Eligible count is never invented.
    /// </summary>
    public static class AssistantCampaignDraftPersistCopy
    {
        public const string SuccessTitle = "Campaign Draft saved";
        public const string UpdatedTitle = "Campaign Draft updated";
        public const string FailureTitle = "Campaign Draft not saved";

        public static string SuccessBody(
            string locationName,
            string channelLabel,
            string audienceLabel,
            int? eligibleCount,
            string campaignName,
            string offerLabel = "No Offer",
            string? offerNote = null,
            string? limitNote = null
        )
            => DraftBody(
                $"I saved a Campaign Draft for {locationName}.\n\n",
                locationName,
                channelLabel,
                audienceLabel,
                eligibleCount,
                campaignName,
                offerLabel,
                offerNote,
                limitNote
            );

        public static string UpdatedBody(
            string locationName,
            string channelLabel,
            string audienceLabel,
            int? eligibleCount,
            string campaignName,
            string offerLabel = "No Offer",
            string? offerNote = null,
            string? limitNote = null
        )
            => DraftBody(
                $"I updated the Campaign Draft for {locationName}.\n\n",
                locationName,
                channelLabel,
                audienceLabel,
                eligibleCount,
                campaignName,
                offerLabel,
                offerNote,
                limitNote
            );

        public static string NoPriorDraftToUpdateBody()
            => "There is no Campaign Draft in this conversation to update. "
                + "Create a Campaign Draft first, or change audience in Campaigns.";

        private static string DraftBody(
            string lead,
            string locationName,
            string channelLabel,
            string audienceLabel,
            int? eligibleCount,
            string campaignName,
            string offerLabel,
            string? offerNote,
            string? limitNote = null
        )
        {
            var countQualifier = string.Equals(
                channelLabel,
                "SMS",
                StringComparison.OrdinalIgnoreCase
            )
                ? "SMS-eligible"
                : "Email-eligible";
            var countLine = eligibleCount is int count
                ? $"{audienceLabel} ({count} {countQualifier})"
                : $"{audienceLabel} (eligible count unavailable)";

            var body =
                lead
                + $"- **Location:** {locationName}\n"
                + $"- **Channel:** {channelLabel}\n"
                + $"- **Audience:** {countLine}\n"
                + $"- **Offer:** {offerLabel}\n"
                + $"- **Name:** {campaignName}\n"
                + "- **Status:** Draft\n\n"
                + "Nothing was sent or scheduled.";

            if (!string.IsNullOrWhiteSpace(offerNote))
            {
                body += $"\n\n{offerNote}";
            }

            if (!string.IsNullOrWhiteSpace(limitNote))
            {
                body += $"\n\n{limitNote}";
            }

            return body;
        }

        public static string FailureBody(string failedStep)
            => "I could not save this Campaign Draft. "
                + $"The {failedStep} step failed. "
                + AssistantNextTryCopy.Sentence
                + " Retry this send, or create the Campaign in Campaigns.";
    }
}
