namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Tummly-owned UK VAT math (lock 05): exclusive, half-up to penny.
    /// </summary>
    public static class TummlyVatMath
    {
        public const int DefaultVatRateBps = 2000;

        /// <summary>
        /// Net minor units (pence) → VAT minor units (half-up AwayFromZero).
        /// </summary>
        public static int VatPenceFromNetPence(
            int netPence,
            int vatRateBps = DefaultVatRateBps
        )
        {
            if (netPence < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(netPence));
            }

            if (vatRateBps < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(vatRateBps));
            }

            var rate = vatRateBps / 10_000m;
            return (int)
                decimal.Round(
                    netPence * rate,
                    0,
                    MidpointRounding.AwayFromZero
                );
        }

        /// <summary>
        /// Net minor units (pence) → gross minor units for Revolut plan amounts.
        /// </summary>
        public static int GrossMinorFromNetPence(
            int netPence,
            int vatRateBps = DefaultVatRateBps
        )
        {
            if (netPence < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(netPence));
            }

            if (vatRateBps < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(vatRateBps));
            }

            return netPence + VatPenceFromNetPence(netPence, vatRateBps);
        }
    }
}
