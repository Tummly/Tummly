using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Test double for AuthService paths that must not touch the ledger.
    /// MintPilot returns Ok so ActivateAccountAsync can succeed without grants.
    /// </summary>
    internal sealed class NoOpCreditLedger : ICreditLedger
    {
        public Task<CreditLedgerWriteResult> ConsumeOnSuccessAsync(
            CreditLedgerConsumeRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));
        }

        public Task<CreditLedgerWriteResult> StaffManualAdjustAsync(
            StaffManualAdjustRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));
        }

        public Task<CreditLedgerWriteResult> StaffReverseAsync(
            StaffReverseRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));
        }

        public Task<CreditLedgerMintTopupResult> MintTopupAllocationAsync(
            CreditLedgerMintTopupRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(CreditLedgerMintTopupResult.Fail("not_implemented"));
        }

        public Task<CreditLedgerDrainTopupResult> DrainUnusedTopupAsync(
            CreditLedgerDrainTopupRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(CreditLedgerDrainTopupResult.Fail("not_implemented"));
        }

        public Task<CreditLedgerRestoreTopupResult> RestoreUnusedTopupAsync(
            CreditLedgerRestoreTopupRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(CreditLedgerRestoreTopupResult.Fail("not_implemented"));
        }

        public Task<CreditLedgerWriteResult> ReleaseHeldAsync(
            CreditLedgerReleaseHeldRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));
        }

        public Task<CreditLedgerWriteResult> MintPilotAtActivationAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(CreditLedgerWriteResult.Ok([]));
        }
    }
}
