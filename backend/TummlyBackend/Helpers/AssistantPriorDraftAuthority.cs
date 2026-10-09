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

            if (hasPrior
                && AssistantCampaignDraftBind.LooksLikeContinueEarlier(userMessage)
                && !AssistantTaskClassification.LooksLikeCreateCampaignDraft(userMessage))
            {
                return AssistantPriorDraftMode.MutatePrior;
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

            if (LooksLikeBareOfferFill(userMessage))
            {
                return hasPrior
                    ? AssistantPriorDraftMode.MutatePrior
                    : AssistantPriorDraftMode.NoPrior;
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

            if (LooksLikeUseExistingOffer(message))
            {
                return true;
            }

            if (AssistantCampaignDraftBind.TryReadGuestCap(message) is not null
                && !AssistantTaskClassification.LooksLikeCreateCampaignDraft(message)
                && !AssistantTaskClassification.LooksLikeCreateCampaignWithOffer(message))
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

        public static bool LooksLikeUseExistingOffer(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            return lower.Contains("use the", StringComparison.Ordinal)
                && lower.Contains("offer", StringComparison.Ordinal);
        }

        /// <summary>
        /// A short field fill for the open Offer Draft: "5 days", "make it 15%",
        /// "they need to buy food first". A full "Make the offer valid…" sentence
        /// stays a Gap fill and is not this shape.
        /// </summary>
        public static bool LooksLikeBareOfferFill(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            if (lower.Length == 0 || lower.Length > 80)
            {
                return false;
            }

            if (ContainsAny(
                    lower,
                    "make the offer valid",
                    "create an offer",
                    "create a offer",
                    "draft an offer"
                ))
            {
                return false;
            }

            if (AssistantOfferPathTerms.LooksLikeValidityFollowUp(lower))
            {
                return true;
            }

            if (ContainsAny(lower, "make it ", "make them ")
                && lower.Contains('%', StringComparison.Ordinal))
            {
                return true;
            }

            return ContainsAny(
                lower,
                "buy food",
                "buy something",
                "need to buy"
            );
        }

        /// <summary>
        /// Offer terms edit on the conversation's prior Offers catalog Draft.
        /// End-date / validity fills for an open Gap ("Make the offer valid…")
        /// are not mutate-prior — those resume the Gap instead.
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
            if (AssistantOfferPathTerms.LooksLikeValidityFollowUp(lower))
            {
                return false;
            }

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
