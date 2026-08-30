using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface ICreditThresholdEvaluator
    {
        /// <summary>
        /// Updates the per-channel watermark inside the open ledger transaction.
        /// Returns notifications to deliver after commit.
        /// </summary>
        Task<CreditThresholdApplyResult> ApplyInTransactionAsync(
            int restaurantId,
            string channel,
            IReadOnlyList<CreditLedgerEntry> channelEntriesIncludingPending,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Delivers threshold notifications after a successful ledger commit.
        /// Notify failure does not undo the spend.
        /// </summary>
        Task NotifyAfterCommitAsync(
            CreditThresholdApplyResult applyResult,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Resets all channel watermarks when a new Included period mint lands.
        /// </summary>
        Task ResetBandsForIncludedPeriodMintAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class CreditThresholdApplyResult
    {
        public IReadOnlyList<CreditThresholdPendingNotification> Pending { get; init; } =
            [];
    }

    public sealed class CreditThresholdPendingNotification
    {
        public int RestaurantId { get; init; }

        public string Channel { get; init; } = string.Empty;

        public int Band { get; init; }

        public string PeriodKey { get; init; } = string.Empty;

        public string BillingStatus { get; init; } = string.Empty;

        public bool IsPilot { get; init; }

        public bool SuppressNotify { get; init; }

        public decimal UsedShare { get; init; }

        public int Remaining { get; init; }

        public int Used { get; init; }
    }
}
