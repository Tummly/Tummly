using Microsoft.EntityFrameworkCore;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.SqlServer
{
    [Collection(SqlServerCollection.Name)]
    public sealed class UnlinkedGuestRowsSqlTests
    {
        private readonly SqlServerFixture _fixture;
        private readonly DateTime _now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

        public UnlinkedGuestRowsSqlTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        [SkippableFact]
        public async Task TwoUnlinkedRowsInOneCampaign_Accepted()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            await using var context = _fixture.CreateContext();

            var user = new User
            {
                FullName = "Owner",
                Email = $"sql-unlinked-{Guid.NewGuid():N}@example.com",
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
                Name = "SQL Unlinked Restaurant",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = _now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Soho",
                CreatedAt = _now,
            };
            context.RestaurantLocations.Add(location);
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
                Name = "SQL unlinked campaign",
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

            context.CampaignFrozenRecipients.AddRange(
                new CampaignFrozenRecipient
                {
                    CampaignId = campaign.Id,
                    LocationGuestId = null,
                    FrozenAtUtc = _now,
                },
                new CampaignFrozenRecipient
                {
                    CampaignId = campaign.Id,
                    LocationGuestId = null,
                    FrozenAtUtc = _now,
                }
            );

            context.CampaignRecipientDeliveries.AddRange(
                new CampaignRecipientDelivery
                {
                    CampaignId = campaign.Id,
                    LocationGuestId = null,
                    Channel = "email",
                    Outcome = "accepted",
                    AcceptedAtUtc = _now,
                    AcceptedUnits = 1,
                    UpdatedAtUtc = _now,
                },
                new CampaignRecipientDelivery
                {
                    CampaignId = campaign.Id,
                    LocationGuestId = null,
                    Channel = "email",
                    Outcome = "accepted",
                    AcceptedAtUtc = _now,
                    AcceptedUnits = 1,
                    UpdatedAtUtc = _now,
                }
            );

            context.OfferIssues.AddRange(
                new OfferIssue
                {
                    CatalogOfferId = offer.Id,
                    LocationGuestId = null,
                    CampaignId = campaign.Id,
                    ClaimCode = $"TUM-S{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                    IssuedAtUtc = _now,
                    ClaimedAtUtc = _now,
                    Source = OfferIssueSources.Campaign,
                    ExpiryAtUtc = _now.AddDays(7),
                    OfferType = CatalogOfferType.PercentageDiscount,
                    Title = "SQL offer",
                    Description = "desc",
                    Validity = CatalogOfferValidity.Days14AfterIssue,
                    DiscountPercentage = 10m,
                },
                new OfferIssue
                {
                    CatalogOfferId = offer.Id,
                    LocationGuestId = null,
                    CampaignId = campaign.Id,
                    ClaimCode = $"TUM-T{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                    IssuedAtUtc = _now,
                    ClaimedAtUtc = _now,
                    Source = OfferIssueSources.Campaign,
                    ExpiryAtUtc = _now.AddDays(7),
                    OfferType = CatalogOfferType.PercentageDiscount,
                    Title = "SQL offer",
                    Description = "desc",
                    Validity = CatalogOfferValidity.Days14AfterIssue,
                    DiscountPercentage = 10m,
                }
            );

            var exception = await Record.ExceptionAsync(
                () => context.SaveChangesAsync()
            );

            Assert.Null(exception);
        }
    }
}
