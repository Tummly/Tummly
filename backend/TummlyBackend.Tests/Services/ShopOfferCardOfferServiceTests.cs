using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: set / clear / get Offer Card print OfferId on RestaurantLocation.
    /// </summary>
    public class ShopOfferCardOfferServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ShopOfferCardOfferService _service;
        private readonly RecordingPrintReadyWork _printWork = new();
        private readonly DateTime _now = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

        public ShopOfferCardOfferServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            var offers = new OffersCatalogService(_context, () => _now);
            _service = new ShopOfferCardOfferService(
                _context,
                offers,
                _printWork,
                () => _now
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task SetAsync_AttachesActiveOffer_AndGetReturnsSetupComplete()
        {
            var seeded = await SeedLocationAndOfferAsync(status: "active");

            var set = await _service.SetAsync(seeded.LocationId, seeded.OfferId);

            var ok = Assert.IsType<ShopOfferCardOfferSetResult.Ok>(set);
            Assert.Equal(seeded.OfferId, ok.Value.OfferCardOfferId);
            Assert.Equal("Card 20% off", ok.Value.OfferCardOfferTitle);
            Assert.True(ok.Value.OfferCardOfferLive);
            Assert.Contains(seeded.LocationId, _printWork.EnsureLocationIds);

            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .FirstAsync(row => row.Id == seeded.LocationId);
            Assert.Equal(seeded.OfferId, location.OfferCardCatalogOfferId);

            var get = await _service.GetAsync(seeded.LocationId);
            Assert.Equal(seeded.OfferId, get.OfferCardOfferId);
            Assert.True(await _service.HasAttachAsync(seeded.LocationId));
        }

        [Fact]
        public async Task GetAsync_WhenOfferPaused_StillReturnsAttachedForSetup()
        {
            var seeded = await SeedLocationAndOfferAsync(status: "active");
            await _service.SetAsync(seeded.LocationId, seeded.OfferId);

            var offer = await _context.CatalogOffers
                .FirstAsync(row => row.Id == seeded.OfferId);
            offer.Status = "paused";
            await _context.SaveChangesAsync();

            var get = await _service.GetAsync(seeded.LocationId);

            Assert.Equal(seeded.OfferId, get.OfferCardOfferId);
            Assert.False(get.OfferCardOfferLive);
            Assert.True(await _service.HasAttachAsync(seeded.LocationId));
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

            Assert.IsType<ShopOfferCardOfferSetResult.InvalidOffer>(result);
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
                Title = "Card 20% off",
                Description = "Printed on cards",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 20m,
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            _context.CatalogOffers.Add(offer);
            await _context.SaveChangesAsync();

            return (location.Id, offer.Id);
        }

        private sealed class RecordingPrintReadyWork : IPrintReadyQrMaterialsWork
        {
            public List<int> EnsureLocationIds { get; } = [];

            public ValueTask RequestEnsureAsync(
                int locationId,
                CancellationToken cancellationToken = default
            )
            {
                EnsureLocationIds.Add(locationId);
                return ValueTask.CompletedTask;
            }

            public ValueTask RequestShopOrderEnsureAsync(
                Guid shopOrderId,
                CancellationToken cancellationToken = default
            ) => ValueTask.CompletedTask;

            public Task RunAsync(CancellationToken stoppingToken) =>
                Task.CompletedTask;

            public Task DrainAsync(CancellationToken cancellationToken = default) =>
                Task.CompletedTask;
        }
    }
}
