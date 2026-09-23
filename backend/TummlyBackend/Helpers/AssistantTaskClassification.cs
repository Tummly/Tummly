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
            if (LooksLikeRecoveryPath(message))
            {
                return false;
            }

            if (LooksLikeAttachToCampaignIntent(message)
                || LooksLikeRemoveOfferFromCampaign(message))
            {
                return true;
            }

            if (!LooksLikeCreateCampaignDraft(message))
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

        public static bool LooksLikeAttachToCampaignIntent(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            if (!ContainsAny(
                    lower,
                    "attach",
                    "attach it",
                    "attach to"
                ))
            {
                return false;
            }

            return ContainsAny(lower, "campaign");
        }

        /// <summary>
        /// Attach to an existing Campaign Draft without creating a new Campaign.
        /// True for bare attach asks; false when the ask also creates a Campaign.
        /// Does not treat Offer titles that contain "Draft" as a create ask.
        /// </summary>
        public static bool LooksLikeAttachOnlyToCampaign(string message)
        {
            if (!LooksLikeAttachToCampaignIntent(message)
                || LooksLikeRemoveOfferFromCampaign(message))
            {
                return false;
            }

            var lower = message.Trim().ToLowerInvariant();
            // Explicit create verbs only — CreateCampaignOutcomeRegex also matches
            // bare "draft … campaign", which false-positives Offer titles like
            // "Live Draft Nine".
            return !ContainsAny(
                lower,
                "create a campaign",
                "create campaign",
                "can you create a campaign",
                "start a campaign",
                "help me create a campaign",
                "create an email",
                "create an sms",
                "prepare a campaign",
                "make a campaign",
                "make a draft campaign",
                "write a campaign",
                "build a campaign",
                "draft a campaign",
                "draft an campaign"
            );
        }

        /// <summary>
        /// Clear the Offer attach on an existing Campaign Draft
        /// ("remove the offer from Summer campaign").
        /// </summary>
        public static bool LooksLikeRemoveOfferFromCampaign(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            if (!NamesCampaignNoun(lower))
            {
                return false;
            }

            if (!ContainsAny(lower, "offer", "offers"))
            {
                return false;
            }

            return ContainsAny(
                lower,
                "remove",
                "detach",
                "unattach",
                "clear the offer",
                "clear offer",
                "take off the offer",
                "take the offer off"
            );
        }

        /// <summary>
        /// True when the ask names a catalog Offer between attach and to
        /// (for example "Attach Happy Hour to Summer campaign"), so a miss
        /// must Gap instead of falling back to <c>CreatedOfferId</c>.
        /// </summary>
        public static bool LooksLikeNamedOfferAttachAsk(string message)
        {
            var match = NamedOfferAttachRegex().Match(message.Trim());
            if (!match.Success)
            {
                return false;
            }

            var fragment = match.Groups[1].Value.Trim();
            if (fragment.Length == 0)
            {
                return false;
            }

            var lower = fragment.ToLowerInvariant();
            return lower is not "it"
                and not "this"
                and not "that"
                and not "this offer"
                and not "that offer"
                and not "the offer";
        }

        /// <summary>
        /// True when the ask refers to the conversation's prior Offer
        /// ("attach it", "this offer") rather than a named catalog title.
        /// </summary>
        public static bool LooksLikeReferToPriorCreatedOffer(string message)
        {
            if (LooksLikeNamedOfferAttachAsk(message))
            {
                return false;
            }

            var lower = message.Trim().ToLowerInvariant();
            if (lower.Contains("this offer", StringComparison.Ordinal)
                || lower.Contains("that offer", StringComparison.Ordinal)
                || lower.Contains("attach it", StringComparison.Ordinal))
            {
                return true;
            }

            return LooksLikeAttachToCampaignIntent(message);
        }

        /// <summary>
        /// Attach an existing Offer (or conversation CreatedOfferId) without
        /// creating a new Offers catalog Draft from commercial terms.
        /// </summary>
        public static bool LooksLikeAttachExistingOfferOnly(string message)
        {
            if (!LooksLikeAttachToCampaignIntent(message))
            {
                return false;
            }

            if (LooksLikeOfferPath(message))
            {
                return false;
            }

            var terms = AssistantOfferPathTerms.Parse(message);
            return terms.OfferType is null
                && terms.DiscountPercentage is null
                && terms.DiscountAmount is null
                && string.IsNullOrWhiteSpace(terms.FreeItemText)
                && string.IsNullOrWhiteSpace(terms.ReplacementItemText);
        }

        public static bool LooksLikeCreateCampaignDraft(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            if (LooksLikeCampaignRetrieveOnly(lower))
            {
                return false;
            }

            if (!NamesCampaignNoun(lower))
            {
                return false;
            }

            return ContainsAny(
                lower,
                "draft an",
                "draft a ",
                "create a campaign",
                "create campaign",
                "can you create a campaign",
                "start a campaign",
                "help me create a campaign",
                "create an email",
                "create an sms",
                "prepare a campaign",
                "make a campaign",
                "make a draft campaign",
                "write a campaign"
            )
            || CreateCampaignOutcomeRegex().IsMatch(lower);
        }

        /// <summary>
        /// Campaign noun including common operator typos from QA history.
        /// </summary>
        private static bool NamesCampaignNoun(string lower)
            => ContainsAny(
                lower,
                "campaign",
                "campaigns",
                "camapgin",
                "campagin",
                "campaing",
                "camapaign",
                "campagn"
            );

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
            var campaignDraftNoun = NamesCampaignNoun(lower)
                && ContainsAny(lower, "draft", "drafts");
            if (!retrieve || !campaignDraftNoun)
            {
                return false;
            }

            return !ContainsAny(
                lower,
                "create",
                "creaete",
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

        /// <summary>
        /// Create/draft/start + campaign noun (including common typos).
        /// </summary>
        [System.Text.RegularExpressions.GeneratedRegex(
            @"\b(?:create|creaete|draft|prepare|make|build|set\s+up|write|start|help\s+me\s+create)\b.{0,80}?\b(?:campaign|campaigns|camapgin|campagin|campaing|camapaign|campagn)s?\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
            | System.Text.RegularExpressions.RegexOptions.Singleline
            | System.Text.RegularExpressions.RegexOptions.CultureInvariant
        )]
        private static partial System.Text.RegularExpressions.Regex CreateCampaignOutcomeRegex();

        /// <summary>
        /// "Attach Happy Hour to Summer campaign" — capture the Offer name
        /// between attach and to.
        /// </summary>
        [System.Text.RegularExpressions.GeneratedRegex(
            @"attach\s+(.+?)\s+to\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
            | System.Text.RegularExpressions.RegexOptions.CultureInvariant
        )]
        private static partial System.Text.RegularExpressions.Regex NamedOfferAttachRegex();
    }
}
