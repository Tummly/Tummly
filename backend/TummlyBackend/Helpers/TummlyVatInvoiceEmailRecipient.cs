namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Resolves the invoice PDF mailbox per Billing contacts lock 07 /
    /// CONTEXT <c>Billing email</c>: optional Billing Account mailbox replaces
    /// the Billing contact User email when set.
    /// </summary>
    public static class TummlyVatInvoiceEmailRecipient
    {
        public static string? Resolve(
            string? billingEmail,
            string? billingContactUserEmail
        )
        {
            var mailbox = TrimOrNull(billingEmail);
            if (mailbox != null)
            {
                return mailbox;
            }

            return TrimOrNull(billingContactUserEmail);
        }

        private static string? TrimOrNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }
    }
}
