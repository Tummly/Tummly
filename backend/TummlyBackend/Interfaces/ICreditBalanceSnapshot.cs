namespace TummlyBackend.Interfaces
{
    public interface ICreditBalanceSnapshot
    {
        Task<CreditBalanceAccountSnapshot?> GetAccountAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class CreditBalanceAccountSnapshot
    {
        public bool IsPilot { get; init; }

        public string StarterKitState { get; init; } = string.Empty;

        public string PeriodLabel { get; init; } = string.Empty;

        public IReadOnlyList<CreditBalanceChannelSnapshot> Channels { get; init; }
            = [];
    }

    public sealed class CreditBalanceChannelSnapshot
    {
        public string Channel { get; init; } = string.Empty;

        public int Remaining { get; init; }

        public int Held { get; init; }

        public int UsedThisCycle { get; init; }

        public int IncludedThisPeriod { get; init; }

        public int PurchasedRemaining { get; init; }

        public DateTime? EarliestPurchasedExpiryUtc { get; init; }

        public decimal UsedShare { get; init; }
    }
}
