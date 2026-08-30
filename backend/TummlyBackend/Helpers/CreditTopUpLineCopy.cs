namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Shared line description for credit-pack Revolut orders and TM invoices.
    /// </summary>
    public static class CreditTopUpLineCopy
    {
        public static string FormatLineDescription(string channel, int quantity)
        {
            var label = channel.Trim().ToLowerInvariant() switch
            {
                "sms" => "SMS",
                "email" => "Email",
                "ai" => "AI",
                _ => channel.Trim(),
            };
            return $"{label} credit pack ({quantity:N0})";
        }
    }
}
