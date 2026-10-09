namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Guest-facing Campaign copy must match the audience. A generic
    /// invitation is replaced when the audience needs a specific voice.
    /// </summary>
    public static class CampaignAudienceVoice
    {
        public static (string Body, string? Subject) Fit(
            string audienceKey,
            string locationName,
            string channel,
            string? subject,
            string body,
            string? offerTitle
        )
        {
            var place = string.IsNullOrWhiteSpace(locationName)
                ? "us"
                : locationName.Trim();
            var sms = string.Equals(channel, "sms", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(
                    audienceKey,
                    AssistantCampaignDraftBind.AudienceNegative,
                    StringComparison.Ordinal
                )
                && CampaignNegativeFeedbackCopy.NeedsGovernedCopy(subject, body))
            {
                return CampaignNegativeFeedbackCopy.Governed(place, channel, offerTitle);
            }

            if (string.Equals(
                    audienceKey,
                    AssistantCampaignDraftBind.AudiencePositive,
                    StringComparison.Ordinal
                )
                && !body.Contains("thank", StringComparison.OrdinalIgnoreCase))
            {
                return sms
                    ? ($"Thank you for visiting {place}. We hope to see you again.", null)
                    : (
                        $"Hello,\n\nThank you for visiting {place}. "
                            + "We are glad you came and we hope to see you again.\n\n"
                            + $"The {place} Team",
                        $"Thank you for visiting {place}"
                    );
            }

            if (string.Equals(
                    audienceKey,
                    AssistantCampaignDraftBind.AudienceDormant,
                    StringComparison.Ordinal
                )
                && !body.Contains("while", StringComparison.OrdinalIgnoreCase)
                && !body.Contains("miss", StringComparison.OrdinalIgnoreCase))
            {
                return sms
                    ? ($"It has been a while since your visit to {place}. You are welcome back.", null)
                    : (
                        $"Hello,\n\nIt has been a while since your last visit to {place}. "
                            + "You are welcome back whenever it suits you.\n\n"
                            + $"The {place} Team",
                        $"It has been a while, from {place}"
                    );
            }

            if (string.Equals(
                    audienceKey,
                    AssistantCampaignDraftBind.AudienceNewGuests,
                    StringComparison.Ordinal
                )
                && !body.Contains("welcome", StringComparison.OrdinalIgnoreCase))
            {
                return sms
                    ? ($"Welcome to {place}. We are glad you visited.", null)
                    : (
                        $"Hello,\n\nWelcome to {place}. We are glad you visited.\n\n"
                            + $"The {place} Team",
                        $"Welcome to {place}"
                    );
            }

            if (HasInventedChannel(body) || HasInventedChannel(subject))
            {
                return sms
                    ? ($"You are welcome at {place}. Reply to this message if you need the team.", null)
                    : (
                        $"Hello,\n\nYou are welcome at {place}. "
                            + "Reply to this email if you need anything from the team.\n\n"
                            + $"The {place} Team",
                        $"You are welcome at {place}"
                    );
            }

            return (body, sms ? null : subject);
        }

        private static bool HasInventedChannel(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var lower = text.ToLowerInvariant();
            return lower.Contains("visit our website", StringComparison.Ordinal)
                || lower.Contains("call us", StringComparison.Ordinal)
                || lower.Contains("follow us on social", StringComparison.Ordinal);
        }
    }
}
