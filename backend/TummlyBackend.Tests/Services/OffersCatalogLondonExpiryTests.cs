using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
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

        /// <summary>
        /// Create always stores Draft, and Draft is checked before the fixed
        /// expiry in <see cref="CatalogOfferStatus.ResolveEffectiveStatus"/>. So
        /// the create response reads "draft" whatever the venue date is. This
        /// test pins that create at 23:30Z BST (London already 16 July) with a
        /// fixed expiry of 15 July still returns Draft. The London-date change
        /// on the create path cannot change this response (no observable RED).
        /// </summary>
        [Fact]
        public async Task CreateDraftAsync_FixedExpiryUtcDate_At2330UtcBst_ReturnsDraft()
        {
            _now = new DateTime(2026, 7, 15, 23, 30, 0, DateTimeKind.Utc);
            var locationId = await SeedLocationAsync();
            var service = new OffersCatalogService(_context, () => _now);

            var dto = await service.CreateDraftAsync(
                new CreateCatalogOfferRequest
                {
                    LocationId = locationId,
                    OfferType = "percentage_discount",
                    Title = "Fixed expiry create",
                    Description = "Create at London midnight",
                    Validity = "choose_expiry_date",
                    ExpiryDate = "2026-07-15",
                    DiscountPercentage = 10m,
                },
                createdByUserId: 1
            );

            Assert.Equal(CatalogOfferStatus.Draft, dto.Status);
            Assert.Equal("2026-07-15", dto.ExpiryDate);
        }

        private async Task<int> SeedLocationAsync()
        {
            var restaurant = new Restaurant
            {
                Name = "London Create Rest",
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "London Create Loc",
                Address = "1 High St",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
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
