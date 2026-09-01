namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Fixed platform copy for guest-form permissions (ticket 04 / PRD §3).
    /// Feedback follow-up is not operator-editable.
    /// </summary>
    public static class GuestFormConsentCopy
    {
        public const string FeedbackFollowUpWording =
            "They may contact you about your feedback using the contact details you provide.";

        public const string MarketingOptOutHint =
            "Untick here if you would prefer not to receive offers.";
    }

}
