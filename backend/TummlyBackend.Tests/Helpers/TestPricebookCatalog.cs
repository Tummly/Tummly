using TummlyBackend.Billing.Pricebook;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Helpers
{
    internal static class TestPricebookPaths
    {
        public static string PackDirectory()
        {
            var dir = Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "..",
                    "docs",
                    "product",
                    "billing-pack-v3.0"
                )
            );
            if (!Directory.Exists(dir))
            {
                dir = Path.GetFullPath(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "..",
                        "..",
                        "..",
                        "..",
                        "docs",
                        "product",
                        "billing-pack-v3.0"
                    )
                );
            }

            if (!Directory.Exists(dir))
            {
                throw new InvalidOperationException(
                    $"Pack directory missing: {dir}"
                );
            }

            return dir;
        }

        public static IPricebookCatalog LoadV3()
        {
            return PricebookCatalog.LoadFromDirectory(PackDirectory());
        }
    }

    internal sealed class StubPricebookCatalog : IPricebookCatalog
    {
        private readonly PricebookChannelCredits _oneTime;
        private readonly string _pricebookId;

        public StubPricebookCatalog(
            PricebookChannelCredits oneTime,
            string pricebookId = "TEST-PRICEBOOK"
        )
        {
            _oneTime = oneTime;
            _pricebookId = pricebookId;
        }

        public string CurrentPricebookId => _pricebookId;

        public PricebookSnapshot GetRequired(string pricebookId)
        {
            return new PricebookSnapshot
            {
                Id = pricebookId,
                Plans = new Dictionary<string, PricebookPlan>
                {
                    ["pilot"] = new PricebookPlan
                    {
                        Key = "pilot",
                        DisplayName = "Pilot",
                        CreditsOneTime = _oneTime,
                    },
                },
                TopUpPacks = [],
                VatRateBps = 2000,
            };
        }

        public BillingCurrentCatalogDto BuildCurrentCatalog(bool sms5000Available)
        {
            throw new NotSupportedException();
        }

        public string FormatPlanPriceNet(PricebookPlan plan, string? billingCycle)
        {
            throw new NotSupportedException();
        }

        public string FormatIncludedCreditsLabel(PricebookPlan plan, string channel)
        {
            throw new NotSupportedException();
        }
    }
}
