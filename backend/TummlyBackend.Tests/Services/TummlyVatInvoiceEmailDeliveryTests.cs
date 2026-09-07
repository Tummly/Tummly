using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class TummlyVatInvoiceEmailDeliveryTests
    {
        private readonly IPricebookCatalog _pricebook = TestPricebookPaths.LoadV3();
        private readonly DateTime _now = new(2026, 2, 15, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task DeliverIfNew_SendsToBillingEmail_WhenSet()
        {
            await using var context = CreateContext();
            var (restaurant, _) = await SeedRestaurantAsync(
                context,
                billingEmail: "invoices@venue.test",
                contactEmail: "contact@venue.test"
            );
            var invoice = await SeedInvoiceAsync(context, restaurant.Id);
            var email = new TrackingInvoiceEmailService();
            var delivery = CreateDelivery(context, email);

            await delivery.DeliverIfNewAsync(invoice, wasNewlyMinted: true);

            Assert.Single(email.Calls);
            Assert.Equal("invoices@venue.test", email.Calls[0].ToEmail);
            Assert.Equal(invoice.DocumentNumber, email.Calls[0].DocumentNumber);
            Assert.True(email.Calls[0].PdfContent.Length > 0);
            Assert.EndsWith(".pdf", email.Calls[0].PdfFileName);
        }

        [Fact]
        public async Task DeliverIfNew_FallsBackToBillingContact_WhenBillingEmailEmpty()
        {
            await using var context = CreateContext();
            var (restaurant, contact) = await SeedRestaurantAsync(
                context,
                billingEmail: null,
                contactEmail: "contact@venue.test"
            );
            var invoice = await SeedInvoiceAsync(context, restaurant.Id);
            var email = new TrackingInvoiceEmailService();
            var delivery = CreateDelivery(context, email);

            await delivery.DeliverIfNewAsync(invoice, wasNewlyMinted: true);

            Assert.Single(email.Calls);
            Assert.Equal(contact.Email, email.Calls[0].ToEmail);
        }

        [Fact]
        public async Task DeliverIfNew_Skips_WhenNotNewlyMinted()
        {
            await using var context = CreateContext();
            var (restaurant, _) = await SeedRestaurantAsync(
                context,
                billingEmail: "invoices@venue.test",
                contactEmail: "contact@venue.test"
            );
            var invoice = await SeedInvoiceAsync(context, restaurant.Id);
            var email = new TrackingInvoiceEmailService();
            var delivery = CreateDelivery(context, email);

            await delivery.DeliverIfNewAsync(invoice, wasNewlyMinted: false);

            Assert.Empty(email.Calls);
        }

        private TummlyVatInvoiceEmailDelivery CreateDelivery(
            ApplicationDbContext context,
            IEmailService email
        )
        {
            var vat = new TummlyVatInvoiceService(
                context,
                _pricebook,
                Options.Create(
                    new TummlySellerVatSettings
                    {
                        RegistrationNumber = "GB123456789",
                        EffectiveDate = "2024-01-01",
                        LegalName = "Tummly Ltd",
                        RegisteredAddress = "1 High Street",
                    }
                )
            );
            return new TummlyVatInvoiceEmailDelivery(context, vat, email);
        }

        private async Task<(Restaurant Restaurant, User Contact)> SeedRestaurantAsync(
            ApplicationDbContext context,
            string? billingEmail,
            string contactEmail
        )
        {
            var owner = new User
            {
                Email = $"owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Owner",
                Role = "Owner",
                CreatedAt = _now,
            };
            var contact = new User
            {
                Email = contactEmail,
                PasswordHash = "x",
                FullName = "Billing Contact",
                Role = "Owner",
                CreatedAt = _now,
            };
            context.Users.AddRange(owner, contact);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Invoice Email Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = contact.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = _now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var account = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                _pricebook.CurrentPricebookId
            );
            account.BillingEmail = billingEmail;
            context.BillingAccounts.Add(account);
            await context.SaveChangesAsync();

            return (restaurant, contact);
        }

        private async Task<TummlyVatInvoice> SeedInvoiceAsync(
            ApplicationDbContext context,
            int restaurantId
        )
        {
            var invoice = new TummlyVatInvoice
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "TM-2026-000099",
                DocumentPrefix = "TM",
                RevolutOrderId = $"ord_{Guid.NewGuid():N}",
                RestaurantId = restaurantId,
                InvoiceDateUtc = _now,
                TaxPointUtc = _now,
                LineDescription = "AI credits top-up",
                Quantity = 1,
                NetPence = 1000,
                VatRateBps = 2000,
                VatPence = 200,
                GrossPence = 1200,
                Currency = TummlyVatInvoice.CurrencyGbp,
                PaymentStatus = TummlyVatInvoice.PaymentStatusPaid,
                CustomerBusinessName = "Venue",
                CustomerAddress = "1 Street",
                SellerLegalName = "Tummly Ltd",
                SellerRegisteredAddress = "1 High Street",
                SellerVatRegistrationNumber = "GB123456789",
            };
            context.TummlyVatInvoices.Add(invoice);
            await context.SaveChangesAsync();
            return invoice;
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

        private sealed class TrackingInvoiceEmailService : EmailServiceStubBase
        {
            public List<(
                string ToEmail,
                string DocumentNumber,
                string LineDescription,
                int GrossPence,
                byte[] PdfContent,
                string PdfFileName
            )> Calls { get; } = [];

            public override Task SendTummlyVatInvoiceEmailAsync(
                string toEmail,
                string documentNumber,
                string lineDescription,
                int grossPence,
                byte[] pdfContent,
                string pdfFileName
            )
            {
                Calls.Add(
                    (
                        toEmail,
                        documentNumber,
                        lineDescription,
                        grossPence,
                        pdfContent,
                        pdfFileName
                    )
                );
                return Task.CompletedTask;
            }
        }
    }
}
