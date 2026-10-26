using TummlyBackend.Models;

namespace TummlyBackend.Billing.CustomPlan
{
    /// <summary>
    /// One progressive Location price band (doc 08 N10.25). Slots 1–5 are in the Group base.
    /// </summary>
    public sealed record LocationBand(int Index, int FirstSlot, int LastSlot, string RateKey);

    /// <summary>
    /// The part of a slot range that falls in one band.
    /// </summary>
    public sealed record LocationBandSlice(int BandIndex, int SlotCount, int FirstSlot, int LastSlot);

    /// <summary>
    /// Net rate for one band, per Location (unit quantity 1).
    /// </summary>
    public sealed record LocationBandRate(int Index, int MonthlyNetPence, int? AnnualNetPence);

    /// <summary>
    /// Progressive per-Location pricing for Custom Plan quotes. Band limits are code constants.
    /// Rates are PROVISIONAL until the Director / Mohamed table is signed; the rates are also
    /// seeded as catalogue data, so a later change is data only.
    /// </summary>
    public static class LocationBandPricing
    {
        public const int FirstPricedSlot = 6;

        public static readonly IReadOnlyList<LocationBand> Bands =
        [
            new LocationBand(1, 6, 30, CommercialCustomUnitRateKeys.Location),
            new LocationBand(2, 31, 50, CommercialCustomUnitRateKeys.ExtraLocationBand2),
            new LocationBand(3, 51, 100, CommercialCustomUnitRateKeys.ExtraLocationBand3),
            new LocationBand(4, 101, 250, CommercialCustomUnitRateKeys.ExtraLocationBand4),
            new LocationBand(5, 251, 500, CommercialCustomUnitRateKeys.ExtraLocationBand5),
        ];

        /// <summary>
        /// Provisional rates for bands 2–5 (band 1 is the existing <c>location</c> unit rate).
        /// </summary>
        public static readonly IReadOnlyDictionary<int, LocationBandRate> ProvisionalRates =
            new Dictionary<int, LocationBandRate>
            {
                [2] = new LocationBandRate(2, 3700, 37740),
                [3] = new LocationBandRate(3, 3500, 35700),
                [4] = new LocationBandRate(4, 3300, 33660),
                [5] = new LocationBandRate(5, 3100, 31620),
            };

        /// <summary>
        /// Splits an inclusive slot range (Location numbers) into per-band slices.
        /// Slots below <see cref="FirstPricedSlot"/> are ignored. Empty when <paramref name="lastSlot"/> is before the first priced slot.
        /// </summary>
        public static IReadOnlyList<LocationBandSlice> Slice(int firstSlot, int lastSlot)
        {
            var from = Math.Max(firstSlot, FirstPricedSlot);
            if (lastSlot < from)
            {
                return [];
            }

            // Throws for a slot above the quote maximum, so an over-range request is never under-priced.
            BandIndexForSlot(lastSlot);

            var slices = new List<LocationBandSlice>();
            foreach (var band in Bands)
            {
                var start = Math.Max(from, band.FirstSlot);
                var end = Math.Min(lastSlot, band.LastSlot);
                if (start > end)
                {
                    continue;
                }

                slices.Add(new LocationBandSlice(band.Index, end - start + 1, start, end));
            }

            return slices;
        }

        /// <summary>
        /// Sums the net price of a slot range. Returns false with the first band that has no rate.
        /// </summary>
        public static bool TryPrice(
            int firstSlot,
            int lastSlot,
            string cadence,
            IReadOnlyDictionary<int, LocationBandRate> ratesByBand,
            out int netPence,
            out int? missingBand
        )
        {
            netPence = 0;
            missingBand = null;

            foreach (var slice in Slice(firstSlot, lastSlot))
            {
                if (!ratesByBand.TryGetValue(slice.BandIndex, out var rate))
                {
                    missingBand = slice.BandIndex;
                    return false;
                }

                netPence += CustomPlanLineNet.ForCadence(
                    cadence,
                    slice.SlotCount,
                    rate.MonthlyNetPence,
                    rate.AnnualNetPence,
                    1
                );
            }

            return true;
        }

        /// <summary>
        /// Band index (1–5) for one Location slot (6–500).
        /// </summary>
        public static int BandIndexForSlot(int slot)
        {
            if (slot < FirstPricedSlot || slot > CustomPlanConfig.MaxLocations)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }

            foreach (var band in Bands)
            {
                if (slot >= band.FirstSlot && slot <= band.LastSlot)
                {
                    return band.Index;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(slot));
        }
    }
}
