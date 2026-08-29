using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    internal static class CreditThresholdMath
    {
        public static decimal UsedShare(
            int usedThisCycle,
            int remaining
        )
        {
            if (remaining <= 0)
            {
                return 1m;
            }

            return (decimal)usedThisCycle / (usedThisCycle + remaining);
        }

        public static int TargetBand(decimal usedShare, int remaining)
        {
            if (remaining <= 0)
            {
                return CreditThresholdBands.Band100;
            }

            if (usedShare >= 0.9m)
            {
                return CreditThresholdBands.Band90;
            }

            if (usedShare >= 0.8m)
            {
                return CreditThresholdBands.Band80;
            }

            return CreditThresholdBands.None;
        }

        public static IReadOnlyList<int> BandsToEmit(
            int currentWatermark,
            int targetBand,
            int remaining,
            int usedThisCycle
        )
        {
            if (targetBand <= currentWatermark)
            {
                return [];
            }

            // Lock 09: a hold that zeros remaining makes used share 100% while
            // used stays 0 — emit 100 only; 80/90 wait for settle.
            if (remaining <= 0 && usedThisCycle <= 0)
            {
                return currentWatermark < CreditThresholdBands.Band100
                    ? [CreditThresholdBands.Band100]
                    : [];
            }

            return CreditThresholdBands.Ordered
                .Where(band => band <= targetBand && band > currentWatermark)
                .ToList();
        }
    }
}
