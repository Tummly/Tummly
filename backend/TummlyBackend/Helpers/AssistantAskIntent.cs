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

        private static bool OnlyMutate(string text)
            => LooksLikeMutate(text) && !LooksLikeInScope(text);

        private static bool LooksLikeMutate(string text)
        {
            var lower = text.ToLowerInvariant();
            return ContainsAny(
                lower,
                "create a campaign",
                "create an offer",
                "send an email",
                "send a message",
                "change the record",
                "update the record",
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
            return ContainsAny(
                lower,
                "help centre",
                "help center",
                "how do i",
                "how to use",
                "where is the button",
                "product how-to",
                "how does the dashboard"
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
                "location guest"
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
    }
}
