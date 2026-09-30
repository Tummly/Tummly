namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Prior draft authority: when the conversation already stores a Campaign or
    /// Offer id, mutate-shaped follow-ups patch that row instead of inserting a
    /// second draft. Explicit create / "new" / "another" still creates.
    /// </summary>
    public enum AssistantPriorDraftMode
    {
        CreateNew,
        MutatePrior,
        NoPrior,
    }

    public static class AssistantPriorDraftAuthority
    {
        public static AssistantPriorDraftMode ResolveCampaign(
            int? createdCampaignId,
            string userMessage
        )
        {
            var hasPrior = createdCampaignId is int id && id > 0;
            if (LooksLikeExplicitAnother(userMessage, "campaign"))
            {
                return AssistantPriorDraftMode.CreateNew;
            }

            if (AssistantTaskClassification.LooksLikeCreateCampaignDraft(userMessage))
            {
                return AssistantPriorDraftMode.CreateNew;
            }

            if (LooksLikeMutateCampaignFamily(userMessage))
            {
                return hasPrior
                    ? AssistantPriorDraftMode.MutatePrior
                    : AssistantPriorDraftMode.NoPrior;
            }

            return AssistantPriorDraftMode.CreateNew;
        }

        public static AssistantPriorDraftMode ResolveOffer(
            int? createdOfferId,
            string userMessage
        )
        {
            var hasPrior = createdOfferId is int id && id > 0;
            if (LooksLikeExplicitAnother(userMessage, "offer"))
            {
                return AssistantPriorDraftMode.CreateNew;
            }

            if (LooksLikeMutateOfferFamily(userMessage))
            {
                return hasPrior
                    ? AssistantPriorDraftMode.MutatePrior
                    : AssistantPriorDraftMode.NoPrior;
            }

            return AssistantPriorDraftMode.CreateNew;
        }

        /// <summary>
        /// Audience / channel edit, including short chip follow-ups that omit
        /// mutate verbs ("sms eligible only") and the Campaign noun.
        /// </summary>
        public static bool LooksLikeMutateCampaignFamily(string message)
        {
            if (AssistantTaskClassification.LooksLikeCreateCampaignDraft(message)
                || AssistantTaskClassification.LooksLikeAttachToCampaignIntent(message)
                || AssistantTaskClassification.LooksLikeRemoveOfferFromCampaign(message))
            {
                return false;
            }

            if (AssistantTaskClassification.LooksLikeChangeCampaignAudienceOrChannel(
                    message
                ))
            {
                return true;
            }

            var lower = message.Trim().ToLowerInvariant();
            return ContainsAny(
                lower,
                "email eligible",
                "email-eligible",
                "sms eligible",
                "sms-eligible",
                "email only",
                "sms only"
            );
        }

        /// <summary>
        /// Offer terms edit on the conversation's prior Offers catalog Draft.
        /// </summary>
        public static bool LooksLikeMutateOfferFamily(string message)
        {
            if (AssistantTaskClassification.LooksLikeCreateCampaignDraft(message)
                || AssistantTaskClassification.LooksLikeCreateCampaignWithOffer(message)
                || AssistantTaskClassification.LooksLikeAttachToCampaignIntent(message)
                || AssistantTaskClassification.LooksLikeRemoveOfferFromCampaign(message))
            {
                return false;
            }

            var lower = message.Trim().ToLowerInvariant();
            if (!ContainsAny(
                    lower,
                    "change",
                    "update",
                    "switch",
                    "set the",
                    "set offer",
                    "make it",
                    "make the",
                    "edit"
                ))
            {
                return false;
            }

            return ContainsAny(
                lower,
                "offer",
                "offers",
                "percent",
                "%",
                "discount",
                "validity",
                "valid for",
                "expiry",
                "expire",
                "free item",
                "days after",
                "£",
                " gbp"
            );
        }

        private static bool LooksLikeExplicitAnother(string message, string noun)
        {
            var lower = message.Trim().ToLowerInvariant();
            return ContainsAny(
                lower,
                $"new {noun}",
                $"another {noun}",
                $"a second {noun}",
                $"create a new {noun}",
                $"create another {noun}",
                $"draft a new {noun}",
                $"draft another {noun}"
            );
        }

        private static bool ContainsAny(string lower, params string[] needles)
            => needles.Any(needle => lower.Contains(needle, StringComparison.Ordinal));
    }
}
