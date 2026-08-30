namespace TummlyBackend.DTOs.Admin
{
    public sealed class AdminPaymentRefundRequestDto
    {
        public int RestaurantId { get; set; }

        /// <summary>Original payment order UUID (<c>SourcePaymentRef</c>).</summary>
        public string OrderId { get; set; } = string.Empty;

        /// <summary>Omit for full refund. Partial while Bindable remains is refused.</summary>
        public int? AmountMinor { get; set; }
    }

    public sealed class AdminPaymentRefundResponseDto
    {
        public bool Success { get; set; }

        public string? RefundOrderId { get; set; }

        public string? Code { get; set; }
    }
}
