using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: set / clear / get thank-you OfferId on RestaurantLocation (ticket 07).
    /// </summary>
    public class CaptureThankYouOfferServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly CaptureThankYouOfferService _service;
        private readonly DateTime _now = new(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);

        public CaptureThankYouOfferServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            var offers = new OffersCatalogService(_context, () => _now);
            _service = new CaptureThankYouOfferService(
                _context,
                offers,
                () => _now
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task SetAsync_AttachesActiveOffer_AndGetReturnsLive()
        {
            var seeded = await SeedLocationAndOfferAsync(status: "active");

            var set = await _service.SetAsync(seeded.LocationId, seeded.OfferId);

            var ok = Assert.IsType<CaptureThankYouOfferSetResult.Ok>(set);
            Assert.Equal(seeded.OfferId, ok.Value.ThankYouOfferId);
            Assert.Equal("Thank you 10%", ok.Value.ThankYouOfferTitle);
            Assert.True(ok.Value.ThankYouOfferLive);

            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .FirstAsync(row => row.Id == seeded.LocationId);
            Assert.Equal(seeded.OfferId, location.ThankYouCatalogOfferId);

            var get = await _service.GetAsync(seeded.LocationId);
            Assert.Equal(seeded.OfferId, get.ThankYouOfferId);
            Assert.True(get.ThankYouOfferLive);
        }

        [Fact]
        public async Task SetAsync_Clear_SetsNull()
        {
            var seeded = await SeedLocationAndOfferAsync(status: "active");
            await _service.SetAsync(seeded.LocationId, seeded.OfferId);

            var cleared = await _service.SetAsync(seeded.LocationId, null);

            var ok = Assert.IsType<CaptureThankYouOfferSetResult.Ok>(cleared);
            Assert.Null(ok.Value.ThankYouOfferId);
            Assert.False(ok.Value.ThankYouOfferLive);

            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .FirstAsync(row => row.Id == seeded.LocationId);
            Assert.Null(location.ThankYouCatalogOfferId);
        }

        [Fact]
        public async Task SetAsync_RejectsPausedOffer()
        {
            var seeded = await SeedLocationAndOfferAsync(status: "paused");

            var result = await _service.SetAsync(
                seeded.LocationId,
                seeded.OfferId
            );

            Assert.IsType<CaptureThankYouOfferSetResult.InvalidOffer>(result);
            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .FirstAsync(row => row.Id == seeded.LocationId);
            Assert.Null(location.ThankYouCatalogOfferId);
        }

        [Fact]
        public async Task SetAsync_RejectsOfferFromOtherLocation()
        {
            var seeded = await SeedLocationAndOfferAsync(status: "active");
            var other = await SeedLocationAndOfferAsync(
                status: "active",
                nameSuffix: "Other"
            );

            var result = await _service.SetAsync(
                seeded.LocationId,
                other.OfferId
            );

            Assert.IsType<CaptureThankYouOfferSetResult.InvalidOffer>(result);
        }

        [Fact]
        public async Task GetAsync_WhenStoredOfferPaused_ReturnsLiveFalse()
        {
            var seeded = await SeedLocationAndOfferAsync(status: "active");
            await _service.SetAsync(seeded.LocationId, seeded.OfferId);

            var offer = await _context.CatalogOffers
                .FirstAsync(row => row.Id == seeded.OfferId);
            offer.Status = "paused";
            await _context.SaveChangesAsync();

            var get = await _service.GetAsync(seeded.LocationId);

            Assert.Equal(seeded.OfferId, get.ThankYouOfferId);
            Assert.Equal("Thank you 10%", get.ThankYouOfferTitle);
            Assert.False(get.ThankYouOfferLive);
        }

        private async Task<(int LocationId, int OfferId)> SeedLocationAndOfferAsync(
            string status,
            string nameSuffix = ""
        )
        {
            var restaurant = new Restaurant
            {
                Name = $"Rest{nameSuffix}",
                OwnerUserId = 7,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = $"Loc{nameSuffix}",
                Address = "1 High St",
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var offer = new CatalogOffer
            {
                RestaurantLocationId = location.Id,
                Status = status,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "Thank you 10%",
                Description = "Thanks for feedback",
                Validity = CatalogOfferValidity.Days14AfterIssue,
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
