using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Plan allowance for active QR codes as one Workspace-wide pool (G1 / V4).
    /// </summary>
    public sealed record ActiveQrAllowance(int WorkspacePool);

    /// <summary>
    /// Result of <see cref="IActiveQrCapacity.EvaluateAsync"/>. When
    /// <paramref name="EntitlementsAvailable"/> is false the plan could not be
    /// resolved and <paramref name="Fits"/> is false. <paramref name="Used"/> is
    /// the Workspace count (all Locations).
    /// </summary>
    public sealed record ActiveQrCapacityDecision(
        bool EntitlementsAvailable,
        bool Fits,
        int WorkspacePool,
        int Used
    );

    /// <summary>
    /// Shared active-QR capacity rule for Capture and Shop checkout. Callers
    /// must hold an open transaction for the check and the write that follows it.
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
        /// Workspace pool for the account (Commercial V4, 25 Oct 2026), or null
        /// when entitlements are unavailable.
        /// </summary>
        Task<ActiveQrAllowance?> GetAllowanceAsync(
            BillingAccount? account,
            CancellationToken ct
        );

        /// <summary>
        /// Slots used across every Location of the Workspace: Active codes plus
        /// each distinct held reservation QR type (per Location) that has no
        /// Active code of that type.
        /// </summary>
        Task<int> CountUsedForRestaurantAsync(int restaurantId, CancellationToken ct);

        /// <summary>
        /// Slots that activating one Paused code of <paramref name="qrType"/> at
        /// the location takes: 0 when it takes over a held reservation of that
        /// type with no Active code of that type, otherwise 1.
        /// </summary>
        Task<int> ActivationCostAsync(
            int locationId,
            QrType qrType,
            CancellationToken ct
        );

        /// <summary>
        /// Checks whether <paramref name="increment"/> more slots fit under the
        /// Workspace pool. Calls <see cref="LockBillingAccountAsync"/> itself.
        /// An increment of zero or less always fits and reads nothing. Pass
        /// <paramref name="activatingType"/> when the increment activates one
        /// existing Paused code of that type, so a held slot of that type is not
        /// counted twice.
        /// </summary>
        Task<ActiveQrCapacityDecision> EvaluateAsync(
            int restaurantId,
            int locationId,
            int increment,
            QrType? activatingType = null,
            CancellationToken ct = default
        );
    }
}
