using TummlyBackend.Billing.Pricebook;

namespace TummlyBackend.Tests.Services
{
    public class PricebookCatalogTests
    {
        private static string PackDirectory()
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

            Assert.True(
                Directory.Exists(dir),
                $"Pack directory missing: {dir}"
            );
            return dir;
        }

        [Fact]
        public void Load_BindsV3Keys_AndCurrentId()
        {
            var catalog = PricebookCatalog.LoadFromDirectory(PackDirectory());

            Assert.Equal("TUMMLY-UK-GBP-2026-08-V3", catalog.CurrentPricebookId);

            var book = catalog.GetRequired(catalog.CurrentPricebookId);
            Assert.Equal("TUMMLY-UK-GBP-2026-08-V3", book.Id);
            Assert.Equal(3900, book.Plans["starter"].MonthlyNetPence);
            Assert.Equal(9900, book.Plans["growth"].MonthlyNetPence);
            Assert.Equal(19900, book.Plans["group"].MonthlyNetPence);
            Assert.Equal(500, book.Plans["pilot"].CreditsOneTime!.Email);
            Assert.Equal(2, book.Plans["pilot"].IncludedTeamMembers);
            Assert.Equal(3, book.Plans["starter"].IncludedTeamMembers);
            Assert.Equal(10, book.Plans["growth"].IncludedTeamMembers);
            Assert.Equal(25, book.Plans["group"].IncludedTeamMembers);
            Assert.Equal(2500, book.Plans["starter"].CreditsMonthly!.Email);
            Assert.Equal(5, book.Plans["pilot"].ActiveQrPlacementsPerLocation);
            Assert.Equal(10, book.Plans["starter"].ActiveQrPlacementsPerLocation);
            Assert.Equal(25, book.Plans["growth"].ActiveQrPlacementsPerLocation);
            Assert.Equal(50, book.Plans["group"].ActiveQrPlacementsPerLocation);
            Assert.Equal(2000, book.VatRateBps);
            Assert.Contains(
                book.TopUpPacks,
                pack =>
                    pack.Channel == "sms"
                    && pack.Quantity == 5000
                    && pack.LookupKey == "tummly_sms_5000_gbp_v3"
            );
            Assert.Contains(
                book.TopUpPacks,
                pack =>
                    pack.Channel == "ai"
                    && pack.Quantity == 500
                    && pack.LookupKey == "tummly_ai_500_gbp_v3"
            );
        }

        [Fact]
        public void Load_DoesNotExposeInternalCostsOrStripeBlock()
        {
            var catalog = PricebookCatalog.LoadFromDirectory(PackDirectory());
            var book = catalog.GetRequired(catalog.CurrentPricebookId);

            Assert.Null(book.InternalCostAssumptions);
            Assert.Null(book.PaymentProvider);
        }

        [Fact]
        public void Load_FailsClosed_WhenCurrentIdMissing()
        {
            var temp = Directory.CreateTempSubdirectory("pricebook-missing-id");
            try
            {
                File.Copy(
                    Path.Combine(PackDirectory(), "tummly_uk_billing_config_v3.0.json"),
                    Path.Combine(temp.FullName, "tummly_uk_billing_config_v3.0.json")
                );

                var ex = Assert.Throws<InvalidOperationException>(() =>
                    PricebookCatalog.LoadFromDirectory(temp.FullName)
                );
                Assert.Contains("current-pricebook-id", ex.Message);
            }
            finally
            {
                temp.Delete(true);
            }
        }

        [Fact]
        public void Load_FailsClosed_WhenJsonMissing()
        {
            var temp = Directory.CreateTempSubdirectory("pricebook-missing-json");
            try
            {
                File.WriteAllText(
                    Path.Combine(temp.FullName, "current-pricebook-id"),
                    "TUMMLY-UK-GBP-2026-08-V3\n"
                );

                var ex = Assert.Throws<InvalidOperationException>(() =>
                    PricebookCatalog.LoadFromDirectory(temp.FullName)
                );
                Assert.Contains("tummly_uk_billing_config", ex.Message);
            }
            finally
            {
                temp.Delete(true);
            }
        }

        [Fact]
        public void GetRequired_FailsClosed_ForUnknownId()
        {
            var catalog = PricebookCatalog.LoadFromDirectory(PackDirectory());
            var ex = Assert.Throws<InvalidOperationException>(() =>
                catalog.GetRequired("NOT-A-BOOK")
            );
            Assert.Contains("NOT-A-BOOK", ex.Message);
        }
    }
}
