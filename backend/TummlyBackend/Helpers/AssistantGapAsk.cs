namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Product-owned operator wording for a Gap turn. The live-answer body
    /// is never the Gap ask.
    /// </summary>
    public static class AssistantGapAsk
    {
        public const string PreviousDraftDropped =
            "The previous draft ask was dropped.";

        public const string TypeAsk =
            "What should guests get: percent off, money off, a free item, or a replacement?";

        public const string ValueAsk =
            "How much off should guests get, or which item?";

        public const string PercentValueAsk =
            "How much off should guests get?";

        public const string ItemValueAsk =
            "Which item should guests get?";

        public const string RequiredUsageAsk =
            "Must guests buy something first?";

        public const string EndDateAsk =
            "When should the offer end? Send a date, or how many days after a guest gets it.";

        public const string PlacementAsk =
            "Should this Offer show on the thank-you page?";

        public const string ConflictAskPrefix =
            "Which benefit should I keep:";

        public const string LocationAsk =
            "Which venue should this {0} use?";

        public const string ChannelAsk =
            "Should this Campaign Draft use Email or SMS?";

        public const string AudienceAsk =
            "Which guests should this Campaign Draft reach?";

        public const string OfferTitleAskPrefix =
            "Which existing Offer should this Campaign Draft use:";

        public const string CampaignTitleAskPrefix =
            "Which existing Campaign Draft should this attach to:";

        public const string CreateTargetAskPrefix =
            "Which should I create:";

        public const string FeedbackAskPrefix =
            "Which Feedback should I recover:";

        public static string ForOfferTerms(AssistantOfferPathTermsState terms)
        {
            if (terms.ConflictingBenefits.Count >= 2)
            {
                return $"{ConflictAskPrefix} {AssistantCreateLocationGap.Join(terms.ConflictingBenefits)}?";
            }

            var missing = AssistantOfferPathTerms.MissingFields(terms);
            if (missing.Count == 0)
            {
                return EndDateAsk;
            }

            return missing[0] switch
            {
                "type" => TypeAsk,
                "value" => ValueAskFor(terms),
                "required usage" => RequiredUsageAsk,
                "validity" => EndDateAsk,
                "placement" => PlacementAsk,
                _ => EndDateAsk,
            };
        }

        public static string ExplainOfferTerms(AssistantOfferPathTermsState terms)
        {
            var ask = ForOfferTerms(terms);
            if (ask == EndDateAsk)
            {
                return "I still need to know when the offer should end. "
                    + "Send a calendar date, or how many days after a guest gets the offer. "
                    + "I will not pick an end date for you.";
            }

            if (ask == TypeAsk)
            {
                return "I still need to know what guests get. "
                    + "Say percent off, money off, a free item, or a replacement.";
            }

            if (ask == PercentValueAsk || ask == ItemValueAsk || ask == ValueAsk)
            {
                return "I still need the amount or the item. " + ask;
            }

            if (ask == RequiredUsageAsk)
            {
                return "I still need to know whether guests must buy something first. "
                    + RequiredUsageAsk;
            }

            if (ask == PlacementAsk)
            {
                return "I still need to know whether this Offer should show on the thank-you page.";
            }

            return "I still need one choice from you. " + ask;
        }

        public static string ForLocation(string draftNoun)
            => string.Format(LocationAsk, draftNoun);

        public static string ExplainLocation(
            string draftNoun,
            IReadOnlyList<string> options
        )
        {
            var ask = ForLocation(draftNoun);
            if (options.Count == 0)
            {
                return "I still need one venue. " + ask;
            }

            return "More than one venue could match. Which venue: "
                + AssistantCreateLocationGap.Join(options)
                + "?";
        }

        public static string ForBind(string kind, IReadOnlyList<string> options)
            => kind switch
            {
                AssistantGapTurn.KindChannel => ChannelAsk,
                AssistantGapTurn.KindAudience => AudienceAskWith(options),
                AssistantGapTurn.KindOffer =>
                    $"{OfferTitleAskPrefix} {AssistantCreateLocationGap.Join(options)}?",
                AssistantGapTurn.KindCampaignTitle =>
                    $"{CampaignTitleAskPrefix} {AssistantCreateLocationGap.Join(options)}?",
                AssistantGapTurn.KindCreateTarget =>
                    AssistantGapTurn.CreateTargetBody(options),
                AssistantGapTurn.KindFeedback =>
                    $"{FeedbackAskPrefix} {AssistantCreateLocationGap.Join(options)}?",
                _ => ForLocation("Campaign Draft"),
            };

        public static string ExplainBind(string kind, IReadOnlyList<string> options)
        {
            if (kind == AssistantGapTurn.KindChannel)
            {
                return "I still need Email or SMS. " + ChannelAsk;
            }

            if (options.Count >= 2)
            {
                return "I am not sure which of these you mean. "
                    + ForBind(kind, options);
            }

            return "I still need one of these. " + ForBind(kind, options);
        }

        public static bool LooksLikeConfusedPhrase(string message)
        {
            var normalized = message
                .Trim()
                .Trim('.', ',', ';', ':', '!')
                .ToLowerInvariant();
            if (normalized.Length == 0)
            {
                return true;
            }

            return normalized is "what does that mean"
                or "what does this mean"
                or "what do you mean"
                or "i don't know"
                or "i dont know"
                or "don't know"
                or "dont know"
                or "not sure"
                or "huh"
                or "?"
                or "what?"
                or "what";
        }

        public static bool LooksLikeQuestionNamingAsk(
            string message,
            string lastAsk
        )
        {
            var trimmed = message.Trim();
            if (trimmed.Length == 0 || trimmed[^1] != '?')
            {
                return false;
            }

            var lower = trimmed.ToLowerInvariant();
            if (!LooksLikeQuestionLead(lower))
            {
                return false;
            }

            return NamesAsk(lower, lastAsk);
        }

        public static bool ContainsForbiddenSchemaNoun(string body)
        {
            var lower = body.ToLowerInvariant();
            return ContainsWhole(lower, "validity")
                || ContainsWhole(lower, "value")
                || ContainsWhole(lower, "type")
                || lower.Contains("required usage", StringComparison.Ordinal);
        }

        private static string ValueAskFor(AssistantOfferPathTermsState terms)
            => terms.OfferType switch
            {
                "percentage_discount" or "fixed_discount" => PercentValueAsk,
                "free_item" or "replacement_item" => ItemValueAsk,
                _ => ValueAsk,
            };

        private static string AudienceAskWith(IReadOnlyList<string> options)
            => options.Count == 0
                ? AudienceAsk
                : $"{AudienceAsk} {AssistantCreateLocationGap.Join(options)}?";

        private static bool LooksLikeQuestionLead(string lower)
            => lower.StartsWith("what ", StringComparison.Ordinal)
                || lower.StartsWith("when ", StringComparison.Ordinal)
                || lower.StartsWith("which ", StringComparison.Ordinal)
                || lower.StartsWith("how ", StringComparison.Ordinal)
                || lower.StartsWith("where ", StringComparison.Ordinal)
                || lower.StartsWith("do i ", StringComparison.Ordinal)
                || lower.StartsWith("should i ", StringComparison.Ordinal);

        private static bool NamesAsk(string lowerMessage, string lastAsk)
        {
            var askLower = lastAsk.ToLowerInvariant();
            if (askLower.Contains("when should the offer end", StringComparison.Ordinal)
                && (lowerMessage.Contains("end", StringComparison.Ordinal)
                    || lowerMessage.Contains("date", StringComparison.Ordinal)
                    || lowerMessage.Contains("days", StringComparison.Ordinal)))
            {
                return true;
            }

            if (askLower.Contains("what should guests get", StringComparison.Ordinal)
                && lowerMessage.Contains("guests get", StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        private static bool ContainsWhole(string lower, string noun)
        {
            var index = lower.IndexOf(noun, StringComparison.Ordinal);
            while (index >= 0)
            {
                var beforeOk = index == 0 || !char.IsLetter(lower[index - 1]);
                var after = index + noun.Length;
                var afterOk = after >= lower.Length || !char.IsLetter(lower[after]);
                if (beforeOk && afterOk)
                {
                    return true;
                }

                index = lower.IndexOf(noun, index + 1, StringComparison.Ordinal);
            }

            return false;
        }

        public static bool LooksLikeKeepGapAnswer(string message)
        {
            if (LooksLikeConfusedPhrase(message))
            {
                return false;
            }

            if (AssistantAskIntent.IsHelpCentreAsk(message))
            {
                return true;
            }

            if (AssistantSendScheduleAsk.LooksLikeSendOrSchedule(message)
                || AssistantSendScheduleAsk.LooksLikeOfferActivate(message))
            {
                return true;
            }

            if (AssistantAskIntent.IsFullRefusal(AssistantAskIntent.Classify(message)))
            {
                return true;
            }

            return AssistantAskIntent.HasReplacingRetrieveAsk(message);
        }

        public static bool LooksLikeNewCreateDuringGap(string message)
        {
            if (LooksLikeConfusedPhrase(message)
                || AssistantAskIntent.IsHelpCentreAsk(message))
            {
                return false;
            }

            var detected = AssistantCreateTargets.Detect(message);
            // Two named creates always replace the open Gap. One named
            // create does the same unless the send is Retrieve / Refuse.
            if (detected.Count >= 2)
            {
                return true;
            }

            if (LooksLikeKeepGapAnswer(message))
            {
                return false;
            }

            if (detected.Count == 1)
            {
                return true;
            }

            var task = AssistantTaskClassification.Classify(message);
            return task is AssistantTask.CreateCampaignDraft
                or AssistantTask.CreateCampaignWithOffer
                or AssistantTask.OfferPath
                or AssistantTask.RecoveryPath;
        }
    }
}
