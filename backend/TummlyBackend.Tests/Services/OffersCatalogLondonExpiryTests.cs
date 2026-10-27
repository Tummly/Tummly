using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Offer "today" is the London date, not the UTC date. During BST the
    /// London date is one day ahead of UTC between 23:00Z and 23:59Z.
    /// </summary>
    public class OffersCatalogLondonExpiryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private DateTime _now = new(2026, 7, 15, 22, 30, 0, DateTimeKind.Utc);

        public OffersCatalogLondonExpiryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task IsAttachableForLocationAsync_FixedExpiryYesterdayLondon_NotAttachableAt2330Utc()
        {
            var seeded = await SeedActiveOfferAsync(new DateOnly(2026, 7, 15));
            var service = new OffersCatalogService(_context, () => _now);

            _now = new DateTime(2026, 7, 15, 23, 30, 0, DateTimeKind.Utc);

            Assert.False(
                await service.IsAttachableForLocationAsync(
                    seeded.OfferId,
                    seeded.LocationId
                )
            );
        }

        [Fact]
        public async Task IsAttachableForLocationAsync_FixedExpiryToday_AttachableAt2230Utc()
        {
            var seeded = await SeedActiveOfferAsync(new DateOnly(2026, 7, 15));
            var service = new OffersCatalogService(_context, () => _now);

            Assert.True(
                await service.IsAttachableForLocationAsync(
                    seeded.OfferId,
                    seeded.LocationId
                )
            );
        }

        private async Task<(int LocationId, int OfferId)> SeedActiveOfferAsync(
            DateOnly customExpiryDate
        )
        {
            var restaurant = new Restaurant
            {
                Name = "London Expiry Rest",
                OwnerUserId = 7,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "London Expiry Loc",
                Address = "1 High St",
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var offer = new CatalogOffer
            {
                RestaurantLocationId = location.Id,
                Status = CatalogOfferStatus.Active,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "Fixed expiry 10%",
                Description = "Ends on a fixed London date",
                Validity = CatalogOfferValidity.ChooseExpiryDate,
                CustomExpiryDate = customExpiryDate,
                DiscountPercentage = 10m,
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            _context.CatalogOffers.Add(offer);
            await _context.SaveChangesAsync();

            return (location.Id, offer.Id);
        }
    }
}
