namespace TummlyBackend.Helpers
{
    /// <summary>
    /// The text Tummly actually sends by SMS. The stored reply or Campaign body stays
    /// the operator text. Reservations, segment counts and the provider call use this.
    /// </summary>
    public static class GuestSmsText
    {
        /// <summary>The line that every Campaign SMS ends with (PRD R5.1).</summary>
        public const string CampaignStopFooter = "\nReply STOP to stop texts.";

        /// <summary>
        /// Adds the restaurant name in front of the operator text ("{name}: {body}").
        /// A blank name adds no prefix.
        /// </summary>
        public static string ForReply(string restaurantName, string body)
        {
            var text = body ?? string.Empty;
            var name = (restaurantName ?? string.Empty).Trim();
            return name.Length == 0 ? text : $"{name}: {text}";
        }

        /// <summary>The Campaign SMS text: the reply text plus the STOP footer.</summary>
        public static string ForCampaign(string restaurantName, string body)
        {
            return ForReply(restaurantName, body) + CampaignStopFooter;
        }
    }
}
