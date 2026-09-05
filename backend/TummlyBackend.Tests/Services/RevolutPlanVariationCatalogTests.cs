using System.Text.Json;
using TummlyBackend.Billing.Revolut;
using TummlyBackend.Helpers;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class RevolutPlanVariationCatalogTests
    {
        [Fact]
        public void GrossMinorFromNetPence_AppliesHalfUpVat()
        {
            Assert.Equal(4680, TummlyVatMath.GrossMinorFromNetPence(3900));
            Assert.Equal(11880, TummlyVatMath.GrossMinorFromNetPence(9900));
            Assert.Equal(23880, TummlyVatMath.GrossMinorFromNetPence(19900));
            // 1001 * 1.2 = 1201.2 → half-up to 1201
            Assert.Equal(1201, TummlyVatMath.GrossMinorFromNetPence(1001));
        }

        [Fact]
        public void BuildFromPackJson_YieldsEightRecurring_WithGrossAmounts()
        {
            var json = File.ReadAllText(PackJsonPath());
            var rows = RevolutPlanVariationCatalog.BuildFromPackJson(json);

            Assert.Equal(8, rows.Count);
            Assert.Equal(
                RevolutPlanVariationKeys.All.OrderBy(k => k).ToArray(),
                rows.Select(r => r.LookupKey).OrderBy(k => k).ToArray()
            );

            var starterMonthly = rows.Single(r =>
                r.LookupKey == RevolutPlanVariationKeys.StarterMonthly
            );
            Assert.Equal(3900, starterMonthly.NetPence);
            Assert.Equal(4680, starterMonthly.GrossMinor);
            Assert.Equal("P1M", starterMonthly.CycleDuration);
            Assert.Equal("starter", starterMonthly.PlanKey);

            var growthAnnual = rows.Single(r =>
                r.LookupKey == RevolutPlanVariationKeys.GrowthAnnual
            );
            Assert.Equal(101000, growthAnnual.NetPence);
            Assert.Equal(121200, growthAnnual.GrossMinor);
            Assert.Equal("P1Y", growthAnnual.CycleDuration);
        }

        [Fact]
        public void BuildFromPackJson_ExcludesTopUpsAndPilot()
        {
            var json = File.ReadAllText(PackJsonPath());
            var rows = RevolutPlanVariationCatalog.BuildFromPackJson(json);

            Assert.DoesNotContain(
                rows,
                r => r.LookupKey.Contains("sms", StringComparison.Ordinal)
            );
            Assert.DoesNotContain(
                rows,
                r => r.PlanKey.Equals("pilot", StringComparison.Ordinal)
            );
        }

        [Fact]
        public void ToCreatePlanBodies_EightPlans_OneVariation_FriendlyDisplayName()
        {
            var json = File.ReadAllText(PackJsonPath());
            var rows = RevolutPlanVariationCatalog.BuildFromPackJson(json);
            var bodies = RevolutPlanVariationCatalog.ToCreatePlanBodies(rows);

            Assert.Equal(8, bodies.Count);
            Assert.All(
                bodies,
                body =>
                {
                    Assert.Single(body.Variations);
                    Assert.Equal(
                        body.Variations[0].LookupKey,
                        body.Variations[0].Label
                    );
                }
            );

            var starterMonthly = bodies.Single(b =>
                b.Name == "Paid Starter Plan Monthly"
            );
            Assert.Equal("starter", starterMonthly.PlanKey);
            Assert.Equal(
                RevolutPlanVariationKeys.StarterMonthly,
                starterMonthly.Variations[0].LookupKey
            );
            Assert.Equal(4680, starterMonthly.Variations[0].AmountGrossMinor);
            Assert.Equal("GBP", starterMonthly.Variations[0].Currency);

            var starterAnnual = bodies.Single(b =>
                b.Name == "Paid Starter Plan Annual"
            );
            Assert.Equal(
                RevolutPlanVariationKeys.StarterAnnual,
                starterAnnual.Variations[0].LookupKey
            );

            var createJson = RevolutPlanVariationCatalog.ToCreatePlanRequestJson(
                starterMonthly
            );
            using var doc = JsonDocument.Parse(createJson);
            Assert.Equal(
                "Paid Starter Plan Monthly",
                doc.RootElement.GetProperty("name").GetString()
            );
            var named = doc
                .RootElement.GetProperty("variations")
                .EnumerateArray()
                .Select(v => v.GetProperty("name").GetString())
                .ToArray();
            Assert.Equal(
                RevolutPlanVariationKeys.StarterMonthly,
                named[0]
            );
            Assert.Single(named);
        }

        [Fact]
        public void DisplayNameFor_StarterMonthly_IsPaidStarterPlanMonthly()
        {
            Assert.Equal(
                "Paid Starter Plan Monthly",
                RevolutPlanVariationCatalog.DisplayNameFor("starter", "monthly")
            );
            Assert.Equal(
                "Paid Growth Plan Annual",
                RevolutPlanVariationCatalog.DisplayNameFor("growth", "annual")
            );
        }

        [Fact]
        public void MapCreateResponse_BindsVariationIdsByCycleDuration()
        {
            var rows = RevolutPlanVariationCatalog.BuildFromPackJson(
                File.ReadAllText(PackJsonPath())
            );
            var starterRows = rows
                .Where(r => r.PlanKey == "starter")
                .ToList();
            using var doc = JsonDocument.Parse(
                """
                {
                  "id": "plan-starter",
                  "variations": [
                    {
                      "id": "var-monthly",
                      "phases": [{ "cycle_duration": "P1M", "amount": 4680 }]
                    },
                    {
                      "id": "var-annual",
                      "phases": [{ "cycle_duration": "P1Y", "amount": 47760 }]
                    }
                  ]
                }
                """
            );

            var map = RevolutPlanVariationCatalog.MapCreateResponse(
                starterRows,
                doc.RootElement
            );

            Assert.Equal(
                "var-monthly",
                map[RevolutPlanVariationKeys.StarterMonthly]
            );
            Assert.Equal(
                "var-annual",
                map[RevolutPlanVariationKeys.StarterAnnual]
            );
        }

        [Fact]
        public void FormatEnvMapLines_WritesEightPlanVariationKeys()
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [RevolutPlanVariationKeys.StarterMonthly] = "a",
                [RevolutPlanVariationKeys.StarterAnnual] = "b",
                [RevolutPlanVariationKeys.GrowthMonthly] = "c",
                [RevolutPlanVariationKeys.GrowthAnnual] = "d",
                [RevolutPlanVariationKeys.GroupMonthly] = "e",
                [RevolutPlanVariationKeys.GroupAnnual] = "f",
                [RevolutPlanVariationKeys.GroupLocationMonthly] = "g",
                [RevolutPlanVariationKeys.GroupLocationAnnual] = "h",
            };

            var lines = RevolutPlanVariationCatalog.FormatEnvMapLines(map);

            Assert.Equal(8, lines.Count);
            Assert.Contains(
                "Revolut__PlanVariations__tummly_starter_monthly_gbp_v3=a",
                lines
            );
            Assert.DoesNotContain(
                lines,
                line => line.Contains("PATCH", StringComparison.OrdinalIgnoreCase)
            );
        }

        private static string PackJsonPath()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(
                    dir.FullName,
                    "docs",
                    "product",
                    "billing-pack-v3.0",
                    "tummly_uk_billing_config_v3.0.json"
                );
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException(
                "billing-pack-v3.0 JSON not found."
            );
        }
    }
}
