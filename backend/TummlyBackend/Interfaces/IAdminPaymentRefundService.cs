namespace TummlyBackend.Interfaces
{
    public interface IAdminPaymentRefundService
    {
        Task<AdminPaymentRefundResult> RefundAsync(
            AdminPaymentRefundRequest request,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class AdminPaymentRefundRequest
    {
        public int RestaurantId { get; init; }

        /// <summary>Original payment order UUID.</summary>
        public string OrderId { get; init; } = string.Empty;

        public int? AmountMinor { get; init; }

        public string IdempotencyKey { get; init; } = string.Empty;

        public int ActorStaffUserId { get; init; }
    }

    public sealed class AdminPaymentRefundResult
    {
        public bool Succeeded { get; init; }

        public string? Code { get; init; }

        public string? RefundOrderId { get; init; }

        public static AdminPaymentRefundResult Ok(string refundOrderId)
        {
            return new AdminPaymentRefundResult
            {
                Succeeded = true,
                RefundOrderId = refundOrderId,
            };
        }

        public static AdminPaymentRefundResult Fail(string code)
        {
            return new AdminPaymentRefundResult
            {
                Succeeded = false,
                Code = code,
            };
        }
    }
}
