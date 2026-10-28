using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Billing;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// ADM-04-03: Group adds +3 active Offers per paid extra Location (ordinals 6-500).
    /// </summary>
    public sealed class ActiveOfferCapGateTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ActiveOfferCapGate _gate;
        private readonly DateTime _now = new(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

        public ActiveOfferCapGateTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            _context = new ApplicationDbContext(options);
            _gate = new ActiveOfferCapGate(
                _context,
                PricebookCatalog.LoadFromDirectory(TestPricebookPaths.PackDirectory())
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Theory]
        [InlineData(0, 25)]
        [InlineData(1, 28)]
        [InlineData(2, 31)]
        public async Task GroupCap_AddsThreeOffersPerPaidExtraLocation(
            int paidExtraLocations,
            int expectedCap
        )
        {
            var restaurantId = await SeedGroupWorkspaceAsync(paidExtraLocations);

            var decision = await _gate.DenyIncrementAsync(restaurantId);

            Assert.True(decision.AllowIncrement);
            Assert.Equal(expectedCap, decision.Cap);
            Assert.Equal(0, decision.Current);
        }

        /// <summary>
        /// 26 active Offers is above the Group base of 25: refused at 5 Locations,
        /// allowed at 7 (cap 31).
        /// </summary>
        [Fact]
        public async Task GroupAtSevenLocations_AllowsOfferAboveBaseCap()
        {
            var restaurantId = await SeedGroupWorkspaceAsync(paidExtraLocations: 2);
            var locationId = await _context.RestaurantLocations
                .Where(row => row.RestaurantId == restaurantId)
                .Select(row => row.Id)
                .SingleAsync();
            for (var i = 0; i < 26; i++)
            {
                _context.CatalogOffers.Add(NewActiveOffer(locationId));
            }

            await _context.SaveChangesAsync();

            var decision = await _gate.DenyIncrementAsync(restaurantId);

            Assert.True(decision.AllowIncrement);
            Assert.Equal(31, decision.Cap);
            Assert.Equal(26, decision.Current);
        }

        [Fact]
        public async Task StarterCap_IgnoresPaidExtraLocationCount()
        {
            var restaurantId = await SeedWorkspaceAsync(
                BillingSubscriptionPlans.Starter,
                paidExtraLocations: 2
            );

            var decision = await _gate.DenyIncrementAsync(restaurantId);

            Assert.Equal(3, decision.Cap);
        }

        private async Task<int> SeedGroupWorkspaceAsync(int paidExtraLocations)
        {
            return await SeedWorkspaceAsync(
                BillingSubscriptionPlans.Group,
                paidExtraLocations
            );
        }

        private async Task<int> SeedWorkspaceAsync(
            string subscriptionPlan,
            int paidExtraLocations
        )
        {
            var restaurant = new Restaurant
            {
                Name = "Offer Cap Venue",
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var billing = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                "TUMMLY-UK-GBP-2026-08-V3"
            );
            billing.SubscriptionPlan = subscriptionPlan;
            billing.PaidExtraLocationCount = paidExtraLocations;
            _context.BillingAccounts.Add(billing);

            _context.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = "Main",
                    Address = "1 High St",
                    CreatedAt = _now,
                }
            );
            await _context.SaveChangesAsync();
            return restaurant.Id;
        }

        private CatalogOffer NewActiveOffer(int locationId) =>
            new()
            {
                RestaurantLocationId = locationId,
                Status = CatalogOfferStatus.Active,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "Cap offer",
                Description = "Stored catalog offer.",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 10m,
                CreatedAt = _now,
                UpdatedAt = _now,
            };
    }
}
