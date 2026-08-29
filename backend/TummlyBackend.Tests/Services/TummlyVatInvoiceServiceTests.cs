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
