namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Billing Reserve adapter for Campaign schedule / send commit (ticket 26).
    /// Production stays unavailable until Billing Phase A ships Reserve.
    /// </summary>
    public interface ICampaignBillingReserve
    {
        /// <summary>
        /// False until Billing CreditLedgerService.Reserve is live — commit must hard-block.
        /// </summary>
        bool IsLive { get; }

        Task<CampaignBillingReserveResult> ReserveAsync(
            CampaignBillingReserveRequest request,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class CampaignBillingReserveRequest
    {
        public required int CampaignId { get; init; }

        public required int LocationId { get; init; }

        public required string Channel { get; init; }

        public required int Units { get; init; }
    }

    public abstract class CampaignBillingReserveResult
    {
        private CampaignBillingReserveResult()
        {
        }

        public sealed class Ok : CampaignBillingReserveResult
        {
            public required string ReservationRef { get; init; }
        }

        public sealed class Failed : CampaignBillingReserveResult
        {
            public required string Message { get; init; }
        }
    }
}
