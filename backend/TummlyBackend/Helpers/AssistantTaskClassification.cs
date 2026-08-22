namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Shared outcome needles for Fake live-answer and server Gap gates.
    /// Persist still requires the live-answer Assistant task; this must not
    /// upgrade Retrieve or Refuse to a stored Draft.
    /// </summary>
    public static partial class AssistantTaskClassification
    {
        public static string Classify(string userMessage)
        {
            if (AssistantAskIntent.IsHelpCentreAsk(userMessage)
                && (LooksLikeCreateCampaignDraft(userMessage)
                    || LooksLikeCreateCampaignWithOffer(userMessage)
                    || LooksLikeOfferPath(userMessage)
                    || LooksLikeRecoveryPath(userMessage)))
            {
                return AssistantTask.Refuse;
            }

            if (LooksLikeCreateCampaignWithOffer(userMessage))
            {
                return AssistantTask.CreateCampaignWithOffer;
            }

            if (LooksLikeCreateCampaignDraft(userMessage))
            {
                return AssistantTask.CreateCampaignDraft;
            }

            if (LooksLikeRecoveryPath(userMessage))
            {
                return AssistantTask.RecoveryPath;
            }

            if (LooksLikeOfferPath(userMessage))
            {
                return AssistantTask.OfferPath;
            }

            if (AssistantSendScheduleAsk.LooksLikeOfferActivate(userMessage)
                || AssistantSendScheduleAsk.LooksLikeSendOrSchedule(userMessage))
            {
                return AssistantTask.Refuse;
            }

            if (AssistantAskIntent.IsFullRefusal(AssistantAskIntent.Classify(userMessage)))
            {
                return AssistantTask.Refuse;
            }

            return AssistantTask.Retrieve;
        }

        public static bool LooksLikeOfferPath(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            if (LooksLikeOfferRetrieveOnly(lower))
            {
                return false;
            }

            if (AssistantRecoveryIntent.LooksLikeRecoveryAsk(message)
                && !ContainsAny(
                    lower,
                    "offer draft",
                    "offers catalog draft",
                    "create an offer",
                    "create a new offer",
                    "draft an offer",
                    "draft a offer"
                ))
            {
                return false;
            }

            return OfferPathOutcomeRegex().IsMatch(lower)
                || ContainsAny(
                    lower,
                    "offer draft",
                    "offers catalog draft"
                );
        }

        public static bool LooksLikeOfferRetrieveOnly(string lower)
        {
            var retrieve = ContainsAny(
                lower,
                "show me",
                "show ",
                "list ",
                "summarise",
                "summarize",
                "what "
            );
            var offerNoun = ContainsAny(lower, "offer", "offers");
            if (!retrieve || !offerNoun)
            {
                return false;
            }

            return !ContainsAny(
                lower,
                "create",
                "prepare",
                "make a",
                "make an",
                "draft an",
                "draft a ",
                "build",
                "set up",
                "write"
            );
        }

        public static bool LooksLikeRecoveryPath(string message)
            => AssistantRecoveryIntent.LooksLikeRecoveryAsk(message);

        public static bool LooksLikeNewOfferBesideCampaign(string message)
            => ContainsAny(
                message.Trim().ToLowerInvariant(),
                "and an offer",
                "and a new offer",
                "and create an offer",
                "and draft an offer",
                "and an offer draft",
                "plus an offer"
            );

        public static bool LooksLikeCreateCampaignWithOffer(string message)
        {
            if (!LooksLikeCreateCampaignDraft(message)
                || LooksLikeRecoveryPath(message))
            {
                return false;
            }

            if (LooksLikeOfferPath(message)
                || LooksLikeNewOfferBesideCampaign(message))
            {
                return true;
            }

            var terms = AssistantOfferPathTerms.Parse(message);
            return terms.OfferType is not null
                || terms.DiscountPercentage is not null
                || terms.DiscountAmount is not null
                || !string.IsNullOrWhiteSpace(terms.FreeItemText)
                || !string.IsNullOrWhiteSpace(terms.ReplacementItemText)
                || terms.ConflictingBenefits.Count > 0;
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
                && !LooksLikeCreateCampaignDraft(message)
                && !LooksLikeCreateCampaignWithOffer(message)
                && !LooksLikeOfferPath(message)
                && !LooksLikeRecoveryPath(message);
        }

        public static string ForCreateTargetGap(
            IReadOnlyList<string> options,
            string sourceMessage
        )
        {
            var classified = Classify(sourceMessage);
            if (classified is AssistantTask.CreateCampaignDraft
                or AssistantTask.CreateCampaignWithOffer
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

        [System.Text.RegularExpressions.GeneratedRegex(
            @"\b(?:create|draft|prepare|make|build|set\s+up|write)\b.{0,120}?\boffers?\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
            | System.Text.RegularExpressions.RegexOptions.Singleline
            | System.Text.RegularExpressions.RegexOptions.CultureInvariant
        )]
        private static partial System.Text.RegularExpressions.Regex OfferPathOutcomeRegex();
    }
}
