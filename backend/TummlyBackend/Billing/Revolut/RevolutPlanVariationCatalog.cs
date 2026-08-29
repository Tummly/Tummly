using System.Text.Json;
using TummlyBackend.Helpers;
using TummlyBackend.Services;

namespace TummlyBackend.Billing.Revolut
{
    /// <summary>
    /// Builds Revolut subscription-plan create payloads from the pack pricebook
    /// (ticket 13 / lock 06). Create only — never PATCH a live variation amount.
    /// </summary>
    public static class RevolutPlanVariationCatalog
    {
        private static readonly string[] RecurringPlanKeys =
        [
            "starter",
            "growth",
            "group",
            "additional_group_location",
        ];

        public static IReadOnlyList<RevolutPlanVariationRow> BuildFromPackJson(
            string packJson
        )
        {
            using var doc = JsonDocument.Parse(packJson);
            var root = doc.RootElement;
            var vatRate = root.GetProperty("vat").GetProperty("rate").GetDecimal();
            var vatRateBps = (int)decimal.Round(vatRate * 10_000m);
            var plans = root.GetProperty("plans");
            var rows = new List<RevolutPlanVariationRow>(8);

            foreach (var planKey in RecurringPlanKeys)
            {
                if (!plans.TryGetProperty(planKey, out var plan))
                {
                    throw new InvalidOperationException(
                        $"Pack plans.{planKey} is missing."
                    );
                }

                var lookupKeys = plan.GetProperty("lookup_keys");
                var monthlyKey =
                    lookupKeys.GetProperty("monthly").GetString()
                    ?? throw new InvalidOperationException(
                        $"Pack plans.{planKey}.lookup_keys.monthly is missing."
                    );
                var annualKey =
                    lookupKeys.GetProperty("annual").GetString()
                    ?? throw new InvalidOperationException(
                        $"Pack plans.{planKey}.lookup_keys.annual is missing."
                    );
                var monthlyNet = plan.GetProperty("monthly_price_pence").GetInt32();
                var annualNet = plan.GetProperty("annual_price_pence").GetInt32();

                rows.Add(
                    new RevolutPlanVariationRow(
                        PlanKey: planKey,
                        LookupKey: monthlyKey,
                        Cadence: "monthly",
                        CycleDuration: "P1M",
                        NetPence: monthlyNet,
                        GrossMinor: TummlyVatMath.GrossMinorFromNetPence(
                            monthlyNet,
                            vatRateBps
                        ),
                        Currency: "GBP"
                    )
                );
                rows.Add(
                    new RevolutPlanVariationRow(
                        PlanKey: planKey,
                        LookupKey: annualKey,
                        Cadence: "annual",
                        CycleDuration: "P1Y",
                        NetPence: annualNet,
                        GrossMinor: TummlyVatMath.GrossMinorFromNetPence(
                            annualNet,
                            vatRateBps
                        ),
                        Currency: "GBP"
                    )
                );
            }

            ValidateAgainstKnownKeys(rows);
            return rows;
        }

        public static IReadOnlyList<RevolutCreatePlanBody> ToCreatePlanBodies(
            IReadOnlyList<RevolutPlanVariationRow> rows
        )
        {
            return rows
                .GroupBy(r => r.PlanKey, StringComparer.Ordinal)
                .OrderBy(g => Array.IndexOf(RecurringPlanKeys, g.Key))
                .Select(g =>
                {
                    var variations = g.OrderBy(r =>
                            r.Cadence == "monthly" ? 0 : 1
                        )
                        .Select(r => new RevolutCreatePlanVariation(
                            Label: r.LookupKey,
                            LookupKey: r.LookupKey,
                            CycleDuration: r.CycleDuration,
                            AmountGrossMinor: r.GrossMinor,
                            Currency: r.Currency
                        ))
                        .ToList();
                    return new RevolutCreatePlanBody(g.Key, variations);
                })
                .ToList();
        }

        /// <summary>
        /// Bind create-response variation ids to pack lookup keys by cycle duration.
        /// </summary>
        public static IReadOnlyDictionary<string, string> MapCreateResponse(
            IReadOnlyList<RevolutPlanVariationRow> expectedRows,
            JsonElement planResponse
        )
        {
            var byCycle = expectedRows.ToDictionary(
                r => r.CycleDuration,
                r => r.LookupKey,
                StringComparer.Ordinal
            );
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (
                !planResponse.TryGetProperty("variations", out var variations)
            )
            {
                throw new InvalidOperationException(
                    "Create response missing variations."
                );
            }

            foreach (var variation in variations.EnumerateArray())
            {
                var id =
                    variation.GetProperty("id").GetString()
                    ?? throw new InvalidOperationException(
                        "Variation id missing."
                    );
                var phases = variation.GetProperty("phases");
                var first = phases.EnumerateArray().First();
                var cycle =
                    first.GetProperty("cycle_duration").GetString()
                    ?? throw new InvalidOperationException(
                        "cycle_duration missing."
                    );
                if (!byCycle.TryGetValue(cycle, out var lookupKey))
                {
                    throw new InvalidOperationException(
                        $"Unexpected cycle_duration '{cycle}'."
                    );
                }

                map[lookupKey] = id;
            }

            return map;
        }

        public static IReadOnlyList<string> FormatEnvMapLines(
            IReadOnlyDictionary<string, string> lookupToVariationId
        )
        {
            var lines = new List<string>(RevolutPlanVariationKeys.All.Count);
            foreach (var key in RevolutPlanVariationKeys.All)
            {
                if (
                    !lookupToVariationId.TryGetValue(key, out var id)
                    || string.IsNullOrWhiteSpace(id)
                )
                {
                    throw new InvalidOperationException(
                        $"Map missing variation id for '{key}'."
                    );
                }

                lines.Add($"Revolut__PlanVariations__{key}={id.Trim()}");
            }

            return lines;
        }

        /// <summary>
        /// JSON body for <c>POST /api/subscription-plans</c>. Variation label is
        /// the pack lookup key (lock 06); amount is Tummly gross minor units.
        /// </summary>
        public static string ToCreatePlanRequestJson(RevolutCreatePlanBody body)
        {
            // Merchant API Variation-Creation has no name field. Product "label"
            // is the pack lookup_key — carried in our map keys and dry-run output,
            // not as a PATCH/name field on a live variation.
            var variations = body.Variations.Select(v => new Dictionary<
                string,
                object?
            >
            {
                ["phases"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["ordinal"] = 1,
                        ["cycle_duration"] = v.CycleDuration,
                        ["amount"] = v.AmountGrossMinor,
                        ["currency"] = v.Currency,
                    },
                },
            });

            return JsonSerializer.Serialize(
                new Dictionary<string, object?>
                {
                    ["name"] = body.Name,
                    ["variations"] = variations,
                },
                new JsonSerializerOptions { WriteIndented = true }
            );
        }

        private static void ValidateAgainstKnownKeys(
            IReadOnlyList<RevolutPlanVariationRow> rows
        )
        {
            var keys = rows.Select(r => r.LookupKey).OrderBy(k => k).ToArray();
            var expected = RevolutPlanVariationKeys.All.OrderBy(k => k).ToArray();
            if (!keys.SequenceEqual(expected, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pack recurring lookup keys do not match RevolutPlanVariationKeys.All."
                );
            }
        }
    }

    public sealed record RevolutPlanVariationRow(
        string PlanKey,
        string LookupKey,
        string Cadence,
        string CycleDuration,
        int NetPence,
        int GrossMinor,
        string Currency
    );

    public sealed record RevolutCreatePlanBody(
        string Name,
        IReadOnlyList<RevolutCreatePlanVariation> Variations
    );

    public sealed record RevolutCreatePlanVariation(
        string Label,
        string LookupKey,
        string CycleDuration,
        int AmountGrossMinor,
        string Currency
    );
}
