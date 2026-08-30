using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Services
{
    internal sealed class FixedCreditBalanceSnapshot : ICreditBalanceSnapshot
    {
        private readonly int _remaining;

        public FixedCreditBalanceSnapshot(int remaining)
        {
            _remaining = remaining;
        }

        public Task<CreditBalanceAccountSnapshot?> GetAccountAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<CreditBalanceAccountSnapshot?>(
                new CreditBalanceAccountSnapshot
                {
                    Channels = CreditChannels.All
                        .Select(channel => new CreditBalanceChannelSnapshot
                        {
                            Channel = channel,
                            Remaining = _remaining,
                            Held = 0,
                            UsedThisCycle = 0,
                            IncludedThisPeriod = _remaining,
                            PurchasedRemaining = 0,
                            UsedShare = _remaining <= 0 ? 1m : 0m,
                        })
                        .ToList(),
                }
            );
        }
    }
}
