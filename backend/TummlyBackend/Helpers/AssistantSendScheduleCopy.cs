namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Server-owned later Refuse send / schedule live answer. Nothing is sent.
    /// </summary>
    public static class AssistantSendScheduleCopy
    {
        public const string Title = "Open to confirm";

        public const string MismatchTitle = "Which Campaign?";

        public static string CampaignBody(string step)
            => string.Equals(step, AssistantSendScheduleAsk.StepSchedule, StringComparison.Ordinal)
                ? "Nothing was sent. Opening Campaign Schedule so you can pick a send time."
                : "This Draft is ready to send. Nothing is sent until you confirm.";

        public static string ReadyToSendBody(
            string campaignName,
            string? channel,
            string? audienceKey,
            int? offerId
        )
        {
            var offerLine = offerId is int id && id > 0
                ? $"Offer {id}"
                : "No Offer";
            return "This Draft is ready to send.\n\n"
                + $"- **Name:** {campaignName}\n"
                + $"- **Channel:** {channel}\n"
                + $"- **Audience:** {audienceKey}\n"
                + $"- **Offer:** {offerLine}\n\n"
                + "Nothing is sent until you confirm.";
        }

        public static string DatePendingTimeBody(string dateLocal)
            => $"The Draft date is {dateLocal}. The time is still open. "
                + "Nothing is scheduled until you confirm a time.";

        public static string RecoveryBody()
            => "Nothing was sent. Opening Feedback recovery Review.";

        public static string NamedMismatchBody(string storedCampaignName)
            => $"This Assistant conversation has the Campaign Draft {storedCampaignName}. "
                + "Should I open that one?";

        public static string OtherTypeBody()
            => "I did not open that stored item. Say send it now for the matching Campaign or Feedback recovery, or use Campaigns and Feedback.";

        public static string OfferActivateBody()
            => "I cannot activate, issue, or redeem an Offer from the AI Assistant.";

        public static string RecoveryScheduleBody()
            => "I cannot schedule a recovery send from the AI Assistant. Open Review recovery when you want to send.";

        public static string OpenFailureBody()
            => "I could not open the confirm path. Nothing was sent.";

        public static string NoStoredBody()
            => "I cannot send or schedule from the AI Assistant.";
    }
}
