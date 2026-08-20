namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Server-owned completing live answer copy for Offer path persist.
    /// Overwrites Fake/model body.
    /// </summary>
    public static class AssistantOfferPathPersistCopy
    {
        public const string SuccessTitle = "Offers catalog Draft saved";
        public const string FailureTitle = "Offers catalog Draft not saved";

        public static string SuccessBody(
            string locationName,
            string typeLabel,
            string valueLabel,
            string validityLabel,
            string title,
            bool refusedActivate
        )
        {
            var activateLine = refusedActivate
                ? " I did not activate this Offer. It stays Draft only. Activate later on Offer Details."
                : string.Empty;

            return
                $"I saved an Offers catalog Draft for {locationName}.{activateLine}\n\n"
                + "- **Status:** Draft (not Active)\n"
                + $"- **Location:** {locationName}\n"
                + $"- **Type:** {typeLabel}\n"
                + $"- **Value:** {valueLabel}\n"
                + $"- **Validity:** {validityLabel}\n"
                + $"- **Title:** {title}\n"
                + "- Not attached to a Campaign, recovery, or thank-you.\n\n"
                + "Nothing was issued. Nothing was sent.";
        }

        public static string FailureBody(string failedStep)
            => "I could not save this Offers catalog Draft. "
                + $"The {failedStep} step failed. "
                + AssistantNextTryCopy.Sentence
                + " Retry this send, or create the Offer in Offers.";
    }
}
