using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.SqlServer
{
    [Collection(SqlServerCollection.Name)]
    public sealed class GuestDeleteSqlTests
    {
        private readonly SqlServerFixture _fixture;
        private readonly DateTime _now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

        public GuestDeleteSqlTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        [SkippableFact]
        public async Task Delete_GuestWithClaimAndCampaignRows_Completes()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            SeededLinkedGuest seeded;
            await using (var seedContext = _fixture.CreateContext())
            {
                seeded = await SeedGuestWithClaimAndCampaignAsync(seedContext);
            }

            // Fresh context: dependents are not tracked, so SQL Server enforces
            // the Restrict FK (X1) until hard-delete unlinks first.
            await using var context = _fixture.CreateContext();
            var hasher = new GuestContactHasher("test-key");
            var suppressions = new GuestContactSuppressionService(context, hasher);
            var audit = new AdminAuditService(context, TimeProvider.System);
            var delete = new LocationGuestDeleteService(
                context,
                new OwnedLocationService(context),
                audit,
                suppressions
            );

            var exception = await Record.ExceptionAsync(
                () =>
                    delete.DeleteAsync(
                        seeded.OwnerUserId,
                        seeded.LocationGuestId,
                        seeded.LocationId
                    )
            );

            Assert.Null(exception);
            Assert.False(
                await context.LocationGuests.AnyAsync(
                    lg => lg.Id == seeded.LocationGuestId
                )
            );

            Assert.Null(
                (
                    await context.OfferIssues.SingleAsync(
                        o => o.Id == seeded.ClaimId
                    )
                ).LocationGuestId
            );
            Assert.Null(
                (
                    await context.CampaignFrozenRecipients.SingleAsync(
                        r => r.Id == seeded.FrozenId
                    )
                ).LocationGuestId
            );
            Assert.Null(
                (
                    await context.CampaignRecipientDeliveries.SingleAsync(
                        d => d.Id == seeded.DeliveryId
                    )
                ).LocationGuestId
            );
        }

        [SkippableFact]
        public async Task Purge_GuestWithClaimAndCampaignRows_Completes()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            SeededLinkedGuest seeded;
            await using (var seedContext = _fixture.CreateContext())
            {
                seeded = await SeedGuestWithClaimAndCampaignAsync(
                    seedContext,
                    dormantEnteredAt: _now.AddDays(-91)
                );
            }

            await using var context = _fixture.CreateContext();
            var hasher = new GuestContactHasher("test-key");
            var suppressions = new GuestContactSuppressionService(context, hasher);
            var audit = new AdminAuditService(context, TimeProvider.System);
            var purge = new GuestRetentionPurgeService(
                context,
                audit,
                suppressions,
                NullLogger<GuestRetentionPurgeService>.Instance
            );

            var result = await purge.ProcessOnceAsync(_now);

            Assert.Equal(1, result.EligibleFound);
            Assert.Equal(1, result.Purged);
            Assert.Equal(0, result.Failed);
            Assert.Equal(1, result.GuestsDeleted);

            Assert.False(
                await context.LocationGuests.AnyAsync(
                    lg => lg.Id == seeded.LocationGuestId
                )
            );
            Assert.Null(
                (
                    await context.OfferIssues.SingleAsync(
                        o => o.Id == seeded.ClaimId
                    )
                ).LocationGuestId
            );
            Assert.Null(
                (
                    await context.CampaignFrozenRecipients.SingleAsync(
                        r => r.Id == seeded.FrozenId
                    )
                ).LocationGuestId
            );
            Assert.Null(
                (
                    await context.CampaignRecipientDeliveries.SingleAsync(
                        d => d.Id == seeded.DeliveryId
                    )
                ).LocationGuestId
            );

            var ba = await context.BillingAccounts.SingleAsync(a =>
                a.RestaurantId == seeded.RestaurantId
            );
            Assert.Equal(_now, ba.GuestRetentionPurgedAtUtc);
        }

        [SkippableFact]
        public async Task Purge_TwoLocationGuestsSameContact_WritesOneSuppressionRowPerKind()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            SeededTwoGuests seeded;
            await using (var seedContext = _fixture.CreateContext())
            {
                seeded = await SeedTwoLocationGuestsSameMasterAsync(seedContext);
            }

            await using var context = _fixture.CreateContext();
            var hasher = new GuestContactHasher("test-key");
            var suppressions = new GuestContactSuppressionService(context, hasher);
            var audit = new AdminAuditService(context, TimeProvider.System);
            var purge = new GuestRetentionPurgeService(
                context,
                audit,
                suppressions,
                NullLogger<GuestRetentionPurgeService>.Instance
            );

            var result = await purge.ProcessOnceAsync(_now);

            Assert.Equal(1, result.Purged);
            Assert.Equal(2, result.GuestsDeleted);

            var rows = await context.GuestContactSuppressions
                .Where(r => r.RestaurantId == seeded.RestaurantId)
                .ToListAsync();

            Assert.Equal(3, rows.Count);
            Assert.Equal(
                3,
                rows.Select(r => r.PermissionKind).Distinct().Count()
            );
            Assert.All(
                rows,
                row =>
                {
                    Assert.Equal(
                        GuestContactSuppressionSources.RetentionPurge,
                        row.Source
                    );
                    Assert.Equal(
                        hasher.Hash("email:shared@example.com"),
                        row.ContactHash
                    );
                }
            );
        }

        private async Task<SeededLinkedGuest> SeedGuestWithClaimAndCampaignAsync(
            TummlyBackend.Data.ApplicationDbContext context,
            DateTime? dormantEnteredAt = null
        )
        {
            var suffix = Guid.NewGuid().ToString("N");
            var user = new User
            {
                FullName = "Owner",
                Email = $"sql-del-{suffix}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900000",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = _now,
                ActivatedAt = _now,
                ActivationExpiresAt = _now.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = $"SQL Delete Restaurant {suffix}",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = _now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            if (dormantEnteredAt is DateTime dormantAt)
            {
                context.BillingAccounts.Add(
                    new BillingAccount
                    {
                        RestaurantId = restaurant.Id,
                        SubscriptionPlan = BillingSubscriptionPlans.Pilot,
                        BillingStatus = BillingStatuses.Dormant,
                        ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
                        StarterKitState = StarterKitStates.Unused,
                        DormantEnteredAt = dormantAt,
                    }
                );
            }

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Soho",
                CreatedAt = _now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = $"guest-{suffix}@example.com",
                NormalizedEmail = $"guest-{suffix}@example.com",
                CreatedAt = _now,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "SQL Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = _now,
            };
            context.LocationGuests.Add(guest);
            await context.SaveChangesAsync();

            var offer = new CatalogOffer
            {
                RestaurantLocationId = location.Id,
                Status = "active",
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "SQL offer",
                Description = "desc",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 10m,
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            context.CatalogOffers.Add(offer);
            await context.SaveChangesAsync();

            var campaign = new Campaign
            {
                RestaurantLocationId = location.Id,
                Status = "sending",
                Name = "SQL delete campaign",
                GoalId = "thank-recent-guests",
                AudienceKey = "all-eligible-guests",
                Channel = "email",
                OfferStance = "include-offer",
                OfferId = offer.Id,
                MessageSubject = "Hi",
                MessageBody = "Body",
                ScheduleMode = "send-now",
                ScheduleTimeZone = "Europe/London",
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            context.Campaigns.Add(campaign);
            await context.SaveChangesAsync();

            var claim = new OfferIssue
            {
                CatalogOfferId = offer.Id,
                LocationGuestId = guest.Id,
                CampaignId = campaign.Id,
                ClaimCode = $"TUM-D{suffix}"[..12].ToUpperInvariant(),
                IssuedAtUtc = _now,
                ClaimedAtUtc = _now,
                Source = OfferIssueSources.Campaign,
                ExpiryAtUtc = _now.AddDays(7),
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "SQL offer",
                Description = "desc",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 10m,
            };
            var frozen = new CampaignFrozenRecipient
            {
                CampaignId = campaign.Id,
                LocationGuestId = guest.Id,
                FrozenAtUtc = _now,
            };
            var delivery = new CampaignRecipientDelivery
            {
                CampaignId = campaign.Id,
                LocationGuestId = guest.Id,
                Channel = "email",
                Outcome = "accepted",
                AcceptedAtUtc = _now,
                AcceptedUnits = 1,
                UpdatedAtUtc = _now,
            };
            context.OfferIssues.Add(claim);
            context.CampaignFrozenRecipients.Add(frozen);
            context.CampaignRecipientDeliveries.Add(delivery);
            await context.SaveChangesAsync();

            return new SeededLinkedGuest(
                user.Id,
                restaurant.Id,
                location.Id,
                guest.Id,
                claim.Id,
                frozen.Id,
                delivery.Id
            );
        }

        private async Task<SeededTwoGuests> SeedTwoLocationGuestsSameMasterAsync(
            TummlyBackend.Data.ApplicationDbContext context
        )
        {
            var suffix = Guid.NewGuid().ToString("N");
            var user = new User
            {
                FullName = "Owner",
                Email = $"sql-two-{suffix}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900000",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = _now,
                ActivatedAt = _now,
                ActivationExpiresAt = _now.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = $"SQL Two Guest Restaurant {suffix}",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = _now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                new BillingAccount
                {
                    RestaurantId = restaurant.Id,
                    SubscriptionPlan = BillingSubscriptionPlans.Pilot,
                    BillingStatus = BillingStatuses.Dormant,
                    ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
                    StarterKitState = StarterKitStates.Unused,
                    DormantEnteredAt = _now.AddDays(-91),
                }
            );

            var locationA = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Soho",
                CreatedAt = _now,
            };
            var locationB = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Shoreditch",
                CreatedAt = _now,
            };
            context.RestaurantLocations.AddRange(locationA, locationB);
            await context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "shared@example.com",
                NormalizedEmail = "shared@example.com",
                CreatedAt = _now,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            context.LocationGuests.AddRange(
                new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = locationA.Id,
                    Name = "Shared A",
                    MarketingPreference =
                        LocationGuestMarketingPreference.OptedOut,
                    CreatedAt = _now,
                },
                new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = locationB.Id,
                    Name = "Shared B",
                    MarketingPreference =
                        LocationGuestMarketingPreference.OptedOut,
                    CreatedAt = _now,
                }
            );
            await context.SaveChangesAsync();

            return new SeededTwoGuests(restaurant.Id);
        }

        private sealed record SeededLinkedGuest(
            int OwnerUserId,
            int RestaurantId,
            int LocationId,
            int LocationGuestId,
            int ClaimId,
            int FrozenId,
            int DeliveryId
        );

        private sealed record SeededTwoGuests(int RestaurantId);
    }
}
