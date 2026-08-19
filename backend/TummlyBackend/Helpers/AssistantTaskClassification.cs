namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Shared outcome needles for Fake live-answer and server Gap gates.
    /// Persist still requires the live-answer Assistant task; this must not
    /// upgrade Retrieve or Refuse to a stored Draft.
    /// </summary>
    public static class AssistantTaskClassification
    {
        public static string Classify(string userMessage)
        {
            if (AssistantAskIntent.IsHelpCentreAsk(userMessage)
                && LooksLikeCreateCampaignDraft(userMessage))
            {
                return AssistantTask.Refuse;
            }

            if (LooksLikeCreateCampaignDraft(userMessage))
            {
                return AssistantTask.CreateCampaignDraft;
            }

            if (AssistantAskIntent.IsFullRefusal(AssistantAskIntent.Classify(userMessage)))
            {
                return AssistantTask.Refuse;
            }

            return AssistantTask.Retrieve;
        }

        public static bool LooksLikeCreateCampaignDraft(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            if (LooksLikeCampaignRetrieveOnly(lower))
            {
                return false;
            }

            return ContainsAny(
                lower,
                "draft an email campaign",
                "draft a campaign",
                "create a campaign draft",
                "create a campaign",
                "create an email campaign",
                "prepare a campaign",
                "make a campaign",
                "make a draft campaign",
                "write a campaign"
            );
        }

        public static bool LooksLikeCampaignRetrieveOnly(string lower)
        {
            var retrieve = ContainsAny(
                lower,
                "show me",
                "show ",
                "list ",
                "summarise",
                "summarize"
            );
            var campaignDraftNoun = lower.Contains("campaign draft", StringComparison.Ordinal)
                || lower.Contains("campaign drafts", StringComparison.Ordinal);
            if (!retrieve || !campaignDraftNoun)
            {
                return false;
            }

            return !ContainsAny(
                lower,
                "create",
                "prepare",
                "make a",
                "draft an",
                "draft a "
            );
        }

        public static bool LooksLikeCreateTurn(string message)
            => AssistantCreateTargets.Detect(message).Count > 0;

        private static bool ContainsAny(string lower, params string[] needles)
            => needles.Any(needle => lower.Contains(needle, StringComparison.Ordinal));
    }
}
