using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Stub Recovery SMS billing reserve for tests that assert the 503 hard-block
    /// when reserve is not live.
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
