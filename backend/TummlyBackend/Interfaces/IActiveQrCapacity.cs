using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Plan allowance for active QR codes. <paramref name="AccountExtra"/> is
    /// the account-wide pool that absorbs per-location overflow.
    /// </summary>
    public sealed record ActiveQrAllowance(int PerLocationCap, int AccountExtra);

    /// <summary>
    /// Result of <see cref="IActiveQrCapacity.EvaluateAsync"/>. When
    /// <paramref name="EntitlementsAvailable"/> is false the plan could not be
    /// resolved and <paramref name="Fits"/> is false.
    /// </summary>
    public sealed record ActiveQrCapacityDecision(
        bool EntitlementsAvailable,
        bool Fits,
        int PerLocationCap,
        int Used
    );

    /// <summary>
    /// Shared active-QR capacity rule for Capture and Shop checkout (spec
    /// 4.4.2). Callers must hold an open transaction for the check and the
    /// write that follows it.
    /// </summary>
    public interface IActiveQrCapacity
    {
        /// <summary>
        /// Takes the <c>BillingAccounts</c> row lock (UPDLOCK on SQL Server, plain
        /// read otherwise). The caller owns the transaction.
        /// </summary>
        Task<BillingAccount?> LockBillingAccountAsync(
            int restaurantId,
            CancellationToken ct
        );

        /// <summary>
        /// Plan allowance for the account, or null when entitlements are unavailable.
        /// </summary>
        Task<ActiveQrAllowance?> GetAllowanceAsync(
            BillingAccount? account,
            int locationId,
            CancellationToken ct
        );

        /// <summary>
        /// Slots used at the location: Active codes plus distinct held
        /// reservation QR types with no Active code of that type.
        /// </summary>
        Task<int> CountUsedAsync(int locationId, CancellationToken ct);

        /// <summary>
        /// Checks whether <paramref name="increment"/> more slots fit under the
        /// per-location cap or the account-extra pool. Calls
        /// <see cref="LockBillingAccountAsync"/> itself. An increment of zero
        /// or less always fits and reads nothing.
        /// </summary>
        Task<ActiveQrCapacityDecision> EvaluateAsync(
            int restaurantId,
            int locationId,
            int increment,
            CancellationToken ct
        );
    }
}
