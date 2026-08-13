using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: Existing-offer picker (Recovery + Campaign) lists with pageSize 100.
    /// </summary>
    public class OffersCatalogServicePagingTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OffersCatalogService _service;
        private readonly DateTime _now = new(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);

        public OffersCatalogServicePagingTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new OffersCatalogService(_context, () => _now);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task ListAsync_ExistingOfferPickerQuery_AllowsPageSize100()
        {
            var seeded = await SeedActiveOfferAsync();

            var response = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "all",
                    Sort = "recent-activity",
                    Page = 1,
                    PageSize = 100,
                    Status = new[] { CatalogOfferStatus.Active },
                    UtcOffsetMinutes = 300,
                }
            );

            Assert.Equal(100, response.PageSize);
            Assert.Equal(1, response.TotalCount);
            Assert.Single(response.Items);
            Assert.Equal(seeded.OfferId, response.Items[0].Id);
            Assert.Equal(CatalogOfferStatus.Active, response.Items[0].Status);
        }

        private async Task<(int LocationId, int OfferId)> SeedActiveOfferAsync()
        {
            var restaurant = new Restaurant
            {
                Name = "Picker Rest",
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Picker Loc",
                Address = "3 High St",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var offer = new CatalogOffer
            {
                RestaurantLocationId = location.Id,
                Status = CatalogOfferStatus.Active,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "Picker 10%",
                Description = "Existing offer picker",
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
