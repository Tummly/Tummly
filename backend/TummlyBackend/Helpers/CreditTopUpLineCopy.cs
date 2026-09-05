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
                "sms" => "SMS Credits",
                "email" => "Email Credits",
                "ai" => "AI Credits",
                _ => $"{channel.Trim()} Credits",
            };
            return $"{label} {quantity} Topup";
        }
    }
}
