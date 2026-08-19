namespace TummlyBackend.Helpers
{
    public enum AssistantAskKind
    {
        InScope = 0,
        Mutate = 1,
        HelpCentre = 2,
        Mixed = 3,
    }

    public enum AssistantGroundedAsk
    {
        Summarise = 0,
        ListFeedback = 1,
        ListGuests = 2,
        Placeholder4 = 3,
    }

    public static class AssistantAskIntent
    {
        public static AssistantAskKind Classify(string userMessage)
        {
            var text = userMessage.Trim();
            if (text.Length == 0)
            {
                return AssistantAskKind.InScope;
            }

            var mutate = LooksLikeMutate(text);
            var helpCentre = LooksLikeHelpCentre(text);
            var inScope = LooksLikeInScope(text);

            if (mutate && (inScope || helpCentre is false && !OnlyMutate(text)))
            {
                if (inScope)
                {
                    return AssistantAskKind.Mixed;
                }
            }

            if (helpCentre && inScope)
            {
                return AssistantAskKind.Mixed;
            }

            if (mutate && !inScope)
            {
                return AssistantAskKind.Mutate;
            }

            if (helpCentre && !inScope)
            {
                return AssistantAskKind.HelpCentre;
            }

            if (mutate && inScope)
            {
                return AssistantAskKind.Mixed;
            }

            return AssistantAskKind.InScope;
        }

        public static AssistantGroundedAsk ClassifyGrounded(string userMessage)
        {
            var lower = userMessage.Trim().ToLowerInvariant();
            if (lower.Length == 0)
            {
                return AssistantGroundedAsk.Summarise;
            }

            if (LooksLikePlaceholder4(lower))
            {
                return AssistantGroundedAsk.Placeholder4;
            }

            if (LooksLikeSummarise(lower))
            {
                return AssistantGroundedAsk.Summarise;
            }

            if (LooksLikeListGuests(lower))
            {
                return AssistantGroundedAsk.ListGuests;
            }

            if (LooksLikeListFeedback(lower))
            {
                return AssistantGroundedAsk.ListFeedback;
            }

            return AssistantGroundedAsk.Summarise;
        }

        public static bool IsFullRefusal(AssistantAskKind kind)
            => kind is AssistantAskKind.Mutate or AssistantAskKind.HelpCentre;

        public static bool IsHelpCentreAsk(string text)
            => LooksLikeHelpCentre(text);

        public static bool HasRetrieveAsk(string text)
            => LooksLikeInScope(text);

        public static bool HasExplicitRetrieveAsk(string text)
        {
            var lower = text.Trim().ToLowerInvariant();
            return ContainsAny(
                lower,
                "summarise",
                "summarize",
                "show",
                "list",
                "how many",
                "count",
                "which ",
                "who ",
                "compare",
                "overview",
                "breakdown",
                "performance",
                "trend"
            );
        }

        /// <summary>
        /// Retrieve or compare that replaces an open Gap turn. Omits "which"
        /// / "who" so a Location answer is not treated as a new Retrieve task.
        /// </summary>
        public static bool HasReplacingRetrieveAsk(string text)
        {
            var lower = text.Trim().ToLowerInvariant();
            return ContainsAny(
                lower,
                "summarise",
                "summarize",
                "show",
                "list",
                "how many",
                "compare",
                "overview",
                "breakdown",
                "performance",
                "trend"
            );
        }

        public static bool LooksLikeReport(string text)
        {
            var lower = text.ToLowerInvariant();
            return ContainsWholeWord(lower, "report")
                || ContainsWholeWord(lower, "reports");
        }

        public static string? LiveSmartGroupFor(string userMessage)
        {
            var lower = userMessage.Trim().ToLowerInvariant();
            if (LooksLikePlaceholder4(lower))
            {
                return null;
            }

            if (ContainsAny(lower, "needs recovery"))
            {
                return "needs-recovery";
            }

            if (ContainsAny(lower, "new guests", "new guest"))
            {
                return "new-guests";
            }

            if (ContainsAny(lower, "positive feedback"))
            {
                return "positive-feedback";
            }

            if (ContainsAny(lower, "dormant"))
            {
                return "dormant-guests";
            }

            return null;
        }

        public static bool NeedsCampaignCopy(string userMessage)
        {
            var lower = userMessage.Trim().ToLowerInvariant();
            if (ContainsAny(
                    lower,
                    "campaign message",
                    "campaign copy",
                    "what does the campaign",
                    "campaign say",
                    "campaign subject",
                    "campaign body"
                ))
            {
                return true;
            }

            return ContainsAny(lower, "campaign")
                && ContainsAny(
                    lower,
                    "message body",
                    "message subject",
                    "subject line",
                    "email body",
                    "sms body"
                );
        }

        public static bool LooksLikeStubCounts(string text)
        {
            var lower = text.ToLowerInvariant();
            return ContainsAny(
                lower,
                "home offer redemption",
                "offer redemptions",
                "offerclaims",
                "offer claims"
            );
        }

        public static bool LooksLikeOutOfAllowList(string text)
        {
            var lower = text.ToLowerInvariant();
            return ContainsAny(
                lower,
                "capture overview",
                "campaign template",
                "campaign templates",
                "latest activity",
                "csv",
                "notes",
                "settings",
                "billing",
                "ai credit",
                "reports",
                "help centre",
                "help center",
                "qr configuration",
                "digital guest link",
                "archive",
                "thank-you",
                "preview-options"
            );
        }

        private static bool OnlyMutate(string text)
            => LooksLikeMutate(text) && !LooksLikeInScope(text);

        public static bool LooksLikeMutateAsk(string text)
            => LooksLikeMutate(text);

        private static bool LooksLikeMutate(string text)
        {
            var lower = text.ToLowerInvariant();
            return ContainsAny(
                lower,
                "create a campaign",
                "create an offer",
                "send an email",
                "send a message",
                "schedule a campaign",
                "schedule the campaign",
                "schedule it",
                "issue an offer",
                "issue the offer",
                "issue it",
                "change the record",
                "update the record",
                "change the status",
                "update the status",
                "set the status",
                "delete the",
                "mark this resolved",
                "mark as resolved",
                "write a reply and send",
                "send the recovery"
            );
        }

        private static bool LooksLikeHelpCentre(string text)
        {
            var lower = text.ToLowerInvariant();
            return LooksLikeReport(lower)
                || ContainsAny(
                lower,
                "help centre",
                "help center",
                "how do i",
                "how to use",
                "where is the button",
                "product how-to",
                "how does the dashboard",
                "capture overview",
                "campaign template",
                "campaign templates",
                "latest activity"
            );
        }

        private static bool LooksLikeInScope(string text)
        {
            var lower = text.ToLowerInvariant();
            return ContainsAny(
                lower,
                "feedback",
                "complain",
                "needs attention",
                "summarise",
                "summarize",
                "guest recovery",
                "this week",
                "recently",
                "lately",
                "what needs",
                "list guests",
                "show guests",
                "show guest",
                "list guest",
                "which guests",
                "named guests",
                "who opted",
                "opted in",
                "opted out",
                "marketing eligible",
                "location guest",
                "offers",
                "offers performance",
                "catalog offer",
                "catalogue",
                "catalog",
                "offer redemption",
                "offer claim",
                "claim",
                "redemption",
                "redeem",
                "campaigns",
                "campaign list",
                "campaign summary",
                "campaign message",
                "in-flight",
                "in flight",
                "eligibility",
                "capture",
                "qr scan",
                "qr scans",
                "performance overview",
                "performance",
                "guests joined",
                "feedback submitted"
            );
        }

        private static bool LooksLikePlaceholder4(string lower)
        {
            var listish = ContainsAny(lower, "show", "list");
            var negative = ContainsAny(
                lower,
                "poor feedback",
                "negative feedback",
                "poor",
                "negative"
            );
            var eligible = ContainsAny(
                lower,
                "opted in",
                "marketing eligible",
                "eligible"
            );
            return listish && negative && eligible;
        }

        private static bool LooksLikeSummarise(string lower)
            => ContainsAny(
                lower,
                "summarise",
                "summarize",
                "needs attention",
                "what needs"
            );

        private static bool LooksLikeListGuests(string lower)
        {
            if (ContainsAny(
                    lower,
                    "which guests",
                    "named guests",
                    "who opted out",
                    "list guests",
                    "show guests",
                    "show guest ",
                    "list guest ",
                    "show the guests",
                    "list the guests"
                ))
            {
                return true;
            }

            return ContainsAny(lower, "who are")
                && ContainsAny(lower, "guest");
        }

        private static bool LooksLikeListFeedback(string lower)
        {
            if (!ContainsAny(lower, "feedback"))
            {
                return false;
            }

            return ContainsAny(lower, "show", "list", "who are", "named");
        }

        private static bool ContainsAny(string haystack, params string[] needles)
        {
            foreach (var needle in needles)
            {
                if (haystack.Contains(needle, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsWholeWord(string text, string word)
        {
            var start = 0;
            while ((start = text.IndexOf(word, start, StringComparison.Ordinal)) >= 0)
            {
                var before = start == 0 || !char.IsLetterOrDigit(text[start - 1]);
                var end = start + word.Length;
                var after = end == text.Length || !char.IsLetterOrDigit(text[end]);
                if (before && after)
                {
                    return true;
                }
                start = end;
            }

            return false;
        }
    }
}
