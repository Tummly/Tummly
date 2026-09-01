using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class LocationDetailOfferCardsComposerTests
    {
        [Fact]
        public async Task ComposeAsync_ExcludesExpiredActiveStoredOffers()
        {
            var frozenUtc = new DateTime(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);
            var time = new FakeTimeProvider(frozenUtc);
            await using var context = CreateContext();
            var locationId = await SeedLocationWithOffersAsync(context, frozenUtc);

            var composer = new LocationDetailOfferCardsComposer(context, time);
            var cards = await composer.ComposeAsync(locationId);

            Assert.Single(cards);
            Assert.Equal("offer", cards[0].Kind);
            Assert.Equal("Still Live Offer", cards[0].Title);
        }

        [Fact]
        public async Task ComposeAsync_CapsMergedCardsAtFour()
        {
            var frozenUtc = new DateTime(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);
            var time = new FakeTimeProvider(frozenUtc);
            await using var context = CreateContext();
            var locationId = await SeedManyOffersAsync(context, frozenUtc, count: 6);

            var composer = new LocationDetailOfferCardsComposer(context, time);
            var cards = await composer.ComposeAsync(locationId);

            Assert.Equal(4, cards.Count);
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;
            return new ApplicationDbContext(options);
        }

        private static async Task<int> SeedLocationWithOffersAsync(
            ApplicationDbContext context,
            DateTime createdAt
        )
        {
            var restaurant = new Restaurant
            {
                Name = "Offer Cards Venue",
                AccountType = "Multi",
                OwnerUserId = 1,
                CreatedAt = createdAt,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden",
                Address = "1 High Street",
                LifecycleStatus = LocationLifecycleStatus.Active,
                CreatedAt = createdAt,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            context.CatalogOffers.AddRange(
                new CatalogOffer
                {
                    RestaurantLocationId = location.Id,
                    Title = "Expired Offer",
                    Description = "Expired",
                    Status = CatalogOfferStatus.Active,
                    Validity = CatalogOfferValidity.ChooseExpiryDate,
                    CustomExpiryDate = DateOnly.FromDateTime(
                        LondonDateFormat.ToLondonLocal(createdAt).AddDays(-1)
                    ),
                    OfferType = CatalogOfferType.PercentageDiscount,
                    UpdatedAt = createdAt.AddHours(-1),
                },
                new CatalogOffer
                {
                    RestaurantLocationId = location.Id,
                    Title = "Still Live Offer",
                    Description = "Live",
                    Status = CatalogOfferStatus.Active,
                    Validity = CatalogOfferValidity.Days30AfterIssue,
                    OfferType = CatalogOfferType.PercentageDiscount,
                    UpdatedAt = createdAt,
                }
            );
            await context.SaveChangesAsync();

            return location.Id;
        }

        private static async Task<int> SeedManyOffersAsync(
            ApplicationDbContext context,
            DateTime createdAt,
            int count
        )
        {
            var restaurant = new Restaurant
            {
                Name = "Many Offers Venue",
                AccountType = "Multi",
                OwnerUserId = 1,
                CreatedAt = createdAt,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Soho",
                Address = "2 High Street",
                LifecycleStatus = LocationLifecycleStatus.Active,
                CreatedAt = createdAt,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            for (var index = 0; index < count; index++)
            {
                context.CatalogOffers.Add(
                    new CatalogOffer
                    {
                        RestaurantLocationId = location.Id,
                        Title = $"Offer {index}",
                        Description = "Offer",
                        Status = CatalogOfferStatus.Active,
                        Validity = CatalogOfferValidity.Days30AfterIssue,
                        OfferType = CatalogOfferType.PercentageDiscount,
                        UpdatedAt = createdAt.AddMinutes(index),
                    }
                );
            }

            await context.SaveChangesAsync();
            return location.Id;
        }

        private sealed class FakeTimeProvider(DateTime utcNow) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
        }
    }
}
