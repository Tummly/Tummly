using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class TummlyVatInvoiceServiceTests
    {
        private readonly IPricebookCatalog _pricebook = TestPricebookPaths.LoadV3();
        private readonly DateTime _now = new(2026, 3, 10, 15, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Mint_IsIdempotent_PerRevolutOrderId()
        {
            await using var context = CreateContext();
            var restaurantId = await SeedRestaurantAsync(context);
            var service = CreateService(context);

            var first = await service.MintForCompletedOrderAsync(
                new TummlyVatInvoiceMintRequest(
                    RevolutOrderId: "ord_unique",
                    RevolutSubscriptionId: "sub_unique",
                    RestaurantId: restaurantId,
                    Plan: BillingSubscriptionPlans.Starter,
                    BillingCycle: BillingCycles.Monthly,
                    PaymentSuccessUtc: _now
                )
            );
            var second = await service.MintForCompletedOrderAsync(
                new TummlyVatInvoiceMintRequest(
                    RevolutOrderId: "ord_unique",
                    RevolutSubscriptionId: "sub_unique",
                    RestaurantId: restaurantId,
                    Plan: BillingSubscriptionPlans.Starter,
                    BillingCycle: BillingCycles.Monthly,
                    PaymentSuccessUtc: _now
                )
            );

            Assert.Equal(first.DocumentNumber, second.DocumentNumber);
            Assert.Equal(1, await context.TummlyVatInvoices.CountAsync());
            Assert.Equal("TM-2026-000001", first.DocumentNumber);
            Assert.Equal(3900, first.NetPence);
            Assert.Equal(780, first.VatPence);
            Assert.Equal(4680, first.GrossPence);
            Assert.Equal("Starter plan (Monthly)", first.LineDescription);
            Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(
                TummlyVatInvoicePdfWriter.Render(first)
            ));
        }

        [Fact]
        public void Render_TaxInvoice_ContainsLayoutMarkersAndSignOffFields()
        {
            var invoice = new TummlyVatInvoice
            {
                DocumentNumber = "TM-2026-000001",
                DocumentPrefix = TummlyVatInvoice.PrefixTm,
                InvoiceDateUtc = _now,
                TaxPointUtc = _now,
                LineDescription = "Starter plan (Monthly)",
                Quantity = 1,
                NetPence = 3900,
                VatRateBps = 2000,
                VatPence = 780,
                GrossPence = 4680,
                Currency = TummlyVatInvoice.CurrencyGbp,
                PaymentStatus = TummlyVatInvoice.PaymentStatusPaid,
                CustomerBusinessName = "VAT Venue Ltd",
                CustomerAddress = "10 High Street, London",
                SellerLegalName = "Tummly Ltd",
                SellerRegisteredAddress = "1 Example Road",
                SellerVatRegistrationNumber = "GB999",
            };

            var pdf = System.Text.Encoding.ASCII.GetString(
                TummlyVatInvoicePdfWriter.Render(invoice)
            );

            Assert.StartsWith("%PDF", pdf);
            Assert.Contains("TAX INVOICE", pdf, StringComparison.Ordinal);
            Assert.Contains("TM-2026-000001", pdf, StringComparison.Ordinal);
            Assert.Contains("Tummly Ltd", pdf, StringComparison.Ordinal);
            Assert.Contains("GB999", pdf, StringComparison.Ordinal);
            Assert.Contains("VAT Venue Ltd", pdf, StringComparison.Ordinal);
            Assert.Contains("Starter plan", pdf, StringComparison.Ordinal);
            Assert.Contains("\\(Monthly\\)", pdf, StringComparison.Ordinal);
            Assert.Contains("GBP 39", pdf, StringComparison.Ordinal);
            Assert.Contains("GBP 46.80", pdf, StringComparison.Ordinal);
            Assert.Contains("Payment status: Paid", pdf, StringComparison.Ordinal);
            Assert.Contains("/Helvetica-Bold", pdf, StringComparison.Ordinal);
            Assert.DoesNotContain("CREDIT NOTE", pdf, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_CreditNote_UsesCreditNoteTitle()
        {
            var invoice = new TummlyVatInvoice
            {
                DocumentNumber = "TCN-2026-000001",
                DocumentPrefix = TummlyVatInvoice.PrefixTcn,
                InvoiceDateUtc = _now,
                TaxPointUtc = _now,
                LineDescription = "Credit note for TM-2026-000001",
                Quantity = 1,
                NetPence = 3900,
                VatRateBps = 2000,
                VatPence = 780,
                GrossPence = 4680,
                Currency = TummlyVatInvoice.CurrencyGbp,
                PaymentStatus = TummlyVatInvoice.PaymentStatusPaid,
                CustomerBusinessName = "VAT Venue Ltd",
                SellerLegalName = "Tummly Ltd",
                SellerVatRegistrationNumber = "GB999",
            };

            var pdf = System.Text.Encoding.ASCII.GetString(
                TummlyVatInvoicePdfWriter.Render(invoice)
            );

            Assert.Contains("CREDIT NOTE", pdf, StringComparison.Ordinal);
            Assert.Contains("TCN-2026-000001", pdf, StringComparison.Ordinal);
            Assert.DoesNotContain("TAX INVOICE", pdf, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Mint_AllocatesDistinctNumbers_ForDistinctOrders()
        {
            await using var context = CreateContext();
            var restaurantId = await SeedRestaurantAsync(context);
            var service = CreateService(context);

            var a = await service.MintForCompletedOrderAsync(
                Request(restaurantId, "ord_a")
            );
            var b = await service.MintForCompletedOrderAsync(
                Request(restaurantId, "ord_b")
            );

            Assert.Equal("TM-2026-000001", a.DocumentNumber);
            Assert.Equal("TM-2026-000002", b.DocumentNumber);
        }

        [Fact]
        public void VatPenceFromNetPence_UsesHalfUpAwayFromZero()
        {
            Assert.Equal(780, TummlyVatMath.VatPenceFromNetPence(3900));
            Assert.Equal(200, TummlyVatMath.VatPenceFromNetPence(1001));
            Assert.Equal(1201, TummlyVatMath.GrossMinorFromNetPence(1001));
        }

        [Fact]
        public async Task Mint_FailsClosed_WhenSellerVatIncomplete()
        {
            await using var context = CreateContext();
            var restaurantId = await SeedRestaurantAsync(context);
            var service = new TummlyVatInvoiceService(
                context,
                _pricebook,
                Options.Create(new TummlySellerVatSettings())
            );

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.MintForCompletedOrderAsync(Request(restaurantId, "ord_no_vat"))
            );
            Assert.Equal(RevolutMerchantCreateGate.VatNotReady, ex.Message);
            Assert.Equal(0, await context.TummlyVatInvoices.CountAsync());
        }

        [Fact]
        public void MissingVatEnv_StillFailClosedBeforePay()
        {
            var gate = new RevolutMerchantCreateGate(
                Options.Create(new TummlySellerVatSettings()),
                Options.Create(
                    new RevolutSettings
                    {
                        SecretKey = "sk_test",
                        ApiBaseUrl = RevolutSettings.SandboxApiBaseUrl,
                        ApiVersion = RevolutSettings.DefaultApiVersion,
                    }
                )
            );

            Assert.Equal(
                RevolutMerchantCreateGate.VatNotReady,
                gate.Evaluate(planVariationLookupKey: null)
            );
        }

        private TummlyVatInvoiceMintRequest Request(int restaurantId, string orderId)
        {
            return new TummlyVatInvoiceMintRequest(
                RevolutOrderId: orderId,
                RevolutSubscriptionId: null,
                RestaurantId: restaurantId,
                Plan: BillingSubscriptionPlans.Starter,
                BillingCycle: BillingCycles.Monthly,
                PaymentSuccessUtc: _now
            );
        }

        private TummlyVatInvoiceService CreateService(ApplicationDbContext context)
        {
            return new TummlyVatInvoiceService(
                context,
                _pricebook,
                Options.Create(
                    new TummlySellerVatSettings
                    {
                        RegistrationNumber = "GB999",
                        EffectiveDate = "2024-01-01",
                        LegalName = "Tummly Ltd",
                        RegisteredAddress = "1 Example Road",
                    }
                )
            );
        }

        private async Task<int> SeedRestaurantAsync(ApplicationDbContext context)
        {
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "VAT Owner",
                Role = "Owner",
                CreatedAt = _now,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "VAT Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = _now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    _pricebook.CurrentPricebookId
                )
            );
            context.RestaurantBusinessDetails.Add(
                new RestaurantBusinessDetails
                {
                    RestaurantId = restaurant.Id,
                    LegalBusinessName = "VAT Venue Ltd",
                    AddressLine1 = "10 Market Street",
                    TownCity = "London",
                    Postcode = "E1 1AA",
                    Country = "United Kingdom",
                }
            );
            await context.SaveChangesAsync();
            return restaurant.Id;
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            return new ApplicationDbContext(options);
        }
    }
}
