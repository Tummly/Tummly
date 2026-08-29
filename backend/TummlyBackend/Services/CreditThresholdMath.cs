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
            int targetBand
        )
        {
            if (targetBand <= currentWatermark)
            {
                return [];
            }

            return CreditThresholdBands.Ordered
                .Where(band => band <= targetBand && band > currentWatermark)
                .ToList();
        }
    }
}
