namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Server-owned completing live answer copy for Recovery path.
    /// </summary>
    public static class AssistantRecoveryPersistCopy
    {
        public const string SuccessTitle = "Feedback recovery prepared";
        public const string FailureTitle = "Feedback recovery not prepared";

        public static string IntentLabel(string intent)
            => intent switch
            {
                AssistantRecoveryEligibility.IntentRespondToGuest
                    => "Respond to the guest",
                AssistantRecoveryEligibility.IntentRespondAndRecord
                    => "Respond and record an internal action",
                AssistantRecoveryEligibility.IntentInternalOnly
                    => "Record an internal action only",
                AssistantRecoveryEligibility.IntentRecoveryOffer
                    => "Respond with a recovery offer",
                _ => intent,
            };

        public static string SuccessBody(
            string intent,
            string guestName,
            string channelLabel
        )
            => "I prepared a recovery response for "
                + $"{guestName} by {channelLabel}.\n\n"
                + $"- **Intent:** {IntentLabel(intent)}\n"
                + $"- **Channel:** {channelLabel}\n"
                + "- **Status:** not sent\n\n"
                + "Nothing was sent. Review recovery opens the Feedback wizard.";

        public static string SuccessInternalBody(string guestName)
            => "I prepared Record an internal action only for "
                + $"{guestName}.\n\n"
                + $"- **Intent:** {IntentLabel(AssistantRecoveryEligibility.IntentInternalOnly)}\n"
                + "- **Status:** not sent\n\n"
                + "Nothing was sent. Review recovery opens the Feedback wizard.";

        public static string FailureBody(string failedStep)
            => "I could not prepare this recovery response. "
                + $"The {failedStep} step failed. "
                + AssistantNextTryCopy.Sentence
                + " Retry this send, or use Feedback recovery.";

        public static string ResolvedBody(string? guestName = null)
        {
            var subject = string.IsNullOrWhiteSpace(guestName)
                ? "This Feedback"
                : $"This Feedback for {guestName}";
            return subject
                + " is resolved. Reopen it before I can prepare a "
                + "recovery response. No recovery work was stored.";
        }

        public static string EmptyScopeBody(string locationName, string periodPhrase)
            => "There is no Feedback "
                + ScopeWhere(locationName, periodPhrase)
                + ". No recovery work was stored. "
                + AssistantNextTryCopy.Sentence;

        public static string NoNegativeBody(string locationName, string periodPhrase)
            => "There is no negative Feedback "
                + ScopeWhere(locationName, periodPhrase)
                + ". No recovery work was stored. "
                + AssistantNextTryCopy.Sentence;

        public static string ZeroMatchBody(string reason, string locationName, string periodPhrase)
            => reason switch
            {
                AssistantRecoveryIdentity.ReasonNoNegative
                    => NoNegativeBody(locationName, periodPhrase),
                AssistantRecoveryIdentity.ReasonEmpty
                    => EmptyScopeBody(locationName, periodPhrase),
                _ => ZeroMatchBody(),
            };

        private static string ScopeWhere(string locationName, string periodPhrase)
            => string.IsNullOrWhiteSpace(locationName)
                ? $"in {periodPhrase}"
                : $"at {locationName} in {periodPhrase}";

        public static string NoContactBody()
            => "This guest has no Email or SMS contact. I did not prepare a "
                + "guest message. You can Record an internal action only later, "
                + "or use Feedback recovery. I did not create that action.";

        public static string OfferRefusedBody()
            => "I cannot Respond with a recovery offer. Location Guest marketing "
                + "preference does not allow it. A guest message is still allowed "
                + "when contact exists. No recovery work was stored for the offer.";

        public static string UnavailableBody()
            => "I could not check follow-up eligibility for this Feedback. "
                + "No recovery work was stored. "
                + AssistantNextTryCopy.Sentence
                + " Retry this send, or Record an internal action only in "
                + "Feedback recovery.";

        public static string ZeroMatchBody()
            => "I could not match Feedback for this recovery ask. "
                + "Name the guest. "
                + AssistantNextTryCopy.Sentence
                + " No recovery work was stored.";

        public static string InternalUnboundBody()
            => "I did not prepare that recovery intent. Name the "
                + "action category and a note, or use Feedback recovery. "
                + "I will not default that category.";

        public static string OfferUnboundBody()
            => "I did not prepare Respond with a recovery offer. Name the "
                + "Offer to attach, or use Feedback recovery. "
                + AssistantNextTryCopy.Sentence
                + " No recovery work was stored.";

        public static string ChannelLabel(string channel)
            => string.Equals(channel, "sms", StringComparison.OrdinalIgnoreCase)
                ? "SMS"
                : "Email";
    }
}
