using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TummlyBackend.DTOs.Admin;

namespace TummlyBackend.Billing.CustomPlan
{
    /// <summary>
    /// Fingerprint of a Custom Plan quote. Signup compares it with the quote the guest saw,
    /// so a change to any line price, quantity, VAT, total, recurring or due-today amount,
    /// or start timing is detected before the subscription is saved.
    /// </summary>
    public static class CustomPlanQuoteHash
    {
        public static string Compute(CustomPlanQuoteResult quote)
        {
            var packs = string.Join(
                ";",
                quote.Packs
                    .Where(kv => kv.Value > 0)
                    .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                    .Select(kv =>
                        $"{kv.Key}={kv.Value.ToString(CultureInfo.InvariantCulture)}"
                    )
            );
            // Every line the customer sees: key, quantity, unit price and line total.
            var lines = string.Join(
                ";",
                quote.Lines.Select(line =>
                    string.Join(
                        "=",
                        line.Key,
                        line.Quantity.ToString(CultureInfo.InvariantCulture),
                        line.UnitNetPence.ToString(CultureInfo.InvariantCulture),
                        line.NetPence.ToString(CultureInfo.InvariantCulture)
                    )
                )
            );
            var canonical = string.Join(
                "|",
                quote.CatalogPublicId,
                quote.Cadence,
                quote.BasePlan,
                quote.Locations.ToString(CultureInfo.InvariantCulture),
                packs,
                lines,
                quote.PeriodNetPence.ToString(CultureInfo.InvariantCulture),
                quote.VatRateBps.ToString(CultureInfo.InvariantCulture),
                quote.VatAmountPence.ToString(CultureInfo.InvariantCulture),
                quote.GrossTotalPence.ToString(CultureInfo.InvariantCulture),
                quote.RecurringGrossPence.ToString(CultureInfo.InvariantCulture),
                quote.DueNowGrossPence.ToString(CultureInfo.InvariantCulture),
                quote.EffectiveTiming
            );

            var digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
            return Convert.ToHexString(digest, 0, 16).ToLowerInvariant();
        }
    }
}
