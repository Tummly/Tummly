using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TummlyBackend.DTOs.Admin;

namespace TummlyBackend.Billing.CustomPlan
{
    /// <summary>
    /// Fingerprint of a Custom Plan quote. Signup compares it with the quote the guest saw,
    /// so a price, count or VAT rate change is detected before the subscription is saved.
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
            var canonical = string.Join(
                "|",
                quote.CatalogPublicId,
                quote.Cadence,
                quote.BasePlan,
                quote.Locations.ToString(CultureInfo.InvariantCulture),
                packs,
                quote.PeriodNetPence.ToString(CultureInfo.InvariantCulture),
                quote.VatRateBps.ToString(CultureInfo.InvariantCulture)
            );

            var digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
            return Convert.ToHexString(digest, 0, 16).ToLowerInvariant();
        }
    }
}
