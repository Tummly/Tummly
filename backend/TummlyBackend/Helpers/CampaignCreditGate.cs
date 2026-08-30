using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class CampaignCreditGate
    {
        public sealed record Refusal(
            string Code,
            string Channel,
            int Remaining,
            int Requested
        );

        public static Refusal? EvaluateNewReserve(
            CreditBalanceAccountSnapshot? snapshot,
            string channel,
            int requestedUnits
        )
        {
            if (requestedUnits <= 0)
            {
                return null;
            }

            var normalized = channel.Trim().ToLowerInvariant();
            var channelSnapshot = snapshot?.Channels.FirstOrDefault(row =>
                row.Channel == normalized
            );
            var remaining = channelSnapshot?.Remaining ?? 0;

            if (remaining <= 0)
            {
                return new Refusal(
                    "channel_hard_stopped",
                    normalized,
                    remaining,
                    requestedUnits
                );
            }

            if (remaining < requestedUnits)
            {
                return new Refusal(
                    "insufficient_credits",
                    normalized,
                    remaining,
                    requestedUnits
                );
            }

            return null;
        }
    }
}
