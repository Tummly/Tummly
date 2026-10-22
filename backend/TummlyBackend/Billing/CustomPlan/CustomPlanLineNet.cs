namespace TummlyBackend.Billing.CustomPlan
{
    /// <summary>
    /// Shared lineNetPence maths (contract §1): monthly = ceil(qty × monthly / unitQty);
    /// annual = ceil(qty × annual / unitQty) when annual &gt; 0, else round(monthly × 12 × 0.85).
    /// </summary>
    public static class CustomPlanLineNet
    {
        public static int Monthly(int quantity, int monthlyNetPence, int unitQuantity)
        {
            if (quantity <= 0)
            {
                return 0;
            }

            var unitQty = Math.Max(1, unitQuantity);
            return (int)Math.Ceiling(quantity * (double)monthlyNetPence / unitQty);
        }

        public static int Annual(
            int quantity,
            int monthlyNetPence,
            int? annualNetPence,
            int unitQuantity
        )
        {
            if (quantity <= 0)
            {
                return 0;
            }

            var unitQty = Math.Max(1, unitQuantity);
            if (annualNetPence is > 0)
            {
                return (int)Math.Ceiling(
                    quantity * (double)annualNetPence.Value / unitQty
                );
            }

            return (int)Math.Round(
                Monthly(quantity, monthlyNetPence, unitQuantity)
                    * 12
                    * CustomPlanPacks.AnnualFallbackFactor
            );
        }

        public static int ForCadence(
            string cadence,
            int quantity,
            int monthlyNetPence,
            int? annualNetPence,
            int unitQuantity
        )
        {
            return string.Equals(cadence, "annual", StringComparison.OrdinalIgnoreCase)
                ? Annual(quantity, monthlyNetPence, annualNetPence, unitQuantity)
                : Monthly(quantity, monthlyNetPence, unitQuantity);
        }
    }
}
