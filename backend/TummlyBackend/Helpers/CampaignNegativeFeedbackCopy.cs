namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Guest message for a negative-feedback Campaign. A generic invitation
    /// is replaced. A sincere make-it-right draft is kept.
    /// </summary>
    public static class CampaignNegativeFeedbackCopy
    {
        public static bool NeedsGovernedCopy(string? subject, string body)
        {
            var text = $"{subject}\n{body}".ToLowerInvariant();
            if (HasGenericInvite(text))
            {
                return true;
            }

            return !HasMakeItRight(text);
        }

        public static (string Body, string? Subject) Governed(
            string locationName,
            string channel,
            string? offerTitle
        )
        {
            var place = string.IsNullOrWhiteSpace(locationName)
                ? "us"
                : locationName.Trim();
            var offerLine = string.IsNullOrWhiteSpace(offerTitle)
                ? ""
                : $" We have added {offerTitle.Trim()} for your next visit.";

            if (string.Equals(channel, "sms", StringComparison.OrdinalIgnoreCase))
            {
                return (
                    $"Sorry your last visit to {place} was not right. "
                        + "We would like to make it right. You are welcome back."
                        + offerLine,
                    null
                );
            }

            var body =
                "Hello,\n\n"
                + $"We are sorry your last visit to {place} was not the experience you should have had. "
                + "We would like the chance to make it right."
                + offerLine
                + "\n\nYou are welcome back whenever it suits you. "
                + "If you would like the team to follow up, reply to this email.\n\n"
                + $"We hope to see you again,\n\nThe {place} Team";
            return (body, "We are sorry your visit was not right");
        }

        private static bool HasGenericInvite(string text)
            => text.Contains("latest dish", StringComparison.Ordinal)
                || text.Contains("relaxed atmosphere", StringComparison.Ordinal)
                || text.Contains("friendly service", StringComparison.Ordinal)
                || text.Contains("follow us on social", StringComparison.Ordinal)
                || text.Contains("visit our website", StringComparison.Ordinal)
                || text.Contains("call us", StringComparison.Ordinal)
                || text.Contains("private event", StringComparison.Ordinal)
                || text.Contains("weekend celebration", StringComparison.Ordinal)
                || text.Contains("invite you back", StringComparison.Ordinal)
                || text.Contains("invited back", StringComparison.Ordinal);

        private static bool HasMakeItRight(string text)
            => text.Contains("sorry", StringComparison.Ordinal)
                || text.Contains("make it right", StringComparison.Ordinal)
                || text.Contains("was not right", StringComparison.Ordinal)
                || text.Contains("not the experience", StringComparison.Ordinal)
                || text.Contains("fell short", StringComparison.Ordinal);
    }
}
