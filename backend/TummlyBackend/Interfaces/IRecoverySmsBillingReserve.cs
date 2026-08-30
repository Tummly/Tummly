namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Recovery SMS reserve / settle / release — separate from Campaign billing
    /// (ticket 22 / lock 05).
    /// </summary>
    public interface IRecoverySmsBillingReserve
    {
        bool IsLive { get; }

        Task<RecoverySmsBillingReserveResult> ReserveAsync(
            RecoverySmsBillingReserveRequest request,
            CancellationToken cancellationToken = default
        );

        Task<RecoverySmsBillingSettleResult> SettleAsync(
            RecoverySmsBillingSettleRequest request,
            CancellationToken cancellationToken = default
        );

        Task<RecoverySmsBillingReleaseResult> ReleaseAsync(
            RecoverySmsBillingReleaseRequest request,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class RecoverySmsBillingReserveRequest
    {
        public required int FeedbackId { get; init; }

        public required int LocationId { get; init; }

        public required int Units { get; init; }
    }

    public sealed class RecoverySmsBillingSettleRequest
    {
        public required int FeedbackId { get; init; }

        public required string ReservationRef { get; init; }

        public required int AcceptedUnits { get; init; }
    }

    public sealed class RecoverySmsBillingReleaseRequest
    {
        public required int FeedbackId { get; init; }

        public required string ReservationRef { get; init; }
    }

    public abstract class RecoverySmsBillingReserveResult
    {
        private RecoverySmsBillingReserveResult()
        {
        }

        public sealed class Ok : RecoverySmsBillingReserveResult
        {
            public required string ReservationRef { get; init; }
        }

        public sealed class Failed : RecoverySmsBillingReserveResult
        {
            public required string Code { get; init; }

            public required int Remaining { get; init; }

            public required int Requested { get; init; }
        }
    }

    public abstract class RecoverySmsBillingSettleResult
    {
        private RecoverySmsBillingSettleResult()
        {
        }

        public sealed class Ok : RecoverySmsBillingSettleResult
        {
        }

        public sealed class Failed : RecoverySmsBillingSettleResult
        {
            public required string Message { get; init; }
        }
    }

    public abstract class RecoverySmsBillingReleaseResult
    {
        private RecoverySmsBillingReleaseResult()
        {
        }

        public sealed class Ok : RecoverySmsBillingReleaseResult
        {
        }

        public sealed class Failed : RecoverySmsBillingReleaseResult
        {
            public required string Message { get; init; }
        }
    }
}
