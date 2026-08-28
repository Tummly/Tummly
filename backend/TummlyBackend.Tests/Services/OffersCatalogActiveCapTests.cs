using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Billing;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: stored Drafts do not count toward the Active Offer cap (ticket 30).
    /// </summary>
    public class OffersCatalogActiveCapTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OffersCatalogService _offers;
        private readonly DateTime _now = new(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc);

        public OffersCatalogActiveCapTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            _context = new ApplicationDbContext(options);
            var catalog = PricebookCatalog.LoadFromDirectory(PackDirectory());
            var gate = new ActiveOfferCapGate(_context, catalog);
            _offers = new OffersCatalogService(_context, () => _now, gate);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task SyncInFlight_DoesNotCountDrafts_TowardCap()
        {
            var locationId = await SeedPilotLocationAsync();
            var draftA = await SeedOfferAsync(locationId, CatalogOfferStatus.Draft);
            var draftB = await SeedOfferAsync(locationId, CatalogOfferStatus.Draft);

            var location = await _context.RestaurantLocations.FirstAsync(
                row => row.Id == locationId
            );
            location.ThankYouCatalogOfferId = draftA;
            await _context.SaveChangesAsync();

            var first = await _offers.SyncInFlightStoredStatusAsync(draftA);
            Assert.IsType<CatalogOfferInFlightSyncResult.Ok>(first);
            Assert.Equal(
                CatalogOfferStatus.Active,
                (await _context.CatalogOffers.FirstAsync(row => row.Id == draftA))
                    .Status
            );

            location.ThankYouCatalogOfferId = draftB;
            await _context.SaveChangesAsync();

            var second = await _offers.SyncInFlightStoredStatusAsync(draftB);
            var cap = Assert.IsType<CatalogOfferInFlightSyncResult.CapReached>(
                second
            );
            Assert.Equal(1, cap.Cap);
            Assert.Equal(1, cap.Current);
            Assert.Equal(
                CatalogOfferStatus.Draft,
                (await _context.CatalogOffers.FirstAsync(row => row.Id == draftB))
                    .Status
            );
        }

        [Fact]
        public async Task SyncInFlight_MissingBillingAccount_FailsClosed()
        {
            var locationId = await SeedLocationWithoutBillingAsync();
            var draftId = await SeedOfferAsync(locationId, CatalogOfferStatus.Draft);
            var location = await _context.RestaurantLocations.FirstAsync(
                row => row.Id == locationId
            );
            location.ThankYouCatalogOfferId = draftId;
            await _context.SaveChangesAsync();

            var result = await _offers.SyncInFlightStoredStatusAsync(draftId);

            Assert.IsType<CatalogOfferInFlightSyncResult.FailClosed>(result);
            Assert.Equal(
                CatalogOfferStatus.Draft,
                (await _context.CatalogOffers.FirstAsync(row => row.Id == draftId))
                    .Status
            );
        }

        private async Task<int> SeedLocationWithoutBillingAsync()
        {
            var restaurant = new Restaurant
            {
                Name = "No Billing Venue",
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High St",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }

        private async Task<int> SeedPilotLocationAsync()
        {
            var restaurant = new Restaurant
            {
                Name = "Cap Venue",
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            _context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    "TUMMLY-UK-GBP-2026-08-V3"
                )
            );

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High St",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }

        private async Task<int> SeedOfferAsync(int locationId, string status)
        {
            var offer = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = status,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "Cap offer",
                Description = "Stored catalog offer.",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 10m,
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            _context.CatalogOffers.Add(offer);
            await _context.SaveChangesAsync();
            return offer.Id;
        }

        private static string PackDirectory()
        {
            var dir = Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "..",
                    "docs",
                    "product",
                    "billing-pack-v3.0"
                )
            );
            if (!Directory.Exists(dir))
            {
                dir = Path.GetFullPath(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "..",
                        "..",
                        "..",
                        "..",
                        "docs",
                        "product",
                        "billing-pack-v3.0"
                    )
                );
            }

            Assert.True(Directory.Exists(dir), $"Pack directory missing: {dir}");
            return dir;
        }
    }
}
