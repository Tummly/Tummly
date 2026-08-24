namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Server-side flow control for in-progress create flows: plain-word
    /// cancellation detection and its confirmation copy. Cancellation only
    /// applies while a gap or target-choice state is stored.
    /// </summary>
    public static class AssistantFlowControl
    {
        public const string CancelConfirmTitle = "Cancelled";

        public const string CancelConfirmBody =
            "Done, I dropped that draft. Tell me what you would like to do next.";

        public static bool IsClearCancel(string message)
        {
            var normalized = message
                .Trim()
                .Trim('.', ',', ';', ':', '!')
                .ToLowerInvariant();
            return normalized is "forget it"
                or "forget that"
                or "never mind"
                or "nevermind"
                or "cancel that"
                or "cancel this"
                or "cancel it"
                or "cancel the draft"
                or "cancel draft"
                or "stop the draft"
                or "forget the draft"
                or "drop it"
                or "scratch that";
        }
    }
}
