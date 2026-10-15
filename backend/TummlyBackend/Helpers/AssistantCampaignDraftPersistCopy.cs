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
        public const string NoPriorDraftTitle = "Create a Draft in this chat first";

        public static string SuccessBody(
            string locationName,
            string channelLabel,
            string audienceLabel,
            int? eligibleCount,
            string campaignName,
            string offerLabel = "No Offer",
            string? offerNote = null,
            string? limitNote = null,
            int? matchedCount = null
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
                limitNote,
                matchedCount
            );

        public static string UpdatedBody(
            string locationName,
            string channelLabel,
            string audienceLabel,
            int? eligibleCount,
            string campaignName,
            string offerLabel = "No Offer",
            string? offerNote = null,
            string? limitNote = null,
            int? matchedCount = null
        )
            => ChangesBody([AudienceChange(audienceLabel, channelLabel, matchedCount, eligibleCount)]);

        public static string AudienceChange(string label, string channel, int? matched, int? eligible)
        {
            var counts = matched.HasValue && eligible.HasValue
                ? $"{matched.Value:N0} matched · {eligible.Value:N0} {channel}-eligible"
                : eligible.HasValue ? $"{eligible.Value:N0} {channel}-eligible" : "Recipient counts unavailable";
            return $"Audience: {label} · {counts}";
        }

        public static string ChangesBody(IReadOnlyList<string> changes)
            => changes.Count == 0
                ? "No Draft fields changed. Draft · Not sent."
                : string.Join("\n", changes) + "\n\nDraft · Not sent.";

        public static string NoPriorDraftToUpdateBody()
            => "There is no Campaign Draft linked to this chat. "
                + "Opening the Campaigns page does not select a Draft for the Assistant.\n\n"
                + "Try: \"Create an Email Campaign Draft for the 10 most recent eligible guests.\" "
                + "After the Draft card appears, you can ask me to change its audience.";

        private static string DraftBody(
            string lead,
            string locationName,
            string channelLabel,
            string audienceLabel,
            int? eligibleCount,
            string campaignName,
            string offerLabel,
            string? offerNote,
            string? limitNote = null,
            int? matchedCount = null
        )
        {
            var countQualifier = string.Equals(
                channelLabel,
                "SMS",
                StringComparison.OrdinalIgnoreCase
            )
                ? "SMS-eligible"
                : "Email-eligible";

            string countLine;
            if (matchedCount.HasValue && eligibleCount.HasValue)
            {
                countLine = $"{audienceLabel} ({matchedCount.Value} matched, {eligibleCount.Value} {countQualifier})";
            }
            else if (eligibleCount is int count)
            {
                countLine = $"{audienceLabel} ({count} {countQualifier})";
            }
            else
            {
                countLine = $"{audienceLabel} (eligible count unavailable)";
            }

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
