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
    }
}
