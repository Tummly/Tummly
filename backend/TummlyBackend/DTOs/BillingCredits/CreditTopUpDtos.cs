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

        public string RedirectUrl { get; set; } = string.Empty;
    }
}
