namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Phrase split for Attention Retrieve. Stays Retrieve. All-saved pick-one
    /// stays on ticket 24. Follow-up needles stay on ticket 23.
    /// </summary>
    public enum AssistantAttentionSurface
    {
        None = 0,
        NeedsAttention = 1,
        RecommendedNextStep = 2,
        WeeklyBrief = 3,
        Mix = 4,
    }

    public static class AssistantAttentionAsk
    {
        public static AssistantAttentionSurface Detect(string userMessage)
        {
            var lower = userMessage.Trim().ToLowerInvariant();
            if (lower.Length == 0)
            {
                return AssistantAttentionSurface.None;
            }

            if (ContainsAny(
                    lower,
                    "help me today",
                    "what should i focus on",
                    "focus today"))
            {
                return AssistantAttentionSurface.Mix;
            }

            if (ContainsAny(
                    lower,
                    "what should i do today",
                    "what should i do next"))
            {
                return AssistantAttentionSurface.RecommendedNextStep;
            }

            if (ContainsAny(lower, "weekly brief", "watch next"))
            {
                return AssistantAttentionSurface.WeeklyBrief;
            }

            if (ContainsAny(lower, "last week")
                && !ContainsAny(
                    lower,
                    "compare",
                    "summarise",
                    "summarize",
                    " vs ",
                    " versus "
                ))
            {
                return AssistantAttentionSurface.WeeklyBrief;
            }

            if (ContainsAny(lower, "needs attention", "what needs attention"))
            {
                return AssistantAttentionSurface.NeedsAttention;
            }

            return AssistantAttentionSurface.None;
        }

        public static bool IsAttentionRetrieve(string userMessage)
            => Detect(userMessage) != AssistantAttentionSurface.None;

        private static bool ContainsAny(string lower, params string[] needles)
            => needles.Any(needle => lower.Contains(needle, StringComparison.Ordinal));
    }
}
