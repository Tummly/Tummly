namespace TummlyBackend.Helpers
{
    public enum AssistantAskKind
    {
        InScope = 0,
        Mutate = 1,
        HelpCentre = 2,
        Mixed = 3,
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

        public static bool IsFullRefusal(AssistantAskKind kind)
            => kind is AssistantAskKind.Mutate or AssistantAskKind.HelpCentre;

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
                "what needs"
            );
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
