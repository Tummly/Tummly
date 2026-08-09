namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Billing Reserve / Settle / Release adapter for Campaign schedule, fire,
    /// and lifecycle controls (tickets 26 / 30 / 31). Production stays unavailable
    /// until Billing Phase A ships Reserve·Settle.
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

        /// <summary>Settle accepted provider units against the open reservation.</summary>
        Task<CampaignBillingSettleResult> SettleAsync(
            CampaignBillingSettleRequest request,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Release an unused / dead reservation (Unschedule, Cancel, pause,
        /// Failed cannot-start, eligibility shrink).
        /// </summary>
        Task<CampaignBillingReleaseResult> ReleaseAsync(
            CampaignBillingReleaseRequest request,
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

    public sealed class CampaignBillingSettleRequest
    {
        public required int CampaignId { get; init; }

        public required string ReservationRef { get; init; }

        public required string Channel { get; init; }

        /// <summary>Accepted provider units only.</summary>
        public required int AcceptedUnits { get; init; }
    }

    public abstract class CampaignBillingSettleResult
    {
        private CampaignBillingSettleResult()
        {
        }

        public sealed class Ok : CampaignBillingSettleResult
        {
        }

        public sealed class Failed : CampaignBillingSettleResult
        {
            public required string Message { get; init; }
        }
    }

    public sealed class CampaignBillingReleaseRequest
    {
        public required int CampaignId { get; init; }

        public required string ReservationRef { get; init; }
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

    public abstract class CampaignBillingReleaseResult
    {
        private CampaignBillingReleaseResult()
        {
        }

        public sealed class Ok : CampaignBillingReleaseResult
        {
        }

        public sealed class Failed : CampaignBillingReleaseResult
        {
            public required string Message { get; init; }
        }
    }
}
