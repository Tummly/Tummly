namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Server-owned completing live answer copy for Offer path persist.
    /// Overwrites Fake/model body. Consumes persist-turn attach facts.
    /// </summary>
    public static class AssistantOfferPathPersistCopy
    {
        public const string SuccessTitle = "Offers catalog Draft saved";
        public const string AttachedSuccessTitle = "Offers catalog Offer saved";
        public const string FailureTitle = "Offers catalog Draft not saved";

        public static string TitleFor(string thankYouAttach)
            => string.Equals(thankYouAttach, "attached", StringComparison.Ordinal)
                ? AttachedSuccessTitle
                : SuccessTitle;

        public static string SuccessBody(
            string locationName,
            string typeLabel,
            string valueLabel,
            string validityLabel,
            string title,
            bool refusedActivate,
            string thankYouAttach = "none",
            bool thankYouOfferLive = false
        )
        {
            var catalogFacts =
                $"- **Location:** {locationName}\n"
                + $"- **Type:** {typeLabel}\n"
                + $"- **Value:** {valueLabel}\n"
                + $"- **Validity:** {validityLabel}\n"
                + $"- **Title:** {title}\n";
            var closer = "Nothing was issued. Nothing was sent.";

            if (string.Equals(thankYouAttach, "attached", StringComparison.Ordinal))
            {
                var status = thankYouOfferLive
                    ? "Active"
                    : "Draft (not Active)";
                return
                    $"I saved an Offers catalog Offer for {locationName} and attached it to Guest form thank-you.\n\n"
                    + $"- **Status:** {status}\n"
                    + catalogFacts
                    + "- Attached to Guest form thank-you.\n\n"
                    + closer;
            }

            var activateLine = refusedActivate
                ? " I did not activate this Offer. It stays Draft only. Activate later on Offer Details."
                : string.Empty;

            if (string.Equals(thankYouAttach, "failed", StringComparison.Ordinal))
            {
                return
                    $"I saved an Offers catalog Draft for {locationName}. Guest form thank-you attach failed.{activateLine}\n\n"
                    + "- **Status:** Draft (not Active)\n"
                    + catalogFacts
                    + "- Not attached to Guest form thank-you.\n\n"
                    + AssistantNextTryCopy.Sentence
                    + " Retry attach in Capture Guest experience, or Review offer.\n\n"
                    + closer;
            }

            return
                $"I saved an Offers catalog Draft for {locationName}.{activateLine}\n\n"
                + "- **Status:** Draft (not Active)\n"
                + catalogFacts
                + "- Not attached to a Campaign, recovery, or Guest form thank-you.\n\n"
                + closer;
        }

        public static string FailureBody(string failedStep)
            => "I could not save this Offers catalog Draft. "
                + $"The {failedStep} step failed. "
                + AssistantNextTryCopy.Sentence
                + " Retry this send, or create the Offer in Offers.";
    }
}
