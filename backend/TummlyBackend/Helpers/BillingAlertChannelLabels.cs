namespace TummlyBackend.Helpers
{
    public static class BillingAlertChannelLabels
    {
        public static string LabelFor(string channel)
        {
            return channel switch
            {
                "sms" => "SMS credits",
                "email" => "Email credits",
                "ai" => "AI credits",
                _ => channel,
            };
        }

        /// <summary>
        /// Short channel name for usage email subject/heading
        /// (Email / SMS / AI).
        /// </summary>
        public static string AllowanceKindFor(string channel)
        {
            return channel switch
            {
                "sms" => "SMS",
                "email" => "Email",
                "ai" => "AI",
                _ => channel,
            };
        }
    }
}
