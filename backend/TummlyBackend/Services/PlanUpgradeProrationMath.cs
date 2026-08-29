using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Prorated net + exclusive 20% VAT for same-cadence upgrade pay-now
    /// (ticket 20 / lock 05). Half-up AwayFromZero to whole pence.
    /// </summary>
    public static class PlanUpgradeProrationMath
    {
        public const decimal VatRate = 0.20m;

        public static (DateTime PeriodStartUtc, DateTime PeriodEndUtc) ResolvePeriod(
            DateTime renewalDateUtc,
            string? billingCycle
        )
        {
            var end =
                renewalDateUtc.Kind == DateTimeKind.Utc
                    ? renewalDateUtc
                    : DateTime.SpecifyKind(renewalDateUtc, DateTimeKind.Utc);

            var start = string.Equals(
                billingCycle,
                BillingCycles.Annual,
                StringComparison.Ordinal
            )
                ? end.AddMonths(-12)
                : end.AddMonths(-1);

            return (start, end);
        }

        public static int ProratedNetPence(
            int currentNetPence,
            int targetNetPence,
            decimal remainingRatio
        )
        {
            var delta = targetNetPence - currentNetPence;
            if (delta <= 0 || remainingRatio <= 0m)
            {
                return 0;
            }

            return (int)
                decimal.Round(
                    delta * remainingRatio,
                    MidpointRounding.AwayFromZero
                );
        }

        public static int VatOnNetPence(int netPence)
        {
            if (netPence <= 0)
            {
                return 0;
            }

            return (int)
                decimal.Round(
                    netPence * VatRate,
                    MidpointRounding.AwayFromZero
                );
        }

        public static int NetPenceForCadence(
            int monthlyNetPence,
            int annualNetPence,
            string? billingCycle
        )
        {
            return string.Equals(
                billingCycle,
                BillingCycles.Annual,
                StringComparison.Ordinal
            )
                ? annualNetPence
                : monthlyNetPence;
        }
    }
}
