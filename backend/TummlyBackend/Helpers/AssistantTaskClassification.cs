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

            if (!lower.Contains("campaign", StringComparison.Ordinal))
            {
                return false;
            }

            return ContainsAny(
                lower,
                "draft an",
                "draft a ",
                "create a campaign",
                "create an email",
                "create an sms",
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

        public static bool LooksLikeReplacingTask(string message)
        {
            if (Classify(message) == AssistantTask.Refuse)
            {
                return true;
            }

            return AssistantAskIntent.HasReplacingRetrieveAsk(message)
                && AssistantCreateTargets.Detect(message).Count == 0
                && !LooksLikeCreateCampaignDraft(message);
        }

        public static string ForCreateTargetGap(
            IReadOnlyList<string> options,
            string sourceMessage
        )
        {
            var classified = Classify(sourceMessage);
            if (classified is AssistantTask.CreateCampaignDraft
                or AssistantTask.OfferPath
                or AssistantTask.RecoveryPath)
            {
                return classified;
            }

            if (options.Count == 1)
            {
                return ForCreateTarget(options[0]);
            }

            if (options.Contains(AssistantCreateTargets.Campaign, StringComparer.Ordinal))
            {
                return AssistantTask.CreateCampaignDraft;
            }

            if (options.Contains(AssistantCreateTargets.Offer, StringComparer.Ordinal))
            {
                return AssistantTask.OfferPath;
            }

            if (options.Contains(AssistantCreateTargets.Recovery, StringComparer.Ordinal))
            {
                return AssistantTask.RecoveryPath;
            }

            return classified;
        }

        private static string ForCreateTarget(string target)
            => target switch
            {
                AssistantCreateTargets.Campaign => AssistantTask.CreateCampaignDraft,
                AssistantCreateTargets.Offer => AssistantTask.OfferPath,
                AssistantCreateTargets.Recovery => AssistantTask.RecoveryPath,
                _ => AssistantTask.Retrieve,
            };

        private static bool ContainsAny(string lower, params string[] needles)
            => needles.Any(needle => lower.Contains(needle, StringComparison.Ordinal));
    }
}
