using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Default Recovery SMS billing reserve — not live until ledger Reserve ships.
    /// </summary>
    public sealed class UnavailableRecoverySmsBillingReserve
        : IRecoverySmsBillingReserve
    {
        public bool IsLive => false;

        public Task<RecoverySmsBillingReserveResult> ReserveAsync(
            RecoverySmsBillingReserveRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<RecoverySmsBillingReserveResult>(
                new RecoverySmsBillingReserveResult.Failed
                {
                    Code = "billing_reserve_unavailable",
                    Remaining = 0,
                    Requested = request.Units,
                }
            );
        }

        public Task<RecoverySmsBillingSettleResult> SettleAsync(
            RecoverySmsBillingSettleRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<RecoverySmsBillingSettleResult>(
                new RecoverySmsBillingSettleResult.Failed
                {
                    Message = "Billing Settle is not available.",
                }
            );
        }

        public Task<RecoverySmsBillingReleaseResult> ReleaseAsync(
            RecoverySmsBillingReleaseRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<RecoverySmsBillingReleaseResult>(
                new RecoverySmsBillingReleaseResult.Ok()
            );
        }
    }
}
