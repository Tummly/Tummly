using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public sealed class CreditReservationSweeperBackgroundServiceTests
    {
        private const string PricebookId = "TUMMLY-UK-GBP-2026-08-V3";
        private readonly DateTime _now = new(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task SweepExpiredHolds_ReleasesOpenHoldAndReturnsAvailable()
        {
            var databaseName = Guid.NewGuid().ToString();
            var clock = new FixedTimeProvider(_now);
            await using var context = CreateContext(databaseName);

            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Sweep Owner",
                Role = "Owner",
                CreatedAt = _now,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Sweep Venue",
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
                    PricebookId
                )
            );
            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = _now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurant.Id,
                    Channel = CreditChannels.Sms,
                    EntryType = CreditLedgerEntryTypes.PilotAllocation,
                    Quantity = 4,
                    PricebookVersion = PricebookId,
                    CreatedAtUtc = _now.AddDays(-1),
                }
            );
            await context.SaveChangesAsync();

            var ledger = new CreditLedgerService(
                context,
                clock,
                new StubPricebookCatalog()
            );
            var snapshot = new CreditBalanceSnapshotService(context, clock);
            var billing = new LiveRecoverySmsBillingReserve(
                context,
                ledger,
                snapshot
            );

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                GuestName = "Alex",
                GuestContact = "+447700900111",
                ContactType = ContactType.Phone,
                Comment = "Hi",
                CreatedAt = _now,
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Negative,
                DetectedTagsJson = "[]",
                WorkflowStatus = FeedbackWorkflowStatus.InProgress,
            };
            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();

            var reserve = await billing.ReserveAsync(
                new RecoverySmsBillingReserveRequest
                {
                    FeedbackId = feedback.Id,
                    LocationId = location.Id,
                    Units = 2,
                }
            );
            Assert.True(reserve is RecoverySmsBillingReserveResult.Ok);
            var reservationRef =
                ((RecoverySmsBillingReserveResult.Ok)reserve).ReservationRef;

            context.RecoverySmsSendIdempotencies.Add(
                new RecoverySmsSendIdempotency
                {
                    RestaurantId = restaurant.Id,
                    FeedbackId = feedback.Id,
                    IdempotencyKey = $"anon:{reservationRef}",
                    ReservationRef = reservationRef,
                    ReservedUnits = 2,
                    ReservedAtUtc = _now.AddMinutes(-20),
                    HoldExpiresAtUtc = _now.AddMinutes(-5),
                }
            );
            await context.SaveChangesAsync();

            var services = new ServiceCollection();
            services.AddSingleton(context);
            services.AddSingleton<IRecoverySmsBillingReserve>(billing);
            await using var provider = services.BuildServiceProvider();
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

            var sweeper = new CreditReservationSweeperBackgroundService(
                scopeFactory,
                clock,
                NullLogger<CreditReservationSweeperBackgroundService>.Instance
            );

            await sweeper.SweepExpiredHoldsAsync(CancellationToken.None);

            Assert.False(
                await context.RecoverySmsSendIdempotencies.AnyAsync()
            );
            var account = await snapshot.GetAccountAsync(restaurant.Id);
            var sms = Assert.Single(
                account!.Channels,
                row => row.Channel == CreditChannels.Sms
            );
            Assert.Equal(4, sms.Remaining);
            Assert.Equal(0, sms.Held);
        }

        private static ApplicationDbContext CreateContext(string databaseName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            return new ApplicationDbContext(options);
        }

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;

            public FixedTimeProvider(DateTime utcNow)
            {
                _utcNow = new DateTimeOffset(utcNow, TimeSpan.Zero);
            }

            public override DateTimeOffset GetUtcNow() => _utcNow;
        }

        private sealed class StubPricebookCatalog : IPricebookCatalog
        {
            public string CurrentPricebookId => PricebookId;

            public PricebookSnapshot GetRequired(string pricebookId) =>
                throw new NotImplementedException();

            public string FormatPlanPriceNet(PricebookPlan plan, string? billingCycle) =>
                throw new NotImplementedException();

            public string FormatIncludedCreditsLabel(PricebookPlan plan, string channel) =>
                throw new NotImplementedException();

            public BillingCurrentCatalogDto BuildCurrentCatalog(bool sms5000Available) =>
                throw new NotImplementedException();
        }
    }
}
