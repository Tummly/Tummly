using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Provider call that times out. The outcome is unknown: the text may have
    /// been sent.
    /// </summary>
    public sealed class ThrowingRecoveryGuestSmsDelivery : IRecoveryGuestSmsDelivery
    {
        private int _callCount;

        public int CallCount => _callCount;

        public Task<RecoveryGuestSmsDeliveryResult> SendAsync(
            string phoneNumber,
            string body,
            CancellationToken cancellationToken = default
        )
        {
            Interlocked.Increment(ref _callCount);
            throw new TimeoutException("The provider did not answer in time.");
        }
    }

    /// <summary>
    /// Live Recovery SMS billing, except that settle is always refused. Reserve and
    /// release go to the real implementation.
    /// </summary>
    public sealed class SettleFailingRecoverySmsBilling : IRecoverySmsBillingReserve
    {
        private readonly IRecoverySmsBillingReserve _inner;

        public SettleFailingRecoverySmsBilling(IRecoverySmsBillingReserve inner)
        {
            _inner = inner;
        }

        public bool IsLive => _inner.IsLive;

        public Task<RecoverySmsBillingReserveResult> ReserveAsync(
            RecoverySmsBillingReserveRequest request,
            CancellationToken cancellationToken = default
        ) => _inner.ReserveAsync(request, cancellationToken);

        public Task<RecoverySmsBillingSettleResult> SettleAsync(
            RecoverySmsBillingSettleRequest request,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<RecoverySmsBillingSettleResult>(
            new RecoverySmsBillingSettleResult.Failed
            {
                Message = "Settle refused in test.",
            }
        );

        public Task<RecoverySmsBillingReleaseResult> ReleaseAsync(
            RecoverySmsBillingReleaseRequest request,
            CancellationToken cancellationToken = default
        ) => _inner.ReleaseAsync(request, cancellationToken);
    }
}
