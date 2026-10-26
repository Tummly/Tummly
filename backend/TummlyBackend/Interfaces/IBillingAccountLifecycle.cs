using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IBillingAccountLifecycle
    {
        Task TickAsync(
            int restaurantId,
            DateTime now,
            CancellationToken cancellationToken = default
        );

        Task<BillingLifecycleCommandResult> StartDunningEpisodeAsync(
            int restaurantId,
            DateTime now,
            string? outstandingOrderId = null,
            CancellationToken cancellationToken = default
        );

        Task RecoverDunningAsync(
            int restaurantId,
            DateTime now,
            CancellationToken cancellationToken = default
        );

        Task ActivatePaidPlanAsync(
            int restaurantId,
            DateTime now,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Admin Pilot extension. Locks the BillingAccount row, applies the
        /// bounded rules (14 or 30 days, max two, 90 days total), and writes the
        /// Pilot clock for the owner and members. Same non-null
        /// <paramref name="idempotencyKey"/> as the last extension replays.
        /// </summary>
        Task<PilotExtensionResult> ExtendPilotAsync(
            int restaurantId,
            int days,
            string? idempotencyKey,
            DateTime now,
            CancellationToken cancellationToken = default
        );

        Task SetChargebackRestrictionAsync(
            int restaurantId,
            bool restricted,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Mutates the locked BillingAccount in the caller transaction after paid
        /// Cancel plan applies at renewal end. Does not open a nested transaction.
        /// </summary>
        BillingLifecycleCommandResult ApplyPostCancelSoftLock(
            BillingAccount billingAccount,
            DateTime renewalEndUtc
        );
    }

    /// <summary>
    /// Outcome of <see cref="IBillingAccountLifecycle.ExtendPilotAsync"/>.
    /// <c>Outcome</c> is <c>extended</c>, <c>replayed</c> or <c>refused</c>.
    /// <c>Code</c> is set only when refused.
    /// </summary>
    public sealed record PilotExtensionResult(
        string Outcome,
        string? Code,
        DateTime? OldEndUtc,
        DateTime? NewEndUtc,
        int CountAfter,
        int DaysTotalAfter
    );

    public sealed record BillingLifecycleCommandResult(
        bool Applied,
        bool Refused,
        string? Reason
    )
    {
        public static BillingLifecycleCommandResult Ok() =>
            new(true, false, null);

        public static BillingLifecycleCommandResult NoOp() =>
            new(false, false, null);

        public static BillingLifecycleCommandResult Refuse(string reason) =>
            new(false, true, reason);
    }
}
