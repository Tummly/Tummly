using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: Offers list In flight includes live thank-you attach (ticket 07).
    /// </summary>
    public class OffersCatalogThankYouAttachTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OffersCatalogService _service;
        private readonly DateTime _now = new(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);

        public OffersCatalogThankYouAttachTests()
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
        public async Task ListAsync_InFlight_IncludesActiveThankYouAttach()
        {
            var seeded = await SeedOfferAsync(status: "active");
            var location = await _context.RestaurantLocations
                .FirstAsync(row => row.Id == seeded.LocationId);
            location.ThankYouCatalogOfferId = seeded.OfferId;
            await _context.SaveChangesAsync();

            var list = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "in-flight",
                    Page = 1,
                    PageSize = 25,
                }
            );

            Assert.Equal(1, list.TabCounts.InFlight);
            Assert.Single(list.Items);
            Assert.Equal(seeded.OfferId, list.Items[0].Id);
            Assert.Contains(
                CatalogOfferStatus.AttachSourceGuestFormThankYou,
                list.Items[0].AttachKinds
            );
        }

        [Fact]
        public async Task ListAsync_InFlight_ExcludesPausedThankYouAttach()
        {
            var seeded = await SeedOfferAsync(status: "paused");
            var location = await _context.RestaurantLocations
                .FirstAsync(row => row.Id == seeded.LocationId);
            location.ThankYouCatalogOfferId = seeded.OfferId;
            await _context.SaveChangesAsync();

            var list = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "in-flight",
                    Page = 1,
                    PageSize = 25,
                }
            );

            Assert.Equal(0, list.TabCounts.InFlight);
            Assert.Empty(list.Items);
        }

        private async Task<(int LocationId, int OfferId)> SeedOfferAsync(
            string status
        )
        {
            var restaurant = new Restaurant
            {
                Name = "Catalog Rest",
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Catalog Loc",
                Address = "1 High St",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var offer = new CatalogOffer
            {
                RestaurantLocationId = location.Id,
                Status = status,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "Thank you attach",
                Description = "Guest thank-you",
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
