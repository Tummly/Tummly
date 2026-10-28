namespace TummlyBackend.DTOs.BillingCredits
{
    public sealed class CreditTopUpRequestDto
    {
        public string Channel { get; set; } = string.Empty;

        public int Quantity { get; set; }
    }

    public sealed class CreditTopUpConfirmDto
    {
        public bool Success { get; set; } = true;

        public string Channel { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string ChannelLabel { get; set; } = string.Empty;

        public string NetLabel { get; set; } = string.Empty;

        public string GrossLabel { get; set; } = string.Empty;

        public string VatLabel { get; set; } = string.Empty;
    }

    public sealed class CreditTopUpPayDto
    {
        public bool Success { get; set; } = true;

        public string Outcome { get; set; } = "pay";

        public string RedirectUrl { get; set; } = string.Empty;
    }

    public sealed class CustomPlanCyclePayDto
    {
        public bool Success { get; set; } = true;

        public string CheckoutUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Optional body for a pending-payment retry: the config of the fresh quote the
    /// owner reviewed (with its quote hash). Empty keeps the saved quote.
    /// </summary>
    public sealed class PendingPaymentRetryRequestDto
    {
        public string? CustomPlanConfigJson { get; set; }
    }

    public sealed class PendingPaymentRetryDto
    {
        public bool Success { get; set; } = true;

        public string CheckoutUrl { get; set; } = string.Empty;
    }
}
